using Sofra.API.Enums;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Requests.Reservations;

public class ReservationListRequest : LookupListRequest
{
    public ReservationStatus? Status { get; set; }
    public int? ZoneId { get; set; }
    public int? UserId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
