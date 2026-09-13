using Sofra.API.Enums;

namespace Sofra.API.Entities;

public class InventoryTransaction
{
    public int Id { get; set; }
    public int InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; } = null!;
    public InventoryTransactionType Type { get; set; }
    public decimal Quantity { get; set; }
    public string? Note { get; set; }
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public int CreatedById { get; set; }
    public ApplicationUser CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
