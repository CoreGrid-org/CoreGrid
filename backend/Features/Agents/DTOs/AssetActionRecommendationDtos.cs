namespace CoreGrid.Api.Features.Agents.DTOs;

// Output of the Policy Compliance Agent's recommendation step (SRS §7.3,
// node 4) — advisory only, produced by a deterministic heuristic (no LLM;
// see AssetActionRecommendationEngine). PolicyRuleEngine (§7.6), not this,
// decides PASS/FAIL/NEEDS_REVISION. Rationale is stored on the workflow's
// execution trail for audit but never fed back into the deterministic
// evaluation itself.
public record AssetActionRecommendation(string Recommendation, string Rationale);
