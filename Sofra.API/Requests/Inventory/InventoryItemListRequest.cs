using Sofra.API.Requests.Catalog;

namespace Sofra.API.Requests.Inventory;

public class InventoryItemListRequest : LookupListRequest
{
    public int? InventoryCategoryId { get; set; }
    public bool? LowStock { get; set; }
}
