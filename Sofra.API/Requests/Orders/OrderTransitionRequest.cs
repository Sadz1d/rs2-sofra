using Sofra.API.Enums;

namespace Sofra.API.Requests.Orders;

public class OrderTransitionRequest
{
    public OrderStatus Status { get; set; }
    public string? CancelReason { get; set; }
}
