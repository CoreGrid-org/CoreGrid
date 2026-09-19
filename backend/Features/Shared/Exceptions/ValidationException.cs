namespace CoreGrid.Api.Features.Shared.Exceptions;

// Maps to 400. For business-layer input problems that DataAnnotations model
// binding can't express (cross-field checks, checks that need a DB lookup)
// — [Required]/[Range]/etc. on the DTOs themselves already produce a 400
// via InvalidModelStateResponseFactory before a service method ever runs.
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
