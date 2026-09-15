namespace Sofra.API.Requests.Users;

public class UpdateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int? CityId { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}
