using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sofra.API.Constants;
using Sofra.API.DTOs.Statistics;
using Sofra.API.Requests;
using Sofra.API.Requests.Statistics;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Controllers;

[ApiController]
[Route("api/statistics")]
[Authorize(Roles = Roles.Admin)]
public class StatisticsController(IStatisticsService service) : ControllerBase
{
    [HttpGet("revenue")]
    public async Task<ActionResult<IReadOnlyList<RevenueStatItem>>> Revenue([FromQuery] RevenueStatisticsRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetRevenueAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("orders-by-status")]
    public async Task<ActionResult<IReadOnlyList<OrderStatusStatItem>>> OrdersByStatus([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetOrdersByStatusAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("average-order-value")]
    public async Task<ActionResult<AverageOrderValueResponse>> AverageOrderValue([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetAverageOrderValueAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("top-items")]
    public async Task<ActionResult<IReadOnlyList<TopMenuItemStatItem>>> TopItems([FromQuery] DateRangeRequest request, [FromQuery] int take, CancellationToken cancellationToken)
    {
        var result = await service.GetTopMenuItemsAsync(request, take <= 0 ? 10 : take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("zone-occupancy")]
    public async Task<ActionResult<IReadOnlyList<ZoneOccupancyStatItem>>> ZoneOccupancy([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetZoneOccupancyAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("average-rating")]
    public async Task<ActionResult<AverageRatingResponse>> AverageRating([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetAverageRatingAsync(request, cancellationToken);
        return Ok(result);
    }
}
