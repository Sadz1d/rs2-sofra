using Sofra.API.Constants;
using Sofra.API.Entities;
using Sofra.API.Enums;
using Sofra.API.Exceptions;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

/// <summary>
/// Jedino mjesto koje zna koji su prelazi OrderStatus-a dozvoljeni i ko ih smije izvrsiti.
/// Kontroleri i servisi nikad ne postavljaju Order.Status direktno.
/// </summary>
public class OrderStateMachine : IOrderStateMachine
{
    private sealed record Transition(OrderStatus To, string[] AllowedRoles, bool AllowOwner = false);

    private static readonly Dictionary<OrderStatus, Transition[]> Graph = new()
    {
        [OrderStatus.Pending] =
        [
            new Transition(OrderStatus.Confirmed, [Roles.Konobar, Roles.Admin]),
            new Transition(OrderStatus.Cancelled, [Roles.Konobar, Roles.Admin], AllowOwner: true),
        ],
        [OrderStatus.Confirmed] =
        [
            new Transition(OrderStatus.InPreparation, [Roles.Kuhar, Roles.Admin]),
            new Transition(OrderStatus.Cancelled, [Roles.Konobar, Roles.Admin]),
        ],
        [OrderStatus.InPreparation] =
        [
            new Transition(OrderStatus.Ready, [Roles.Kuhar, Roles.Admin]),
        ],
        [OrderStatus.Ready] =
        [
            new Transition(OrderStatus.Delivered, [Roles.Konobar, Roles.Admin]),
        ],
        [OrderStatus.Delivered] =
        [
            new Transition(OrderStatus.Completed, [Roles.Konobar, Roles.Admin]),
        ],
    };

    private static readonly Dictionary<OrderStatus, string> Labels = new()
    {
        [OrderStatus.Pending] = "Na čekanju",
        [OrderStatus.Confirmed] = "Potvrđena",
        [OrderStatus.InPreparation] = "U pripremi",
        [OrderStatus.Ready] = "Spremna",
        [OrderStatus.Delivered] = "Isporučena",
        [OrderStatus.Completed] = "Završena",
        [OrderStatus.Cancelled] = "Otkazana",
    };

    public void Apply(Order order, OrderStatus newStatus, int actorUserId, IReadOnlyCollection<string> actorRoles, string? cancelReason = null)
    {
        if (!Graph.TryGetValue(order.Status, out var candidates))
        {
            throw new BusinessException($"Narudžba u statusu '{Label(order.Status)}' se više ne može mijenjati.");
        }

        var transition = candidates.FirstOrDefault(t => t.To == newStatus);
        if (transition is null)
        {
            var allowedNext = string.Join(", ", candidates.Select(t => Label(t.To)));
            throw new BusinessException(
                $"Nije moguće promijeniti status narudžbe iz '{Label(order.Status)}' u '{Label(newStatus)}'. " +
                $"Dozvoljeni sljedeći statusi iz '{Label(order.Status)}' su: {allowedNext}.");
        }

        var isOwner = transition.AllowOwner && order.UserId == actorUserId;
        var hasRole = transition.AllowedRoles.Any(actorRoles.Contains);
        if (!isOwner && !hasRole)
        {
            throw new ForbiddenException(
                $"Nemate ovlašćenje da promijenite status narudžbe iz '{Label(order.Status)}' u '{Label(newStatus)}'.");
        }

        order.Status = newStatus;
        var now = DateTime.UtcNow;

        switch (newStatus)
        {
            case OrderStatus.Confirmed:
                order.ConfirmedAt = now;
                break;
            case OrderStatus.InPreparation:
                order.PreparationStartedAt = now;
                break;
            case OrderStatus.Ready:
                order.ReadyAt = now;
                break;
            case OrderStatus.Delivered:
                order.DeliveredAt = now;
                break;
            case OrderStatus.Completed:
                order.CompletedAt = now;
                break;
            case OrderStatus.Cancelled:
                order.CancelledAt = now;
                order.CancelledById = actorUserId;
                order.CancelReason = cancelReason;
                break;
        }
    }

    private static string Label(OrderStatus status) => Labels.GetValueOrDefault(status, status.ToString());
}
