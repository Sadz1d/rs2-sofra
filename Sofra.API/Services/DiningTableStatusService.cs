using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sofra.API.Data;
using Sofra.API.Enums;
using Sofra.API.Options;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class DiningTableStatusService(AppDbContext dbContext, IOptions<ReservationOptions> reservationOptions) : IDiningTableStatusService
{
    // Sto je "u upotrebi" narudzbom od trenutka kad je potvrdjena do isporuke; Pending je jos samo zahtjev.
    private static readonly OrderStatus[] OccupyingOrderStatuses =
    [
        OrderStatus.Confirmed, OrderStatus.InPreparation, OrderStatus.Ready, OrderStatus.Delivered,
    ];

    public async Task RecalculateAsync(int diningTableId, CancellationToken cancellationToken = default)
    {
        var table = await dbContext.DiningTables.FirstOrDefaultAsync(x => x.Id == diningTableId, cancellationToken);
        if (table is null)
        {
            return;
        }

        var hasOccupyingOrder = await dbContext.Orders
            .AnyAsync(x => x.DiningTableId == diningTableId && OccupyingOrderStatuses.Contains(x.Status), cancellationToken);

        if (hasOccupyingOrder)
        {
            table.Status = TableStatus.Occupied;
        }
        else
        {
            var now = DateTime.UtcNow;
            var windowEnd = now.AddMinutes(reservationOptions.Value.UpcomingWindowMinutes);

            var hasUpcomingReservation = await dbContext.Reservations.AnyAsync(x =>
                x.DiningTableId == diningTableId &&
                x.Status == ReservationStatus.Confirmed &&
                x.ReservationAt < windowEnd &&
                x.ReservationAt.AddMinutes(x.DurationMinutes) > now, cancellationToken);

            table.Status = hasUpcomingReservation ? TableStatus.Reserved : TableStatus.Free;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
