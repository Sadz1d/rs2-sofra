namespace Sofra.API.Entities;

public class MenuItemStats
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
    public int OrdersLast30Days { get; set; }
    public decimal PopularityScore { get; set; }
    public decimal AvgRatingNormalized { get; set; }
    public string FeatureVectorJson { get; set; } = string.Empty;
    public DateTime ComputedAt { get; set; }
}
