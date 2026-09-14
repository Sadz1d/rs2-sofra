using FluentValidation;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Validators.Catalog;

public class UnitOfMeasureRequestValidator : AbstractValidator<UnitOfMeasureRequest>
{
    public UnitOfMeasureRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naziv je obavezan.")
            .MaximumLength(50).WithMessage("Naziv može imati najviše 50 znakova.");

        RuleFor(x => x.Abbreviation)
            .NotEmpty().WithMessage("Skraćenica je obavezna.")
            .MaximumLength(10).WithMessage("Skraćenica može imati najviše 10 znakova.");
    }
}
