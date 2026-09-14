using FluentValidation;
using Sofra.API.Requests.Catalog;

namespace Sofra.API.Validators.Catalog;

public class ZoneRequestValidator : AbstractValidator<ZoneRequest>
{
    public ZoneRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naziv je obavezan.")
            .MaximumLength(50).WithMessage("Naziv može imati najviše 50 znakova.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Kapacitet mora biti veći od 0.");
    }
}
