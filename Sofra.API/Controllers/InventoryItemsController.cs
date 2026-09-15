using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sofra.API.Constants;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Inventory;
using Sofra.API.Extensions;
using Sofra.API.Requests.Inventory;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Controllers;

[ApiController]
[Route("api/inventory-items")]
[Authorize]
public class InventoryItemsController(IInventoryItemService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryItemResponse>>> GetList([FromQuery] InventoryItemListRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetListAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InventoryItemResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<InventoryItemResponse>> Create(InventoryItemRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<InventoryItemResponse>> Update(int id, InventoryItemRequest request, CancellationToken cancellationToken)
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

    [HttpPost("{id:int}/adjust")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Kuhar}")]
    public async Task<ActionResult<InventoryItemResponse>> Adjust(int id, AdjustInventoryRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AdjustAsync(id, request, User.GetUserId(), cancellationToken);
        return Ok(result);
    }
}
