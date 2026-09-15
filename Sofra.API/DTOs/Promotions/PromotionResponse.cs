using Sofra.API.Enums;

namespace Sofra.API.DTOs.Promotions;

public record PromotionResponse(
    int Id, string Name, string Code,
    DiscountType DiscountType, decimal Value, PromotionScope Scope,
    int? MenuCategoryId, string? MenuCategoryName,
    DateTime ValidFrom, DateTime ValidTo,
    int? MaxUses, int UsedCount, decimal? MinOrderAmount,
    bool IsActive);
