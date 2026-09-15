using FluentValidation;
using Sofra.API.Requests;

namespace Sofra.API.Validators;

public class DateRangeRequestValidator : AbstractValidator<DateRangeRequest>
{
    public DateRangeRequestValidator()
    {
        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom).WithMessage("Datum 'do' mora biti isti ili poslije datuma 'od'.");
    }
}
