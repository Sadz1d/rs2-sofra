using Sofra.API.Enums;

namespace Sofra.API.DTOs.Payments;

public record PaymentResponse(
    int Id, int OrderId, string OrderNumber,
    int PaymentMethodId, string PaymentMethodName,
    decimal Amount, PaymentStatus Status,
    string? StripePaymentIntentId,
    decimal? RefundedAmount, string? StripeRefundId,
    DateTime? PaidAt, DateTime? RefundedAt, DateTime CreatedAt);
