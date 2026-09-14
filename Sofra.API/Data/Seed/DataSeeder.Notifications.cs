using Microsoft.EntityFrameworkCore;
using Sofra.API.Constants;
using Sofra.API.Entities;
using Sofra.API.Enums;

namespace Sofra.API.Data.Seed;

/// <summary>
/// ~40 notifikacija vezanih preko ReferenceId za vec seed-ovane narudzbe, placanja i rezervacije.
/// Vrijeme prati stvarni tranzicioni datum izvornog zapisa (npr. ConfirmedAt, PaidAt, ProcessedAt).
/// </summary>
public partial class DataSeeder
{
    private async Task SeedNotificationsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Notifications.AnyAsync(cancellationToken))
        {
            return;
        }

        var orders = await dbContext.Orders.AsNoTracking().Include(o => o.Payment).ToListAsync(cancellationToken);
        var reservations = await dbContext.Reservations.AsNoTracking().ToListAsync(cancellationToken);
        var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);

        void AddNotification(int userId, string title, string text, NotificationType type, int? referenceId, DateTime createdAt)
        {
            dbContext.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Text = text,
                Type = type,
                ReferenceId = referenceId,
                IsRead = Rng.Next(100) < 60,
                CreatedAt = createdAt,
            });
        }

        // Narudzbe (20) - tekst i vrijeme prate stvarni status narudzbe.
        var orderSample = orders.Where(o => o.Status != OrderStatus.Pending).OrderBy(_ => Rng.Next()).Take(20).ToList();
        foreach (var order in orderSample)
        {
            var (title, text, at) = order.Status switch
            {
                OrderStatus.Cancelled => ($"Narudžba #{order.Number} otkazana", $"Vaša narudžba #{order.Number} je otkazana. Razlog: {order.CancelReason}.", order.CancelledAt!.Value),
                OrderStatus.Completed => ($"Narudžba #{order.Number} završena", $"Hvala vam! Narudžba #{order.Number} je uspješno završena.", order.CompletedAt!.Value),
                OrderStatus.Delivered => ($"Narudžba #{order.Number} isporučena", $"Vaša narudžba #{order.Number} je isporučena.", order.DeliveredAt!.Value),
                OrderStatus.Ready => ($"Narudžba #{order.Number} spremna", $"Vaša narudžba #{order.Number} je spremna za preuzimanje.", order.ReadyAt!.Value),
                _ => ($"Narudžba #{order.Number} potvrđena", $"Vaša narudžba #{order.Number} je potvrđena i priprema se.", order.ConfirmedAt!.Value),
            };
            AddNotification(order.UserId, title, text, NotificationType.OrderStatus, order.Id, at);
        }

        // Placanja (10).
        var paidOrders = orders.Where(o => o.Payment is not null).OrderBy(_ => Rng.Next()).Take(10).ToList();
        foreach (var order in paidOrders)
        {
            var payment = order.Payment!;
            if (payment.Status == PaymentStatus.Refunded)
            {
                AddNotification(order.UserId, "Uplata refundirana",
                    $"Iznos od {payment.RefundedAmount:0.00} KM za narudžbu #{order.Number} je refundiran.",
                    NotificationType.Payment, order.Id, payment.RefundedAt!.Value);
            }
            else
            {
                AddNotification(order.UserId, "Plaćanje uspješno",
                    $"Plaćanje za narudžbu #{order.Number} u iznosu od {payment.Amount:0.00} KM je uspješno izvršeno.",
                    NotificationType.Payment, order.Id, payment.PaidAt!.Value);
            }
        }

        // Rezervacije (8) za gosta.
        var reservationSample = reservations.Where(r => r.Status != ReservationStatus.Pending).OrderBy(_ => Rng.Next()).Take(8).ToList();
        foreach (var reservation in reservationSample)
        {
            var (title, text, at) = reservation.Status switch
            {
                ReservationStatus.Confirmed => ("Rezervacija potvrđena", $"Vaša rezervacija za {reservation.ReservationAt:dd.MM.yyyy HH:mm} je potvrđena.", reservation.ProcessedAt ?? reservation.CreatedAt),
                ReservationStatus.Rejected => ("Rezervacija odbijena", $"Nažalost, rezervacija za {reservation.ReservationAt:dd.MM.yyyy HH:mm} je odbijena. Razlog: {reservation.RejectReason}.", reservation.ProcessedAt ?? reservation.CreatedAt),
                ReservationStatus.Cancelled => ("Rezervacija otkazana", $"Rezervacija za {reservation.ReservationAt:dd.MM.yyyy HH:mm} je otkazana.", reservation.CancelledAt ?? reservation.CreatedAt),
                ReservationStatus.NoShow => ("Rezervacija označena kao neostvarena", $"Rezervacija za {reservation.ReservationAt:dd.MM.yyyy HH:mm} je označena kao no-show.", reservation.ProcessedAt ?? reservation.CreatedAt),
                _ => ("Rezervacija završena", $"Nadamo se da ste uživali! Rezervacija za {reservation.ReservationAt:dd.MM.yyyy HH:mm} je označena kao završena.", reservation.ProcessedAt ?? reservation.CreatedAt),
            };
            AddNotification(reservation.UserId, title, text, NotificationType.Reservation, reservation.Id, at);
        }

        // Osoblje (2) - nove rezervacije koje cekaju odobrenje.
        var pendingReservations = reservations.Where(r => r.Status == ReservationStatus.Pending).OrderBy(_ => Rng.Next()).Take(2).ToList();
        foreach (var reservation in pendingReservations)
        {
            AddNotification(admins[Rng.Next(admins.Count)].Id, "Nova rezervacija čeka odobrenje",
                $"Stigla je nova rezervacija za {reservation.ReservationAt:dd.MM.yyyy HH:mm} ({reservation.Guests} gostiju).",
                NotificationType.Reservation, reservation.Id, reservation.CreatedAt);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
