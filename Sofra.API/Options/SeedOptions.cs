using System.ComponentModel.DataAnnotations;

namespace Sofra.API.Options;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    [Required] public string DefaultPassword { get; set; } = string.Empty;
}
