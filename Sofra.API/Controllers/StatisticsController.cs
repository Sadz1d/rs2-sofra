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
[Authorize]
public class StatisticsController(IStatisticsService service) : ControllerBase
{
    /// <summary>Sve za Dashboard u jednom pozivu - dostupno svom osoblju. Finansijska polja
    /// (promet, prosjecna vrijednost narudzbe, PDV) dolaze null za ne-Admin osoblje.</summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Konobar},{Roles.Kuhar}")]
    public async Task<ActionResult<DashboardResponse>> Dashboard([FromQuery] RevenueStatisticsRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetDashboardAsync(request, User.IsInRole(Roles.Admin), cancellationToken);
        return Ok(result);
    }

    [HttpGet("revenue")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IReadOnlyList<RevenueStatItem>>> Revenue([FromQuery] RevenueStatisticsRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetRevenueAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("orders-by-status")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IReadOnlyList<OrderStatusStatItem>>> OrdersByStatus([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetOrdersByStatusAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("average-order-value")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<AverageOrderValueResponse>> AverageOrderValue([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetAverageOrderValueAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("top-items")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IReadOnlyList<TopMenuItemStatItem>>> TopItems([FromQuery] DateRangeRequest request, [FromQuery] int take, CancellationToken cancellationToken)
    {
        var result = await service.GetTopMenuItemsAsync(request, take <= 0 ? 10 : take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("zone-occupancy")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IReadOnlyList<ZoneOccupancyStatItem>>> ZoneOccupancy([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetZoneOccupancyAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("average-rating")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<AverageRatingResponse>> AverageRating([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetAverageRatingAsync(request, cancellationToken);
        return Ok(result);
    }
}
