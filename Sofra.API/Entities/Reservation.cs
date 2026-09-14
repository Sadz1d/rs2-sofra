using Sofra.API.Enums;

namespace Sofra.API.Entities;

public class Reservation
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public DateTime ReservationAt { get; set; }
    public int DurationMinutes { get; set; } = 120;
    public int Guests { get; set; }
    public int ZoneId { get; set; }
    public Zone Zone { get; set; } = null!;
    public int? DiningTableId { get; set; }
    public DiningTable? DiningTable { get; set; }
    public string? Note { get; set; }
    public ReservationStatus Status { get; set; }
    public string? RejectReason { get; set; }
    public DateTime? AlternativeAt { get; set; }
    public int? ProcessedById { get; set; }
    public ApplicationUser? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int? CancelledById { get; set; }
    public ApplicationUser? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
}
