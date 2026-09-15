using FluentValidation;
using Sofra.API.Requests.Users;

namespace Sofra.API.Validators.Users;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ime je obavezno.")
            .MaximumLength(100).WithMessage("Ime može imati najviše 100 znakova.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Prezime je obavezno.")
            .MaximumLength(100).WithMessage("Prezime može imati najviše 100 znakova.");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9\s\-]{6,30}$").WithMessage("Broj telefona nije u ispravnom formatu.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
