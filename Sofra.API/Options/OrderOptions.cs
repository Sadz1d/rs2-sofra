using System.ComponentModel.DataAnnotations;

namespace Sofra.API.Options;

public sealed class OrderOptions
{
    public const string SectionName = "Order";

    [Range(0, 100)] public decimal TaxRatePercent { get; set; } = 17;
}
