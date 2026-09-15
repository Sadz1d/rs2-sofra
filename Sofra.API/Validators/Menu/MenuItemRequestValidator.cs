using FluentValidation;
using Sofra.API.Requests.Menu;

namespace Sofra.API.Validators.Menu;

public class MenuItemRequestValidator : AbstractValidator<MenuItemRequest>
{
    public MenuItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naziv je obavezan.")
            .MaximumLength(100).WithMessage("Naziv može imati najviše 100 znakova.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Opis može imati najviše 1000 znakova.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Cijena mora biti veća od 0.");

        RuleFor(x => x.MenuCategoryId)
            .GreaterThan(0).WithMessage("Kategorija je obavezna.");

        RuleFor(x => x.ServingPeriods)
            .NotEqual((Sofra.API.Enums.ServingPeriod)0).WithMessage("Mora biti odabran barem jedan period posluživanja.");
    }
}
