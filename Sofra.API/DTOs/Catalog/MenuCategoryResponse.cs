namespace Sofra.API.DTOs.Catalog;

public record MenuCategoryResponse(
    int Id,
    string Name,
    string? Description,
    int SortOrder,
    string? ImageUrl,
    bool IsActive);
