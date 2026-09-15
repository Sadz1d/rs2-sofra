using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sofra.API.Constants;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Users;
using Sofra.API.Extensions;
using Sofra.API.Requests.Users;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(IUserService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<PagedResult<UserResponse>>> GetList([FromQuery] UserListRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetListAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken cancellationToken)
    {
        var result = await service.GetMeAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserResponse>> UpdateMe(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateMeAsync(User.GetUserId(), request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("me/image")]
    public async Task<ActionResult<UserResponse>> UploadMyImage(IFormFile file, CancellationToken cancellationToken)
    {
        var result = await service.SetProfileImageAsync(User.GetUserId(), file, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UserResponse>> CreateStaff(CreateStaffUserRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateStaffAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UserResponse>> Update(int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await service.DeactivateAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }
}
