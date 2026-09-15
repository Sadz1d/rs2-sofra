using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using Sofra.API.Data;
using Sofra.API.Enums;
using Sofra.API.Exceptions;
using Sofra.API.Options;
using Sofra.API.Reports;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class ReportService(AppDbContext dbContext, IOptions<RestaurantOptions> restaurantOptions) : IReportService
{
    private string RestaurantName => restaurantOptions.Value.Name;


    public async Task<byte[]> GenerateRevenueReportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToRange(from, to);

        // DateOnly.FromDateTime ne moze se prevesti u SQL - agregacija ide u bazi, konverzija u DateOnly u memoriji.
        var raw = await dbContext.Orders.AsNoTracking()
            .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtc && x.Status != OrderStatus.Cancelled)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count(), Subtotal = g.Sum(x => x.Subtotal), Tax = g.Sum(x => x.Tax), Total = g.Sum(x => x.Total) })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        var rows = raw.Select(x => new RevenueReportRow(DateOnly.FromDateTime(x.Date), x.Count, x.Subtotal, x.Tax, x.Total)).ToList();

        var document = new RevenueReportDocument(RestaurantName, from, to, rows);
        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateTopItemsReportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToRange(from, to);

        // GroupBy po navigation propertyju (MenuItem.Name) se ne prevodi u SQL - grupisanje po FK, naziv se dovlaci posebno.
        var raw = await dbContext.OrderItems.AsNoTracking()
            .Where(x => x.Order.CreatedAt >= fromUtc && x.Order.CreatedAt < toUtc && x.Order.Status != OrderStatus.Cancelled)
            .GroupBy(x => x.MenuItemId)
            .Select(g => new { MenuItemId = g.Key, Quantity = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.Quantity * x.UnitPrice) })
            .OrderByDescending(x => x.Quantity)
            .Take(20)
            .ToListAsync(cancellationToken);

        var menuItemIds = raw.Select(x => x.MenuItemId).ToList();
        var names = await dbContext.MenuItems.AsNoTracking()
            .Where(x => menuItemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var rows = raw.Select(x => new TopItemRow(names.GetValueOrDefault(x.MenuItemId, string.Empty), x.Quantity, x.Revenue)).ToList();

        var document = new TopItemsReportDocument(RestaurantName, from, to, rows);
        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateReservationsReportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToRange(from, to);

        var counts = await dbContext.Reservations.AsNoTracking()
            .Where(x => x.ReservationAt >= fromUtc && x.ReservationAt < toUtc)
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var rows = Enum.GetValues<ReservationStatus>()
            .Select(status => new ReservationStatusRow(
                ReservationStateMachine.GetStatusLabel(status),
                counts.FirstOrDefault(x => x.Status == status)?.Count ?? 0))
            .ToList();

        var document = new ReservationsReportDocument(RestaurantName, from, to, rows);
        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateReceiptAsync(int orderId, int actorUserId, bool isStaff, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders.AsNoTracking()
            .Include(x => x.Items).ThenInclude(x => x.MenuItem)
            .Include(x => x.DiningTable)
            .Include(x => x.Payment).ThenInclude(x => x!.PaymentMethod)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new NotFoundException($"Narudžba sa Id {orderId} ne postoji.");

        if (!isStaff && order.UserId != actorUserId)
        {
            throw new ForbiddenException("Ne možete preuzeti račun za tuđu narudžbu.");
        }

        var data = new ReceiptData(
            RestaurantName, order.Number, order.CreatedAt, order.DiningTable?.Number,
            order.Items.Select(i => new ReceiptItemRow(i.MenuItem.Name, i.Quantity, i.UnitPrice, i.UnitPrice * i.Quantity)).ToList(),
            order.Subtotal, order.Discount, order.Tax, order.Total,
            order.Payment?.PaymentMethod.Name,
            order.Payment?.Status == PaymentStatus.Succeeded);

        var document = new ReceiptDocument(data);
        return document.GeneratePdf();
    }

    private static (DateTime FromUtc, DateTime ToUtcExclusive) ToRange(DateOnly from, DateOnly to) =>
        (from.ToDateTime(TimeOnly.MinValue), to.AddDays(1).ToDateTime(TimeOnly.MinValue));
}
