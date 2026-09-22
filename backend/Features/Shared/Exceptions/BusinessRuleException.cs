namespace CoreGrid.Api.Features.Shared.Exceptions;

// Represents a business rule violation.
public class BusinessRuleException(string message, string? code = null, object? payload = null) : Exception(message)
{
    public string? Code { get; } = code;

    public object? Payload { get; } = payload;
}
