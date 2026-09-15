using Sofra.API.Requests.Catalog;

namespace Sofra.API.Requests.Shifts;

public class ShiftListRequest : LookupListRequest
{
    public int? UserId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
