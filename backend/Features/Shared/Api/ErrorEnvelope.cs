namespace CoreGrid.Api.Features.Shared.Api;

// One response shape for every 4xx/5xx CoreGrid returns (§5.4). `Message`
// is kept as its own top-level field — not folded into `Errors` — because
// the frontend's existing getErrorMessage() and the transfers/disposals
// handle() helpers already read `message` first and keep working unchanged
// (§7: this is a wire addition, not a wire break).
public class ErrorEnvelope
{
    public required string Message { get; init; }

    // A short machine-readable identifier for the failure (e.g.
    // "not_found", "validation_error", "asset_not_active") — stable across
    // releases, unlike Message, which is meant for humans and may be
    // reworded freely.
    public string? Code { get; init; }

    // Field-level validation failures (NFR-11): property name -> messages.
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    public string? CorrelationId { get; init; }

    // Extension payload for exceptions that carry structured context beyond
    // a message — e.g. BusinessRuleException/ForbiddenException on
    // POST /disposals/{id}/approve surfacing the P1-P6 precondition
    // snapshot under this same key the frontend already reads.
    public object? Preconditions { get; init; }
}
