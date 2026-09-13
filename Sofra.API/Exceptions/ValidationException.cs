namespace Sofra.API.Exceptions;

public sealed class ValidationException(IDictionary<string, string[]> errors)
    : Exception("Jedan ili više unesenih podataka nisu ispravni.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}
