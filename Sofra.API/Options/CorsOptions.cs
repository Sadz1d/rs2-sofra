using System.ComponentModel.DataAnnotations;

namespace Sofra.API.Options;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    [Required] public string AllowedOrigins { get; set; } = string.Empty;

    public string[] GetOrigins() =>
        AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
