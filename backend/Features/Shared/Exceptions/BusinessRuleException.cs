namespace CoreGrid.Api.Features.Shared.Exceptions;

// Maps to 422. For a request that is well-formed and individually valid but
// violates a documented business rule (BR1-BR3, P1-P6, PR-01-PR-09) given
// the record's current state. Optionally carries a machine-readable
// payload — e.g. the failed disposal preconditions P1-P6 — surfaced by
// ApiExceptionFilter as the error envelope's `preconditions` extension.
public class BusinessRuleException(string message, string? code = null, object? payload = null) : Exception(message)
{
    public string? Code { get; } = code;

    public object? Payload { get; } = payload;
}
