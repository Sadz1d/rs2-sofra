namespace Sofra.Shared.Events;

/// <summary>
/// Rezervisan za Stripe/placanje korak (jos nije implementiran - Payment/PaymentIntent flow ne postoji u API-ju).
/// Definisan sada da model dogadjaja bude kompletan; nista ga trenutno ne objavljuje niti konzumira.
/// </summary>
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
