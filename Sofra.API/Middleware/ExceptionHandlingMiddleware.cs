using System.Text.Json;
using Sofra.API.DTOs;
using Sofra.API.Exceptions;

namespace Sofra.API.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException ex => (StatusCodes.Status422UnprocessableEntity, new ErrorResponse(ex.Message, ex.Errors)),
            NotFoundException ex => (StatusCodes.Status404NotFound, new ErrorResponse(ex.Message)),
            ForbiddenException ex => (StatusCodes.Status403Forbidden, new ErrorResponse(ex.Message)),
            BusinessException ex => (StatusCodes.Status400BadRequest, new ErrorResponse(ex.Message)),
            _ => (StatusCodes.Status500InternalServerError, new ErrorResponse("Došlo je do greške na serveru. Pokušajte ponovo kasnije.")),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Neobrađena greška pri obradi zahtjeva {Method} {Path}.", context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "{ExceptionType} pri obradi zahtjeva {Method} {Path}: {Message}",
                exception.GetType().Name, context.Request.Method, context.Request.Path, exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
