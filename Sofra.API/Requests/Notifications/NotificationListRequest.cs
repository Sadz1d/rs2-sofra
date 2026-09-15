using Sofra.API.Requests.Catalog;

namespace Sofra.API.Requests.Notifications;

public class NotificationListRequest : LookupListRequest
{
    public bool? IsRead { get; set; }
}
