using Sofra.API.Enums;

namespace Sofra.API.Requests.Promotions;

public class PromotionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal Value { get; set; }
    public PromotionScope Scope { get; set; }
    public int? MenuCategoryId { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int? MaxUses { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public bool IsActive { get; set; } = true;
}
