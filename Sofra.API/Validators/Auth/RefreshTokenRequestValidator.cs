using FluentValidation;
using Sofra.API.Requests.Auth;

namespace Sofra.API.Validators.Auth;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token je obavezan.");
    }
}
