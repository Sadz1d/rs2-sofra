using Sofra.API.DTOs.Catalog;
using Sofra.API.Enums;

namespace Sofra.API.DTOs.Menu;

public record MenuItemResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    int MenuCategoryId,
    string MenuCategoryName,
    ServingPeriod ServingPeriods,
    bool IsAvailable,
    decimal? AvgRating,
    int ReviewCount,
    IReadOnlyList<AllergenResponse> Allergens,
    IReadOnlyList<DietaryTagResponse> DietaryTags);
