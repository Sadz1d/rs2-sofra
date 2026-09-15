using FluentValidation;
using Sofra.API.Requests.Statistics;

namespace Sofra.API.Validators.Statistics;

public class RevenueStatisticsRequestValidator : AbstractValidator<RevenueStatisticsRequest>
{
    public RevenueStatisticsRequestValidator()
    {
        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom).WithMessage("Datum 'do' mora biti isti ili poslije datuma 'od'.");

        RuleFor(x => x.Period).IsInEnum().WithMessage("Period nije validan.");
    }
}
