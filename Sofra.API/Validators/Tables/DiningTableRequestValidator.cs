using FluentValidation;
using Sofra.API.Requests.Tables;

namespace Sofra.API.Validators.Tables;

public class DiningTableRequestValidator : AbstractValidator<DiningTableRequest>
{
    public DiningTableRequestValidator()
    {
        RuleFor(x => x.Number)
            .GreaterThan(0).WithMessage("Broj stola mora biti veći od 0.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Kapacitet mora biti veći od 0.");

        RuleFor(x => x.ZoneId)
            .GreaterThan(0).WithMessage("Zona je obavezna.");

        RuleFor(x => x.TableTypeId)
            .GreaterThan(0).WithMessage("Tip stola je obavezan.");

        RuleFor(x => x.WaiterId)
            .GreaterThan(0).WithMessage("Konobar mora biti validan korisnik.")
            .When(x => x.WaiterId.HasValue);

        RuleFor(x => x.QrCode)
            .MaximumLength(50).WithMessage("QR kod može imati najviše 50 znakova.")
            .When(x => !string.IsNullOrWhiteSpace(x.QrCode));
    }
}
