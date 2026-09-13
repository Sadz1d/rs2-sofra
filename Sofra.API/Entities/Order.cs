using Sofra.API.Enums;

namespace Sofra.API.Entities;

public class Order
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public int? DiningTableId { get; set; }
    public DiningTable? DiningTable { get; set; }
    public int? WaiterId { get; set; }
    public ApplicationUser? Waiter { get; set; }
    public OrderType Type { get; set; }
    public OrderStatus Status { get; set; }
    public string? Note { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public int? PromotionId { get; set; }
    public Promotion? Promotion { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? PreparationStartedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public int? CancelledById { get; set; }
    public ApplicationUser? CancelledBy { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Payment? Payment { get; set; }
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
