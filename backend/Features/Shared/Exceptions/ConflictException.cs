namespace CoreGrid.Api.Features.Shared.Exceptions;

// Represents a state or uniqueness conflict.
public class ConflictException(string message, string? code = null) : Exception(message)
{
    public string? Code { get; } = code;
}
