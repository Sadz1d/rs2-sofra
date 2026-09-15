using Sofra.API.DTOs.Statistics;
using Sofra.API.Requests;
using Sofra.API.Requests.Statistics;

namespace Sofra.API.Services.Interfaces;

public interface IStatisticsService
{
    Task<IReadOnlyList<RevenueStatItem>> GetRevenueAsync(RevenueStatisticsRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderStatusStatItem>> GetOrdersByStatusAsync(DateRangeRequest request, CancellationToken cancellationToken = default);
    Task<AverageOrderValueResponse> GetAverageOrderValueAsync(DateRangeRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopMenuItemStatItem>> GetTopMenuItemsAsync(DateRangeRequest request, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ZoneOccupancyStatItem>> GetZoneOccupancyAsync(DateRangeRequest request, CancellationToken cancellationToken = default);
    Task<AverageRatingResponse> GetAverageRatingAsync(DateRangeRequest request, CancellationToken cancellationToken = default);
}
