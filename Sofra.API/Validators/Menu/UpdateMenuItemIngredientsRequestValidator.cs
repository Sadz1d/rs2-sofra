using FluentValidation;
using Sofra.API.Requests.Menu;

namespace Sofra.API.Validators.Menu;

public class UpdateMenuItemIngredientsRequestValidator : AbstractValidator<UpdateMenuItemIngredientsRequest>
{
    public UpdateMenuItemIngredientsRequestValidator()
    {
        RuleForEach(x => x.Ingredients).ChildRules(line =>
        {
            line.RuleFor(x => x.InventoryItemId)
                .GreaterThan(0).WithMessage("Namirnica je obavezna.");

            line.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Količina mora biti veća od 0.");
        });

        RuleFor(x => x.Ingredients)
            .Must(list => list.Select(x => x.InventoryItemId).Distinct().Count() == list.Count)
            .WithMessage("Ista namirnica se ne može navesti dva puta u normativu.");
    }
}
