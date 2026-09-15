using FluentValidation;
using Sofra.API.Requests.Auth;

namespace Sofra.API.Validators.Auth;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail je obavezan.")
            .EmailAddress().WithMessage("E-mail mora biti u ispravnom formatu (npr. ime@domena.com).");
    }
}
