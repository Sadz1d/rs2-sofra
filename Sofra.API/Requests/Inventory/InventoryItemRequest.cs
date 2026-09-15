namespace Sofra.API.Requests.Inventory;

public class InventoryItemRequest
{
    public string Name { get; set; } = string.Empty;
    public int InventoryCategoryId { get; set; }
    public int UnitOfMeasureId { get; set; }
    public decimal MinQuantity { get; set; }
    public decimal? UnitCost { get; set; }
    public bool IsActive { get; set; } = true;
}
