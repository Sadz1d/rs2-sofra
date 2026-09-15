using FluentValidation;
using Sofra.API.Requests.Reviews;

namespace Sofra.API.Validators.Reviews;

public class ReviewRequestValidator : AbstractValidator<ReviewRequest>
{
    public ReviewRequestValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("Narudžba je obavezna.");
        RuleFor(x => x.MenuItemId).GreaterThan(0).WithMessage("Jelo je obavezno.");
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("Ocjena mora biti između 1 i 5.");
        RuleFor(x => x.Comment).MaximumLength(1000).WithMessage("Komentar može imati najviše 1000 znakova.");
    }
}
