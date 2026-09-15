namespace Sofra.Shared.Events;

/// <summary>Rezervacija je obradjena (potvrdjena, odbijena, otkazana, zavrsena ili nedolazak).</summary>
public record ReservationProcessedEvent(
    Guid EventId,
    DateTime OccurredAt,
    int ReservationId,
    int UserId,
    string UserEmail,
    string UserName,
    DateTime ReservationAt,
    string NewStatus,
    string? RejectReason,
    DateTime? AlternativeAt) : IEvent;
