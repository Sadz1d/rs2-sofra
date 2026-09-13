namespace Sofra.API.DTOs.Auth;

public record UserResponse(
    int Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string? ImageUrl,
    IReadOnlyList<string> Roles);
