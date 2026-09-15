namespace Sofra.API.Requests.Reservations;

public class ReservationAvailabilityRequest
{
    public DateOnly Date { get; set; }
    public int Guests { get; set; }
    public int? ZoneId { get; set; }
    public int DurationMinutes { get; set; } = 120;
}
