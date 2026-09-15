using FluentValidation;
using Sofra.API.Requests.Orders;

namespace Sofra.API.Validators.Orders;

public class OrderTransitionRequestValidator : AbstractValidator<OrderTransitionRequest>
{
    public OrderTransitionRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status narudžbe nije validan.");

        RuleFor(x => x.CancelReason)
            .MaximumLength(500).WithMessage("Razlog otkazivanja može imati najviše 500 znakova.");
    }
}
