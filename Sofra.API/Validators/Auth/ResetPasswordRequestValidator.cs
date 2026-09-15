using FluentValidation;
using Sofra.API.Requests.Auth;

namespace Sofra.API.Validators.Auth;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail je obavezan.")
            .EmailAddress().WithMessage("E-mail mora biti u ispravnom formatu (npr. ime@domena.com).");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Kod je obavezan.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nova lozinka je obavezna.")
            .MinimumLength(8).WithMessage("Lozinka mora imati najmanje 8 znakova.")
            .Matches("[A-Z]").WithMessage("Lozinka mora sadržavati najmanje jedno veliko slovo.")
            .Matches("[a-z]").WithMessage("Lozinka mora sadržavati najmanje jedno malo slovo.")
            .Matches("[0-9]").WithMessage("Lozinka mora sadržavati najmanje jednu cifru.");
    }
}
