using Sofra.API.Enums;

namespace Sofra.API.Requests.Statistics;

public class RevenueStatisticsRequest : DateRangeRequest
{
    public StatisticsPeriod Period { get; set; } = StatisticsPeriod.Day;
}
