namespace CoreGrid.Api.Features.Shared.Exceptions;

// Represents a missing resource.
public class NotFoundException(string message) : Exception(message)
{
    public static NotFoundException For(string entityName, object id) =>
        new($"{entityName} '{id}' was not found.");
}
