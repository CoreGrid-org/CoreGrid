namespace CoreGrid.Api.Features.Shared.Exceptions;

// Maps to 404. Thrown by a service when the caller asked for a specific
// record (by id) that does not exist, or does not exist within the
// caller's organisation — the two cases are deliberately indistinguishable
// to the client (SEC-ID-02: existence of another organisation's row is not
// something to disclose).
public class NotFoundException(string message) : Exception(message)
{
    public static NotFoundException For(string entityName, object id) =>
        new($"{entityName} '{id}' was not found.");
}
