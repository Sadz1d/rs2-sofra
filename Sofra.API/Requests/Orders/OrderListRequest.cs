using Sofra.API.Enums;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Requests.Orders;

public class OrderListRequest : LookupListRequest
{
    public OrderStatus? Status { get; set; }
    public OrderType? Type { get; set; }
    public int? UserId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
