using FluentValidation;
using Sofra.API.Requests.Reviews;

namespace Sofra.API.Validators.Reviews;

public class ReviewReplyRequestValidator : AbstractValidator<ReviewReplyRequest>
{
    public ReviewReplyRequestValidator()
    {
        RuleFor(x => x.Reply)
            .NotEmpty().WithMessage("Odgovor je obavezan.")
            .MaximumLength(1000).WithMessage("Odgovor može imati najviše 1000 znakova.");
    }
}
