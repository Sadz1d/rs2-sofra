using Microsoft.EntityFrameworkCore;
using Sofra.API.Data;
using Sofra.API.DTOs.Inventory;
using Sofra.API.DTOs.Orders;
using Sofra.API.DTOs.Reservations;
using Sofra.API.DTOs.Statistics;
using Sofra.API.Enums;
using Sofra.API.Requests;
using Sofra.API.Requests.Statistics;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class StatisticsService(AppDbContext dbContext) : IStatisticsService
{
    public async Task<IReadOnlyList<RevenueStatItem>> GetRevenueAsync(RevenueStatisticsRequest request, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToRange(request.DateFrom, request.DateTo);

        var baseQuery = dbContext.Orders.AsNoTracking()
            .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtc && x.Status != OrderStatus.Cancelled);

        if (request.Period == StatisticsPeriod.Month)
        {
            var raw = await baseQuery
                .GroupBy(x => new { x.CreatedAt.Year, x.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count(), Subtotal = g.Sum(x => x.Subtotal), Tax = g.Sum(x => x.Tax), Total = g.Sum(x => x.Total) })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync(cancellationToken);

            return raw.Select(x => new RevenueStatItem(new DateTime(x.Year, x.Month, 1, 0, 0, 0, DateTimeKind.Utc), x.Count, x.Subtotal, x.Tax, x.Total)).ToList();
        }
        else
        {
            var raw = await baseQuery
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count(), Subtotal = g.Sum(x => x.Subtotal), Tax = g.Sum(x => x.Tax), Total = g.Sum(x => x.Total) })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);

            return raw.Select(x => new RevenueStatItem(DateTime.SpecifyKind(x.Date, DateTimeKind.Utc), x.Count, x.Subtotal, x.Tax, x.Total)).ToList();
        }
    }

    public async Task<IReadOnlyList<OrderStatusStatItem>> GetOrdersByStatusAsync(DateRangeRequest request, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToRange(request.DateFrom, request.DateTo);

        var counts = await dbContext.Orders.AsNoTracking()
            .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtc)
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return Enum.GetValues<OrderStatus>()
            .Select(status => new OrderStatusStatItem(status, counts.FirstOrDefault(x => x.Status == status)?.Count ?? 0))
            .ToList();
    }

    public async Task<AverageOrderValueResponse> GetAverageOrderValueAsync(DateRangeRequest request, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToRange(request.DateFrom, request.DateTo);

        var query = dbContext.Orders.AsNoTracking()
            .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtc && x.Status != OrderStatus.Cancelled);

        var count = await query.CountAsync(cancellationToken);
        if (count == 0)
        {
            return new AverageOrderValueResponse(0, 0);
        }

        var average = await query.AverageAsync(x => x.Total, cancellationToken);
        return new AverageOrderValueResponse(Math.Round(average, 2), count);
    }

    public async Task<IReadOnlyList<TopMenuItemStatItem>> GetTopMenuItemsAsync(DateRangeRequest request, int take, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToRange(request.DateFrom, request.DateTo);

        // GroupBy po navigation propertyju (MenuItem.Name) se ne prevodi u SQL - grupisanje po FK, naziv se dovlaci posebno.
        var raw = await dbContext.OrderItems.AsNoTracking()
            .Where(x => x.Order.CreatedAt >= fromUtc && x.Order.CreatedAt < toUtc && x.Order.Status != OrderStatus.Cancelled)
            .GroupBy(x => x.MenuItemId)
            .Select(g => new { MenuItemId = g.Key, Quantity = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.Quantity * x.UnitPrice) })
            .OrderByDescending(x => x.Quantity)
            .Take(take)
            .ToListAsync(cancellationToken);

        var menuItemIds = raw.Select(x => x.MenuItemId).ToList();
        var names = await dbContext.MenuItems.AsNoTracking()
            .Where(x => menuItemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        return raw.Select(x => new TopMenuItemStatItem(x.MenuItemId, names.GetValueOrDefault(x.MenuItemId, string.Empty), x.Quantity, x.Revenue)).ToList();
    }

    public async Task<IReadOnlyList<ZoneOccupancyStatItem>> GetZoneOccupancyAsync(DateRangeRequest request, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToRange(request.DateFrom, request.DateTo);

        // GroupBy po navigation propertyju (Zone.Name) se ne prevodi u SQL - grupisanje po FK, naziv se dovlaci posebno.
        var raw = await dbContext.Reservations.AsNoTracking()
            .Where(x => x.ReservationAt >= fromUtc && x.ReservationAt < toUtc)
            .GroupBy(x => x.ZoneId)
            .Select(g => new { ZoneId = g.Key, Count = g.Count(), Guests = g.Sum(x => x.Guests) })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        var zoneIds = raw.Select(x => x.ZoneId).ToList();
        var names = await dbContext.Zones.AsNoTracking()
            .Where(x => zoneIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        return raw.Select(x => new ZoneOccupancyStatItem(x.ZoneId, names.GetValueOrDefault(x.ZoneId, string.Empty), x.Count, x.Guests)).ToList();
    }

    public async Task<AverageRatingResponse> GetAverageRatingAsync(DateRangeRequest request, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToRange(request.DateFrom, request.DateTo);

        var query = dbContext.Reviews.AsNoTracking()
            .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtc);

        var count = await query.CountAsync(cancellationToken);
        if (count == 0)
        {
            return new AverageRatingResponse(null, 0);
        }

        var average = await query.AverageAsync(x => (decimal)x.Rating, cancellationToken);
        return new AverageRatingResponse(Math.Round(average, 2), count);
    }

    public async Task<DashboardResponse> GetDashboardAsync(RevenueStatisticsRequest request, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToRange(request.DateFrom, request.DateTo);

        // Racuna se uvijek (jeftino) - finansijska polja se odsijeku tek u projekciji za ne-Admin,
        // da ne dupliramo upite za dvije verzije istog grafa/liste.
        var nonCancelledInRange = dbContext.Orders.AsNoTracking()
            .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtc && x.Status != OrderStatus.Cancelled);

        var ordersCount = await nonCancelledInRange.CountAsync(cancellationToken);
        decimal? revenue = null;
        decimal? tax = null;
        decimal? averageOrderValue = null;
        if (isAdmin)
        {
            revenue = ordersCount == 0 ? 0 : await nonCancelledInRange.SumAsync(x => x.Total, cancellationToken);
            tax = ordersCount == 0 ? 0 : await nonCancelledInRange.SumAsync(x => x.Tax, cancellationToken);
            averageOrderValue = ordersCount == 0 ? 0 : Math.Round(revenue.Value / ordersCount, 2);
        }

        List<DashboardChartPoint> chart;
        if (request.Period == StatisticsPeriod.Month)
        {
            var raw = await nonCancelledInRange
                .GroupBy(x => new { x.CreatedAt.Year, x.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count(), Total = g.Sum(x => x.Total) })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync(cancellationToken);
            chart = raw.Select(x =>
                new DashboardChartPoint(new DateTime(x.Year, x.Month, 1, 0, 0, 0, DateTimeKind.Utc), x.Count, isAdmin ? x.Total : null)).ToList();
        }
        else
        {
            var raw = await nonCancelledInRange
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count(), Total = g.Sum(x => x.Total) })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);
            chart = raw.Select(x =>
                new DashboardChartPoint(DateTime.SpecifyKind(x.Date, DateTimeKind.Utc), x.Count, isAdmin ? x.Total : null)).ToList();
        }

        var rawTopItems = await dbContext.OrderItems.AsNoTracking()
            .Where(x => x.Order.CreatedAt >= fromUtc && x.Order.CreatedAt < toUtc && x.Order.Status != OrderStatus.Cancelled)
            .GroupBy(x => x.MenuItemId)
            .Select(g => new { MenuItemId = g.Key, Quantity = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.Quantity * x.UnitPrice) })
            .OrderByDescending(x => x.Quantity)
            .Take(5)
            .ToListAsync(cancellationToken);
        var topItemIds = rawTopItems.Select(x => x.MenuItemId).ToList();
        var topItemNames = await dbContext.MenuItems.AsNoTracking()
            .Where(x => topItemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var topItems = rawTopItems
            .Select(x => new DashboardTopItem(x.MenuItemId, topItemNames.GetValueOrDefault(x.MenuItemId, string.Empty), x.Quantity, isAdmin ? x.Revenue : null))
            .ToList();

        var activeStatuses = new[] { OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.InPreparation, OrderStatus.Ready, OrderStatus.Delivered };
        var activeOrdersQuery = dbContext.Orders.AsNoTracking().Where(x => activeStatuses.Contains(x.Status));
        var activeOrdersCount = await activeOrdersQuery.CountAsync(cancellationToken);
        var activeOrders = await activeOrdersQuery
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .Select(x => new OrderListItemResponse(
                x.Id, x.Number, x.Type, x.Status,
                x.UserId, x.User.FirstName + " " + x.User.LastName,
                x.DiningTableId, x.DiningTable == null ? null : x.DiningTable.Number,
                x.Total, x.Payment != null && x.Payment.Status == PaymentStatus.Succeeded, x.CreatedAt, x.Items.Count))
            .ToListAsync(cancellationToken);

        var reservationsQuery = dbContext.Reservations.AsNoTracking()
            .Where(x => x.ReservationAt >= fromUtc && x.ReservationAt < toUtc);
        var reservationsCount = await reservationsQuery.CountAsync(cancellationToken);
        var reservationsPendingCount = await reservationsQuery.CountAsync(x => x.Status == ReservationStatus.Pending, cancellationToken);
        var reservations = await reservationsQuery
            .OrderBy(x => x.ReservationAt)
            .Take(10)
            .Select(x => new ReservationListItemResponse(
                x.Id, x.UserId, x.User.FirstName + " " + x.User.LastName,
                x.ReservationAt, x.DurationMinutes, x.Guests,
                x.ZoneId, x.Zone.Name,
                x.DiningTableId, x.DiningTable == null ? null : x.DiningTable.Number,
                x.Status, x.CreatedAt))
            .ToListAsync(cancellationToken);

        var tablesTotal = await dbContext.DiningTables.AsNoTracking().CountAsync(x => x.IsActive, cancellationToken);
        var tablesOccupied = await dbContext.DiningTables.AsNoTracking()
            .CountAsync(x => x.IsActive && x.Status == TableStatus.Occupied, cancellationToken);

        var lowStockQuery = dbContext.InventoryItems.AsNoTracking().Where(x => x.Quantity <= x.MinQuantity);
        var lowStockCount = await lowStockQuery.CountAsync(cancellationToken);
        var lowStockItems = await lowStockQuery
            .OrderBy(x => x.Name)
            .Take(5)
            .Select(x => new InventoryItemResponse(
                x.Id, x.Name,
                x.InventoryCategoryId, x.InventoryCategory.Name,
                x.UnitOfMeasureId, x.UnitOfMeasure.Name, x.UnitOfMeasure.Abbreviation,
                x.Quantity, x.MinQuantity, x.UnitCost,
                true, x.IsActive))
            .ToListAsync(cancellationToken);

        return new DashboardResponse(
            ordersCount, revenue, tax, averageOrderValue,
            activeOrdersCount, reservationsCount, reservationsPendingCount,
            tablesOccupied, tablesTotal,
            lowStockCount,
            chart, activeOrders, reservations, lowStockItems, topItems);
    }

    private static (DateTime FromUtc, DateTime ToUtcExclusive) ToRange(DateOnly from, DateOnly to) =>
        (from.ToDateTime(TimeOnly.MinValue), to.AddDays(1).ToDateTime(TimeOnly.MinValue));
}
