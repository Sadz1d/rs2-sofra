using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sofra.API.Constants;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Orders;
using Sofra.API.Extensions;
using Sofra.API.Requests.Orders;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController(IOrderService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderListItemResponse>>> GetList([FromQuery] OrderListRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetListAsync(request, User.GetUserId(), IsStaff(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, User.GetUserId(), IsStaff(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("quote")]
    public async Task<ActionResult<OrderQuoteResponse>> Quote(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.QuoteAsync(request, User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, User.GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:int}/status")]
    public async Task<ActionResult<OrderResponse>> ChangeStatus(int id, OrderTransitionRequest request, CancellationToken cancellationToken)
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList();
        var result = await service.TransitionAsync(id, request, User.GetUserId(), roles, cancellationToken);
        return Ok(result);
    }

    private bool IsStaff() => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Konobar) || User.IsInRole(Roles.Kuhar);
}
