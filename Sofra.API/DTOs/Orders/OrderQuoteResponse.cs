namespace Sofra.API.DTOs.Orders;

public record OrderQuoteResponse(
    decimal Subtotal, decimal Discount, decimal Tax, decimal Total, decimal TaxRatePercent,
    string? PromotionCode, IReadOnlyList<OrderQuoteItemResponse> Items);
