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
    public FinancialAssessmentResultDto? BudgetAnalysis { get; set; }
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

// Defines the request for creating an agent workflow.
public class CreateAgentWorkflowRequest
{
    [Required]
    public Guid? AssetId { get; set; }

    [Required, MaxLength(2000)]
    public required string Objective { get; set; }
}

// Defines the input for policy evaluation.
public class EvaluatePolicyRequest
{
    [Required, MaxLength(50)]
    public required string ProposedRecommendation { get; set; }

    public FinancialAssessmentFacts? FinancialAssessment { get; set; }
}

// Defines the request for recording a workflow decision.
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

// Represents an individual agent execution step.
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

// Represents an approval decision for a workflow.
public class AgentApprovalDto
{
    public Guid Id { get; set; }
    public required string Decision { get; set; } // APPROVE | REJECT | REVISE
    public Guid DecidedByUserId { get; set; }
    public string? DecidedByEmail { get; set; }
    public required string Reason { get; set; }
    public DateTimeOffset DecidedAt { get; set; }
}

// GET /api/workflows/{id}/execution-summary 
// Provides the execution details and approval history for a workflow.
public class WorkflowExecutionSummaryDto
{
    public required AgentWorkflowDto Workflow { get; set; }
    public List<AgentExecutionStepDto> Steps { get; set; } = [];
    public List<AgentApprovalDto> Approvals { get; set; } = [];
}
