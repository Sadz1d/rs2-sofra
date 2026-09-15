using Sofra.API.Enums;

namespace Sofra.API.DTOs.Orders;

public record OrderResponse(
    int Id, string Number, OrderType Type, OrderStatus Status,
    int UserId, string UserName,
    int? DiningTableId, int? DiningTableNumber,
    int? WaiterId, string? WaiterName,
    string? Note,
    decimal Subtotal, decimal Discount, decimal Tax, decimal Total,
    int? PromotionId, string? PromotionCode,
    bool IsPaid, string? PaymentMethodName,
    DateTime CreatedAt,
    DateTime? ConfirmedAt, DateTime? PreparationStartedAt, DateTime? ReadyAt,
    DateTime? DeliveredAt, DateTime? CompletedAt, DateTime? CancelledAt, string? CancelReason,
    IReadOnlyList<OrderItemResponse> Items);
