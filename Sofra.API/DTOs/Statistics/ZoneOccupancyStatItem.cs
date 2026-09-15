namespace Sofra.API.DTOs.Statistics;

public record ZoneOccupancyStatItem(int ZoneId, string ZoneName, int ReservationCount, int TotalGuests);
