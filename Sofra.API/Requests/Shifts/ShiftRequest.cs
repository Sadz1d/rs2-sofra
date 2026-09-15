namespace Sofra.API.Requests.Shifts;

public class ShiftRequest
{
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Note { get; set; }
}
