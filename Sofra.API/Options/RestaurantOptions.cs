using System.ComponentModel.DataAnnotations;

namespace Sofra.API.Options;

public sealed class RestaurantOptions
{
    public const string SectionName = "Restaurant";

    [Required] public string Name { get; set; } = string.Empty;
    [Required] public TimeSpan OpenTime { get; set; }
    [Required] public TimeSpan CloseTime { get; set; }
}
