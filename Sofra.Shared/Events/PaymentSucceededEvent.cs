namespace Sofra.Shared.Events;

/// <summary>Placanje (kartica preko Stripe webhook-a) je uspjesno zavrseno.</summary>
public record PaymentSucceededEvent(
    Guid EventId,
    DateTime OccurredAt,
    int PaymentId,
    int OrderId,
    string OrderNumber,
    int UserId,
    string UserEmail,
    string UserName,
    decimal Amount) : IEvent;
