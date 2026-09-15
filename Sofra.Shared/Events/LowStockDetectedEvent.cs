namespace Sofra.Shared.Events;

/// <summary>Stanje namirnice je palo na ili ispod minimuma. AdminUserIds su unaprijed razrijeseni u API-ju (Worker ne zna za uloge).</summary>
public record LowStockDetectedEvent(
    Guid EventId,
    DateTime OccurredAt,
    int InventoryItemId,
    string InventoryItemName,
    decimal Quantity,
    decimal MinQuantity,
    string UnitAbbreviation,
    IReadOnlyList<int> AdminUserIds) : IEvent;
