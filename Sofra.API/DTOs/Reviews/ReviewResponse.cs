namespace Sofra.API.DTOs.Reviews;

public record ReviewResponse(
    int Id, int OrderId, int UserId, string UserName,
    int? MenuItemId, string? MenuItemName,
    int Rating, string? Comment,
    string? Reply, int? RepliedById, string? RepliedByName, DateTime? RepliedAt,
    DateTime CreatedAt);
