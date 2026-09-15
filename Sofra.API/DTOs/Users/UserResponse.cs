namespace Sofra.API.DTOs.Users;

public record UserResponse(
    int Id, string Username, string Email,
    string FirstName, string LastName, string? Phone,
    int? CityId, string? CityName, string? ImageUrl,
    bool IsActive, IReadOnlyList<string> Roles);
