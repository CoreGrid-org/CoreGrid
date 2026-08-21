namespace CoreGrid.Api.Features.Agents.DTOs;

public class AgentWorkflowDto
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public required string AssetCode { get; set; }
    public required string Objective { get; set; }
    public required string Status { get; set; }
    public string? Recommendation { get; set; }
    public bool IsHighImpact { get; set; }
    public required string ApprovalStatus { get; set; }
    public int RevisionCount { get; set; }
    public string? FailureReason { get; set; }
    public PolicyValidation? ValidationResult { get; set; }
    public required string CorrelationId { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public string? InitiatedByEmail { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// FR-067/FR-068.
public class CreateAgentWorkflowRequest
{
    public Guid AssetId { get; set; }
    public required string Objective { get; set; }
}

// Stands in for "node 4 (Policy Compliance) plus the deterministic gate"
// (§7.2, §7.6) — until the Planner/Maintenance/Budget agents exist to
// produce a proposedRecommendation automatically, a caller supplies it
// directly, in exactly the shape those agents will eventually feed in.
public class EvaluatePolicyRequest
{
    public required string ProposedRecommendation { get; set; }
    public FinancialAssessmentFacts? FinancialAssessment { get; set; }
}

// AI-13 to AI-20.
public class DecideWorkflowRequest
{
    public required string Decision { get; set; } // APPROVE | REJECT | REVISE
    public required string Reason { get; set; }
}
