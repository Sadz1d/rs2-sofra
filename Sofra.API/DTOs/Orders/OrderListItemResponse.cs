using Sofra.API.Enums;

namespace Sofra.API.DTOs.Orders;

public record OrderListItemResponse(
    int Id, string Number, OrderType Type, OrderStatus Status,
    int UserId, string UserName,
    int? DiningTableId, int? DiningTableNumber,
    decimal Total, bool IsPaid, DateTime CreatedAt, int ItemCount);
