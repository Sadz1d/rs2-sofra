using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sofra.API.Constants;
using Sofra.API.Data;
using Sofra.API.DTOs.Auth;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Options;
using Sofra.API.Requests.Auth;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    ITokenService tokenService,
    IJtiDenylistService jtiDenylistService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            IsActive = true,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            throw new ValidationException(ToErrorDictionary(createResult));
        }

        var roleResult = await userManager.AddToRoleAsync(user, Roles.Gost);
        if (!roleResult.Succeeded)
        {
            throw new ValidationException(ToErrorDictionary(roleResult));
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null || !user.IsActive || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new BusinessException("Pogrešno korisničko ime ili lozinka.");
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var existing = await dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (existing is null || existing.RevokedAt is not null || existing.ExpiresAt <= DateTime.UtcNow || !existing.User.IsActive)
        {
            throw new BusinessException("Refresh token nije validan ili je istekao.");
        }

        var roles = await userManager.GetRolesAsync(existing.User);
        var (accessToken, accessExpiresAtUtc, _) = tokenService.CreateAccessToken(existing.User, roles);
        var newRefreshToken = tokenService.CreateRefreshToken();
        var newHash = tokenService.HashRefreshToken(newRefreshToken);

        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByHash = newHash;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = existing.UserId,
            TokenHash = newHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, newRefreshToken, accessExpiresAtUtc, await MapUserAsync(existing.User, roles));
    }

    public async Task LogoutAsync(int userId, string jti, DateTime accessTokenExpiresAtUtc, string? refreshToken, CancellationToken cancellationToken = default)
    {
        jtiDenylistService.Deny(jti, accessTokenExpiresAtUtc);

        if (string.IsNullOrEmpty(refreshToken))
        {
            return;
        }

        var tokenHash = tokenService.HashRefreshToken(refreshToken);
        var existing = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.UserId == userId, cancellationToken);

        if (existing is not null && existing.RevokedAt is null)
        {
            existing.RevokedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, accessExpiresAtUtc, _) = tokenService.CreateAccessToken(user, roles);
        var refreshToken = tokenService.CreateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, refreshToken, accessExpiresAtUtc, await MapUserAsync(user, roles));
    }

    private static Task<UserResponse> MapUserAsync(ApplicationUser user, IList<string> roles) =>
        Task.FromResult(new UserResponse(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.ImageUrl,
            [.. roles]));

    private static Dictionary<string, string[]> ToErrorDictionary(IdentityResult result) =>
        new() { ["identity"] = [.. result.Errors.Select(e => e.Description)] };
}
