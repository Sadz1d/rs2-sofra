namespace Sofra.API.DTOs.Statistics;

/// <summary>Total je null za ne-Admin osoblje - finansijski podatak.</summary>
public record DashboardChartPoint(DateTime PeriodStart, int OrderCount, decimal? Total);
