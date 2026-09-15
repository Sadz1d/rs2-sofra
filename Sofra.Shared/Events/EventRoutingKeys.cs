namespace Sofra.Shared.Events;

/// <summary>Routing key-evi na topic exchange-u sofra.events - dijeljeni izmedju API (publisher) i Workera (consumer bindovi).</summary>
public static class EventRoutingKeys
{
    public const string OrderStatusChanged = "order.status-changed";
    public const string ReservationProcessed = "reservation.processed";
    public const string PasswordResetRequested = "auth.password-reset-requested";
    public const string LowStockDetected = "inventory.low-stock-detected";
    public const string PaymentSucceeded = "payment.succeeded";
}
