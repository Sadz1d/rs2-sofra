using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Sofra.API.Filters;

/// <summary>
/// Automatski validira svaki action parametar koji ima registrovan IValidator&lt;T&gt;,
/// prije nego sto kontroler-akcija uopste pocne izvrsavanje. Neispravan unos baca
/// Sofra.API.Exceptions.ValidationException koju hvata ExceptionHandlingMiddleware.
/// </summary>
public class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                throw new Exceptions.ValidationException(errors);
            }
        }

        await next();
    }
}
