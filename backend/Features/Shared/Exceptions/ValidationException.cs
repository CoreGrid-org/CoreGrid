namespace CoreGrid.Api.Features.Shared.Exceptions;

// Represents an input validation error.
public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string field, string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]> { [field] = [message] };
    }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors, string message = "One or more fields are invalid.")
        : base(message)
    {
        Errors = errors;
    }
}
