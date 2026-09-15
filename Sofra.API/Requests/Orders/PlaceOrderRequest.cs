using Sofra.API.Enums;

namespace Sofra.API.Requests.Orders;

public class PlaceOrderRequest
{
    public OrderType Type { get; set; }

    /// <summary>QR kod stola - obavezan za Type = DineIn, ignoriše se za Takeaway.</summary>
    public string? TableCode { get; set; }

    public string? PromoCode { get; set; }
    public string? Note { get; set; }
    public List<OrderItemLineRequest> Items { get; set; } = [];
}
