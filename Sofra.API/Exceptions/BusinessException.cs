namespace Sofra.API.Exceptions;

public sealed class BusinessException(string message) : Exception(message);
