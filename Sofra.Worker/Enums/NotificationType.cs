namespace Sofra.Worker.Enums;

/// <summary>Mora ostati u sinhronizaciji sa Sofra.API.Enums.NotificationType (isti redoslijed = ista int vrijednost u bazi).</summary>
public enum NotificationType
{
    OrderStatus,
    Payment,
    Reservation,
    Promotion,
    News,
    LowStock,
    Review,
    System,
}
