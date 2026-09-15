using Sofra.API.Enums;

namespace Sofra.API.DTOs.Reservations;

public record ReservationListItemResponse(
    int Id, int UserId, string UserName,
    DateTime ReservationAt, int DurationMinutes, int Guests,
    int ZoneId, string ZoneName,
    int? DiningTableId, int? DiningTableNumber,
    ReservationStatus Status, DateTime CreatedAt);
