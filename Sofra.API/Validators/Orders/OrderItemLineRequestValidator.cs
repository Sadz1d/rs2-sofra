using FluentValidation;
using Sofra.API.Requests.Orders;

namespace Sofra.API.Validators.Orders;

public class OrderItemLineRequestValidator : AbstractValidator<OrderItemLineRequest>
{
    public OrderItemLineRequestValidator()
    {
        RuleFor(x => x.MenuItemId)
            .GreaterThan(0).WithMessage("Jelo je obavezno.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Količina mora biti veća od 0.");

        RuleFor(x => x.Note)
            .MaximumLength(300).WithMessage("Napomena može imati najviše 300 znakova.");
    }
}
