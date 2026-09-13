namespace Sofra.API.Exceptions;

public sealed class ForbiddenException(string message) : Exception(message);
