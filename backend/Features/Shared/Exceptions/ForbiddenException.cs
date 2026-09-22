namespace CoreGrid.Api.Features.Shared.Exceptions;

// Represents an authorization or access restriction.
public class ForbiddenException(string message, object? payload = null) : Exception(message)
{
    public object? Payload { get; } = payload;
}
