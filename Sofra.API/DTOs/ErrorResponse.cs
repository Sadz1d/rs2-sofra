namespace Sofra.API.DTOs;

public sealed record ErrorResponse(string Message, IDictionary<string, string[]>? Errors = null);
