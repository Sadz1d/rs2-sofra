using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sofra.API.Constants;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Reviews;
using Sofra.API.Extensions;
using Sofra.API.Requests.Reviews;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewsController(IReviewService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ReviewResponse>>> GetList([FromQuery] ReviewListRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetListAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReviewResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ReviewResponse>> Create(ReviewRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, User.GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReviewResponse>> Update(int id, ReviewUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var isStaff = User.IsInRole(Roles.Admin);
        await service.DeleteAsync(id, User.GetUserId(), isStaff, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:int}/reply")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ReviewResponse>> Reply(int id, ReviewReplyRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ReplyAsync(id, request, User.GetUserId(), cancellationToken);
        return Ok(result);
    }
}
