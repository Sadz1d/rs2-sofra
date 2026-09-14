using Sofra.API.Enums;

namespace Sofra.API.Entities;

public class Payment
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = null!;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public decimal? RefundedAmount { get; set; }
    public string? StripeRefundId { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}
