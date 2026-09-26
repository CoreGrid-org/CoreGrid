namespace CoreGrid.Api.Features.Shared.Exceptions;

// A dependency the request needs (photo storage, an external service) isn't
// configured or can't be reached. Mapped to 503 with a message the user can
// act on, instead of the generic 500.
public class ServiceUnavailableException(string message, string? code = null, Exception? inner = null) : Exception(message, inner)
{
    public string? Code { get; } = code;
}
