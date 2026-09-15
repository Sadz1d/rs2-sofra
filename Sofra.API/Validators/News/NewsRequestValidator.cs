using FluentValidation;
using Sofra.API.Requests.News;

namespace Sofra.API.Validators.News;

public class NewsRequestValidator : AbstractValidator<NewsRequest>
{
    public NewsRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Naslov je obavezan.")
            .MaximumLength(150).WithMessage("Naslov može imati najviše 150 znakova.");

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Tekst je obavezan.")
            .MaximumLength(4000).WithMessage("Tekst može imati najviše 4000 znakova.");
    }
}
