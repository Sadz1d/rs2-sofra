namespace Sofra.API.Entities;

/// <summary>
/// Idempotentnost Stripe webhook-a - isti Stripe event Id (string, npr. "evt_...") ne smije dva puta
/// promijeniti stanje. Zaseban od ProcessedEvent (koji koristi Worker za nase interne dogadjaje sa Guid
/// EventId-em) jer Stripe event Id-jevi nisu Guid-ovi.
/// </summary>
public class ProcessedStripeEvent
{
    public string StripeEventId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
