namespace CoreGrid.Api.Features.Agents.DTOs;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Shared.Paging;

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
    public FailureStatisticsDto? MaintenanceAnalysis { get; set; }
    public required string CorrelationId { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public string? InitiatedByEmail { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
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
    [Required]
    public Guid? AssetId { get; set; }

    [Required, MaxLength(2000)]
    public required string Objective { get; set; }
}

// Stands in for "node 4 (Policy Compliance) plus the deterministic gate"
// (§7.2, §7.6) — until the Planner/Maintenance/Budget agents exist to
// produce a proposedRecommendation automatically, a caller supplies it
// directly, in exactly the shape those agents will eventually feed in.
public class EvaluatePolicyRequest
{
    [Required, MaxLength(50)]
    public required string ProposedRecommendation { get; set; }

    public FinancialAssessmentFacts? FinancialAssessment { get; set; }
}

// AI-13 to AI-20.
public class DecideWorkflowRequest
{
    [Required, MaxLength(20)]
    public required string Decision { get; set; } // APPROVE | REJECT | REVISE

    [Required, MaxLength(2000)]
    public required string Reason { get; set; }
}

public class AgentWorkflowQueryParameters : PagedQuery
{
    // Newest-first by default (unlike PagedQuery's own "asc" default) —
    // matches this list's previous, only ordering.
    public AgentWorkflowQueryParameters()
    {
        SortDirection = "desc";
    }

    public string? Status { get; set; }
}

// SRS §9.6 / §7.8: one row per node execution — the "agent outputs, tool
// calls" half of the execution summary's auditable trace.
public class AgentExecutionStepDto
{
    public Guid Id { get; set; }
    public required string Agent { get; set; }
    public int Sequence { get; set; }
    public string? InputHash { get; set; }
    public string? OutputSummary { get; set; }
    public int? DurationMs { get; set; }
    public required string Status { get; set; } // SUCCESS | FAILED
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// The "decision" half of the execution summary — AI-16's recorded reason,
// decider and timestamp.
public class AgentApprovalDto
{
    public Guid Id { get; set; }
    public required string Decision { get; set; } // APPROVE | REJECT | REVISE
    public Guid DecidedByUserId { get; set; }
    public string? DecidedByEmail { get; set; }
    public required string Reason { get; set; }
    public DateTimeOffset DecidedAt { get; set; }
}

// GET /api/workflows/{id}/execution-summary (SRS §9.6): "Full auditable
// trace: plan, agent outputs, tool calls, validation, decision." The first
// four are already on AgentWorkflowDto (Plan/ValidationResult/Recommendation);
// Steps and Approvals are the two collections that dto alone never exposed.
public class WorkflowExecutionSummaryDto
{
    public required AgentWorkflowDto Workflow { get; set; }
    public List<AgentExecutionStepDto> Steps { get; set; } = [];
    public List<AgentApprovalDto> Approvals { get; set; } = [];
}
