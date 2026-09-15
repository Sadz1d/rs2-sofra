using Sofra.API.Requests.Catalog;

namespace Sofra.API.Requests.Users;

public class UserListRequest : LookupListRequest
{
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}
