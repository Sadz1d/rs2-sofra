namespace Sofra.API.Options;

public sealed class RecommenderOptions
{
    public const string SectionName = "Recommender";

    public string RecalculateAtUtc { get; set; } = "03:00";
    public int ProfileCacheMinutes { get; set; } = 10;
}
