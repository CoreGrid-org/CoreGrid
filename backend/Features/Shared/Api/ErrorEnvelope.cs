namespace CoreGrid.Api.Features.Shared.Api;

// Defines the standard API error response.
public class ErrorEnvelope
{
    public required string Message { get; init; }

 // Provides a machine-readable error identifier.
    public string? Code { get; init; }

    // Contains field-level validation errors.
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    public string? CorrelationId { get; init; }

    // Contains additional context for business rule failures.
    public object? Preconditions { get; init; }
}
