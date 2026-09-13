using System.ComponentModel.DataAnnotations;

namespace Sofra.API.Options;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    [Required] public string SecretKey { get; set; } = string.Empty;
    [Required] public string PublishableKey { get; set; } = string.Empty;
    [Required] public string WebhookSecret { get; set; } = string.Empty;
    [Required] public string Currency { get; set; } = "bam";
}
