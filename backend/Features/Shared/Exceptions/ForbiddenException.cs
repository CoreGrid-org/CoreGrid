namespace CoreGrid.Api.Features.Shared.Exceptions;

// Maps to 403. For an authenticated caller whose role/relationship to the
// record forbids this specific action — separation-of-duties (an approver
// who is also the requester), department mismatch, or similar — as
// distinct from a named authorization policy failure (also 403, handled by
// the ASP.NET Core authorization pipeline itself, never reaches this
// exception). Optionally carries a payload, e.g. disposal approval's
// preconditions snapshot.
public class ForbiddenException(string message, object? payload = null) : Exception(message)
{
    public object? Payload { get; } = payload;
}
