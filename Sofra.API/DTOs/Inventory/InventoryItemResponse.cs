namespace Sofra.API.DTOs.Inventory;

public record InventoryItemResponse(
    int Id, string Name,
    int InventoryCategoryId, string InventoryCategoryName,
    int UnitOfMeasureId, string UnitOfMeasureName, string UnitOfMeasureAbbreviation,
    decimal Quantity, decimal MinQuantity, decimal? UnitCost,
    bool IsLowStock, bool IsActive);
