using Sofra.API.DTOs.Inventory;
using Sofra.API.DTOs.Orders;
using Sofra.API.DTOs.Reservations;

namespace Sofra.API.DTOs.Statistics;

/// <summary>
/// Sve sto Dashboard-u treba u jednom pozivu, za zadani raspon datuma. Revenue/Tax/AverageOrderValue
/// (i Revenue unutar Chart/TopItems) su null za ne-Admin osoblje - finansijski podaci se ne racunaju za njih.
/// </summary>
public record DashboardResponse(
    int OrdersCount, decimal? Revenue, decimal? Tax, decimal? AverageOrderValue,
    int ActiveOrdersCount, int ReservationsCount, int ReservationsPendingCount,
    int TablesOccupied, int TablesTotal,
    int LowStockCount,
    IReadOnlyList<DashboardChartPoint> Chart,
    IReadOnlyList<OrderListItemResponse> ActiveOrders,
    IReadOnlyList<ReservationListItemResponse> Reservations,
    IReadOnlyList<InventoryItemResponse> LowStockItems,
    IReadOnlyList<DashboardTopItem> TopItems);
