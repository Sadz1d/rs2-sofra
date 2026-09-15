namespace Sofra.API.DTOs.Reservations;

public record ReservationSlotResponse(DateTime SlotStart, DateTime SlotEnd, int AvailableTables);
