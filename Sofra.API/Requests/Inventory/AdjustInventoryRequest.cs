using Sofra.API.Enums;

namespace Sofra.API.Requests.Inventory;

public class AdjustInventoryRequest
{
    public InventoryTransactionType Type { get; set; }
    public decimal Quantity { get; set; }
    public string? Note { get; set; }
}
