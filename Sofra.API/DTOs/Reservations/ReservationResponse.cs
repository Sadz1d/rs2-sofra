using Sofra.API.Enums;

namespace Sofra.API.DTOs.Reservations;

public record ReservationResponse(
    int Id, int UserId, string UserName,
    DateTime ReservationAt, int DurationMinutes, int Guests,
    int ZoneId, string ZoneName,
    int? DiningTableId, int? DiningTableNumber,
    string? Note, ReservationStatus Status,
    string? RejectReason, DateTime? AlternativeAt,
    int? ProcessedById, string? ProcessedByName, DateTime? ProcessedAt,
    int? CancelledById, string? CancelledByName, DateTime? CancelledAt,
    DateTime CreatedAt);
