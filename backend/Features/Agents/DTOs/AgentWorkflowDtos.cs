namespace CoreGrid.Api.Features.Agents.DTOs;

using System.Text.Json.Serialization;

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
    public PlannerExecutionPlan? Plan { get; set; }
    public PolicyValidation? ValidationResult { get; set; }
    public required string CorrelationId { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public string? InitiatedByEmail { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class PlannerObjectiveRequest
{
    [JsonPropertyName("asset_id")]
    public Guid AssetId { get; set; }

    [JsonPropertyName("objective_text")]
    public required string ObjectiveText { get; set; }

    [JsonPropertyName("initiated_by")]
    public Guid InitiatedBy { get; set; }

    [JsonPropertyName("organization_id")]
    public Guid OrganizationId { get; set; }
}

public class PlannerExecutionPlan
{
    [JsonPropertyName("inScope")]
    public bool InScope { get; set; }

    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }

    [JsonPropertyName("steps")]
    public List<PlannerPlanStep> Steps { get; set; } = [];
}

public class PlannerPlanStep
{
    [JsonPropertyName("seq")]
    public int Seq { get; set; }

    [JsonPropertyName("agent")]
    public required string Agent { get; set; }

    [JsonPropertyName("purpose")]
    public required string Purpose { get; set; }

    [JsonPropertyName("expectedOutput")]
    public required string ExpectedOutput { get; set; }
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
