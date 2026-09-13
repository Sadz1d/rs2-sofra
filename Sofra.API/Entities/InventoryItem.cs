namespace Sofra.API.Entities;

public class InventoryItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int InventoryCategoryId { get; set; }
    public InventoryCategory InventoryCategory { get; set; } = null!;
    public int UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal MinQuantity { get; set; }
    public decimal? UnitCost { get; set; }
    public bool IsActive { get; set; } = true;
}
