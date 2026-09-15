using Sofra.API.Constants;
using Sofra.API.Entities;
using Sofra.API.Enums;
using Sofra.API.Exceptions;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

/// <summary>
/// Jedino mjesto koje zna koji su prelazi ReservationStatus-a dozvoljeni i ko ih smije izvrsiti.
/// Isti obrazac kao OrderStateMachine.
/// </summary>
public class ReservationStateMachine : IReservationStateMachine
{
    private sealed record Transition(ReservationStatus To, string[] AllowedRoles, bool AllowOwner = false, bool OwnerRequiresFutureStart = false);

    private static readonly Dictionary<ReservationStatus, Transition[]> Graph = new()
    {
        [ReservationStatus.Pending] =
        [
            new Transition(ReservationStatus.Confirmed, [Roles.Konobar, Roles.Admin]),
            new Transition(ReservationStatus.Rejected, [Roles.Konobar, Roles.Admin]),
            new Transition(ReservationStatus.Cancelled, [Roles.Konobar, Roles.Admin], AllowOwner: true, OwnerRequiresFutureStart: true),
        ],
        [ReservationStatus.Confirmed] =
        [
            new Transition(ReservationStatus.Cancelled, [Roles.Konobar, Roles.Admin], AllowOwner: true, OwnerRequiresFutureStart: true),
            new Transition(ReservationStatus.NoShow, [Roles.Konobar, Roles.Admin]),
            new Transition(ReservationStatus.Completed, [Roles.Konobar, Roles.Admin]),
        ],
    };

    private static readonly Dictionary<ReservationStatus, string> Labels = new()
    {
        [ReservationStatus.Pending] = "Na čekanju",
        [ReservationStatus.Confirmed] = "Potvrđena",
        [ReservationStatus.Rejected] = "Odbijena",
        [ReservationStatus.Cancelled] = "Otkazana",
        [ReservationStatus.Completed] = "Završena",
        [ReservationStatus.NoShow] = "Nedolazak",
    };

    public void Apply(
        Reservation reservation, ReservationStatus newStatus, int actorUserId, IReadOnlyCollection<string> actorRoles,
        string? rejectReason = null, DateTime? alternativeAt = null)
    {
        if (!Graph.TryGetValue(reservation.Status, out var candidates))
        {
            throw new BusinessException($"Rezervacija u statusu '{Label(reservation.Status)}' se više ne može mijenjati.");
        }

        var transition = candidates.FirstOrDefault(t => t.To == newStatus);
        if (transition is null)
        {
            var allowedNext = string.Join(", ", candidates.Select(t => Label(t.To)));
            throw new BusinessException(
                $"Nije moguće promijeniti status rezervacije iz '{Label(reservation.Status)}' u '{Label(newStatus)}'. " +
                $"Dozvoljeni sljedeći statusi iz '{Label(reservation.Status)}' su: {allowedNext}.");
        }

        var isOwner = transition.AllowOwner && reservation.UserId == actorUserId;
        var hasRole = transition.AllowedRoles.Any(actorRoles.Contains);
        if (!isOwner && !hasRole)
        {
            throw new ForbiddenException(
                $"Nemate ovlašćenje da promijenite status rezervacije iz '{Label(reservation.Status)}' u '{Label(newStatus)}'.");
        }

        // Gost smije otkazati samo dok termin jos nije poceo; osoblje (hasRole) nema ovo ogranicenje.
        if (isOwner && !hasRole && transition.OwnerRequiresFutureStart && reservation.ReservationAt <= DateTime.UtcNow)
        {
            throw new BusinessException("Rezervaciju je moguće otkazati samo prije početka termina.");
        }

        if (newStatus == ReservationStatus.Rejected && string.IsNullOrWhiteSpace(rejectReason))
        {
            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["RejectReason"] = ["Razlog odbijanja je obavezan."],
            });
        }

        reservation.Status = newStatus;
        var now = DateTime.UtcNow;

        switch (newStatus)
        {
            case ReservationStatus.Confirmed:
            case ReservationStatus.NoShow:
            case ReservationStatus.Completed:
                reservation.ProcessedById = actorUserId;
                reservation.ProcessedAt = now;
                break;
            case ReservationStatus.Rejected:
                reservation.ProcessedById = actorUserId;
                reservation.ProcessedAt = now;
                reservation.RejectReason = rejectReason;
                reservation.AlternativeAt = alternativeAt;
                break;
            case ReservationStatus.Cancelled:
                reservation.CancelledById = actorUserId;
                reservation.CancelledAt = now;
                break;
        }
    }

    private static string Label(ReservationStatus status) => Labels.GetValueOrDefault(status, status.ToString());
}
