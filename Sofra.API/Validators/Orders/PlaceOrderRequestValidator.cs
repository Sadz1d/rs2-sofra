using FluentValidation;
using Sofra.API.Enums;
using Sofra.API.Requests.Orders;

namespace Sofra.API.Validators.Orders;

public class PlaceOrderRequestValidator : AbstractValidator<PlaceOrderRequest>
{
    public PlaceOrderRequestValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tip narudžbe nije validan.");

        RuleFor(x => x.TableCode)
            .NotEmpty().WithMessage("QR kod stola je obavezan za narudžbu za stolom.")
            .When(x => x.Type == OrderType.DineIn);

        RuleFor(x => x.PromoCode)
            .MaximumLength(30).WithMessage("Promo kod može imati najviše 30 znakova.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Napomena može imati najviše 500 znakova.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Narudžba mora imati bar jednu stavku.");

        RuleForEach(x => x.Items).SetValidator(new OrderItemLineRequestValidator());
    }
}
