namespace Sofra.API.DTOs.Statistics;

public record RevenueStatItem(DateTime PeriodStart, int OrderCount, decimal Subtotal, decimal Tax, decimal Total);
