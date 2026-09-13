using System.ComponentModel.DataAnnotations;

namespace Sofra.API.Options;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    [Required, Url] public string BaseUrl { get; set; } = string.Empty;
}
