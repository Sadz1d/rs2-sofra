using FluentValidation;
using Sofra.API.Requests.Shifts;

namespace Sofra.API.Validators.Shifts;

public class ShiftRequestValidator : AbstractValidator<ShiftRequest>
{
    public ShiftRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("Zaposlenik je obavezan.");
        RuleFor(x => x.Date).NotEqual(default(DateOnly)).WithMessage("Datum je obavezan.");
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).WithMessage("Kraj smjene mora biti poslije početka.");
        RuleFor(x => x.Note).MaximumLength(200).WithMessage("Napomena može imati najviše 200 znakova.");
    }
}
