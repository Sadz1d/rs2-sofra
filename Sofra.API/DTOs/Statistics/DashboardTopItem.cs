namespace Sofra.API.DTOs.Statistics;

/// <summary>Revenue je null za ne-Admin osoblje - finansijski podatak. QuantitySold ostaje vidljiv svima.</summary>
public record DashboardTopItem(int MenuItemId, string MenuItemName, int QuantitySold, decimal? Revenue);
