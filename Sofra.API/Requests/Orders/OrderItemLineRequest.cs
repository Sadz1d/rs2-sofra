namespace Sofra.API.Requests.Orders;

public class OrderItemLineRequest
{
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
}
