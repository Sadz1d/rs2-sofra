using Microsoft.AspNetCore.Http;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Users;
using Sofra.API.Requests.Users;

namespace Sofra.API.Services.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserResponse>> GetListAsync(UserListRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserResponse> CreateStaffAsync(CreateStaffUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, int actorUserId, CancellationToken cancellationToken = default);
    Task DeactivateAsync(int id, int actorUserId, CancellationToken cancellationToken = default);

    Task<UserResponse> GetMeAsync(int actorUserId, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateMeAsync(int actorUserId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> SetProfileImageAsync(int actorUserId, IFormFile file, CancellationToken cancellationToken = default);
}
