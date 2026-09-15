using Sofra.API.Requests.Catalog;

namespace Sofra.API.Requests.Reviews;

public class ReviewListRequest : LookupListRequest
{
    public int? MenuItemId { get; set; }
    public int? Rating { get; set; }
    public bool? HasReply { get; set; }
}
