namespace Sofra.API.Requests.Reservations;

public class ReservationRequest
{
    public DateTime ReservationAt { get; set; }
    public int DurationMinutes { get; set; } = 120;
    public int Guests { get; set; }
    public int ZoneId { get; set; }
    public int? DiningTableId { get; set; }
    public string? Note { get; set; }
}
