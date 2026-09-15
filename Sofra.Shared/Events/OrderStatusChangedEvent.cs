namespace Sofra.Shared.Events;

/// <summary>Narudzba je promijenila status. Nosi samo podatke potrebne za e-mail/notifikaciju primaocu, ne cijeli entitet.</summary>
public record OrderStatusChangedEvent(
    Guid EventId,
    DateTime OccurredAt,
    int OrderId,
    string OrderNumber,
    int UserId,
    string UserEmail,
    string UserName,
    string OldStatus,
    string NewStatus,
    string? CancelReason) : IEvent;
