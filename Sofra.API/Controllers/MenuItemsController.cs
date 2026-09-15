using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sofra.API.Constants;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Menu;
using Sofra.API.Requests.Menu;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Controllers;

[ApiController]
[Route("api/menu-items")]
[Authorize]
public class MenuItemsController(IMenuItemService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<MenuItemResponse>>> GetList([FromQuery] MenuItemListRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetListAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MenuItemResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<MenuItemResponse>> Create(MenuItemRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<MenuItemResponse>> Update(int id, MenuItemRequest request, CancellationToken cancellationToken)
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

    [HttpGet("{id:int}/ingredients")]
    public async Task<ActionResult<IReadOnlyList<MenuItemIngredientResponse>>> GetIngredients(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetIngredientsAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}/ingredients")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IReadOnlyList<MenuItemIngredientResponse>>> UpdateIngredients(int id, UpdateMenuItemIngredientsRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateIngredientsAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/image")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<MenuItemResponse>> UploadImage(int id, IFormFile file, CancellationToken cancellationToken)
    {
        var result = await service.SetImageAsync(id, file, cancellationToken);
        return Ok(result);
    }
}
