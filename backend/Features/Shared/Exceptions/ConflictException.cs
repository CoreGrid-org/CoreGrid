namespace CoreGrid.Api.Features.Shared.Exceptions;

// Maps to 409. For a state-machine violation (the record exists and the
// request is individually valid, but its current status makes this
// transition illegal — e.g. approving an already-approved transfer) or a
// uniqueness collision (duplicate code). Distinct from BusinessRuleException
// (422): a conflict is about *state*, a business rule is about a
// *precondition* that state must satisfy.
public class ConflictException(string message, string? code = null) : Exception(message)
{
    public string? Code { get; } = code;
}
