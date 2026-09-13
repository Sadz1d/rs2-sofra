using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sofra.API.DTOs.Auth;
using Sofra.API.Extensions;
using Sofra.API.Requests.Auth;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RegisterAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RefreshAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var jti = User.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(long.Parse(User.Claims.First(c => c.Type == JwtRegisteredClaimNames.Exp).Value)).UtcDateTime;

        await authService.LogoutAsync(userId, jti, expiresAtUtc, request.RefreshToken, cancellationToken);
        return NoContent();
    }
}
