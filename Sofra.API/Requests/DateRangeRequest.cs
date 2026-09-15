namespace Sofra.API.Requests;

/// <summary>Zajednicki raspon datuma za izvjestaje i statistiku.</summary>
public class DateRangeRequest
{
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
}
