using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sofra.API.Constants;
using Sofra.API.DTOs;
using Sofra.API.DTOs.News;
using Sofra.API.Extensions;
using Sofra.API.Requests.News;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Controllers;

[ApiController]
[Route("api/news")]
[Authorize]
public class NewsController(INewsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<NewsResponse>>> GetList([FromQuery] NewsListRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetListAsync(request, IsStaff(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NewsResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, IsStaff(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<NewsResponse>> Create(NewsRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, User.GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<NewsResponse>> Update(int id, NewsRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/image")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<NewsResponse>> UploadImage(int id, IFormFile file, CancellationToken cancellationToken)
    {
        var result = await service.SetImageAsync(id, file, cancellationToken);
        return Ok(result);
    }

    private bool IsStaff() => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Konobar) || User.IsInRole(Roles.Kuhar);
}
