using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Users;
using Sofra.API.Entities;
using Sofra.API.Exceptions;
using Sofra.API.Requests.Users;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class UserService(AppDbContext dbContext, UserManager<ApplicationUser> userManager, IImageUploadService imageUploadService) : IUserService
{
    private readonly Expression<Func<ApplicationUser, UserResponse>> _projectToResponse = u => new UserResponse(
        u.Id, u.UserName!, u.Email!,
        u.FirstName, u.LastName, u.Phone,
        u.CityId, u.City == null ? null : u.City.Name, u.ImageUrl,
        u.IsActive,
        dbContext.UserRoles.Where(ur => ur.UserId == u.Id).Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!).ToList());

    public async Task<PagedResult<UserResponse>> GetListAsync(UserListRequest request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => x.FirstName.Contains(request.Search) || x.LastName.Contains(request.Search) || x.UserName!.Contains(request.Search));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            query = query.Where(x => dbContext.UserRoles.Any(ur => ur.UserId == x.Id && dbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == request.Role)));
        }

        query = request.SortDesc ? query.OrderByDescending(x => x.LastName) : query.OrderBy(x => x.LastName);

        return await query.Select(_projectToResponse).ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<UserResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await dbContext.Users.AsNoTracking().Where(x => x.Id == id).Select(_projectToResponse).FirstOrDefaultAsync(cancellationToken);
        return response ?? throw new NotFoundException($"Korisnik sa Id {id} ne postoji.");
    }

    public async Task<UserResponse> CreateStaffAsync(CreateStaffUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            CityId = request.CityId,
            IsActive = true,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            throw new Exceptions.ValidationException(ToErrorDictionary(createResult));
        }

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            throw new Exceptions.ValidationException(ToErrorDictionary(roleResult));
        }

        dbContext.Employees.Add(new Employee
        {
            UserId = user.Id,
            Position = request.Position,
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString())
            ?? throw new NotFoundException($"Korisnik sa Id {id} ne postoji.");

        var currentRoles = await userManager.GetRolesAsync(user);
        var currentRole = currentRoles.FirstOrDefault();

        if (id == actorUserId)
        {
            if (request.Role is not null && request.Role != currentRole)
            {
                throw new ForbiddenException("Ne možete sami sebi promijeniti ulogu.");
            }

            if (request.IsActive == false)
            {
                throw new ForbiddenException("Ne možete sami sebe deaktivirati.");
            }
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.CityId = request.CityId;
        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new Exceptions.ValidationException(ToErrorDictionary(updateResult));
        }

        if (request.Role is not null && request.Role != currentRole)
        {
            if (currentRoles.Count > 0)
            {
                await userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            await userManager.AddToRoleAsync(user, request.Role);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeactivateAsync(int id, int actorUserId, CancellationToken cancellationToken = default)
    {
        if (id == actorUserId)
        {
            throw new ForbiddenException("Ne možete sami sebe deaktivirati.");
        }

        var user = await userManager.FindByIdAsync(id.ToString())
            ?? throw new NotFoundException($"Korisnik sa Id {id} ne postoji.");

        if (!user.IsActive)
        {
            return;
        }

        user.IsActive = false;
        await userManager.UpdateAsync(user);
    }

    public Task<UserResponse> GetMeAsync(int actorUserId, CancellationToken cancellationToken = default) => GetByIdAsync(actorUserId, cancellationToken);

    public async Task<UserResponse> UpdateMeAsync(int actorUserId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(actorUserId.ToString())
            ?? throw new NotFoundException("Korisnik ne postoji.");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.CityId = request.CityId;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new Exceptions.ValidationException(ToErrorDictionary(updateResult));
        }

        return await GetByIdAsync(actorUserId, cancellationToken);
    }

    public async Task<UserResponse> SetProfileImageAsync(int actorUserId, IFormFile file, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(actorUserId.ToString())
            ?? throw new NotFoundException("Korisnik ne postoji.");

        var newUrl = await imageUploadService.SaveAsync(file, "users", cancellationToken);
        imageUploadService.DeleteIfExists(user.ImageUrl);
        user.ImageUrl = newUrl;

        await userManager.UpdateAsync(user);

        return await GetByIdAsync(actorUserId, cancellationToken);
    }

    private static Dictionary<string, string[]> ToErrorDictionary(IdentityResult result) =>
        new() { ["identity"] = [.. result.Errors.Select(e => e.Description)] };
}
