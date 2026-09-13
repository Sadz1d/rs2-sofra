using Microsoft.AspNetCore.Identity;

namespace Sofra.API.Entities;

public class ApplicationUser : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int? CityId { get; set; }
    public City? City { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
