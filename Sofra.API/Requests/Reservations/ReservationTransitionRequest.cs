using Sofra.API.Enums;

namespace Sofra.API.Requests.Reservations;

public class ReservationTransitionRequest
{
    public ReservationStatus Status { get; set; }
    public string? RejectReason { get; set; }
    public DateTime? AlternativeAt { get; set; }
}
