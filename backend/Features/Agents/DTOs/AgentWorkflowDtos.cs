using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Shared.Paging;

namespace CoreGrid.Api.Features.Agents.DTOs;

public class AgentWorkflowDto
{
    public Guid Id { get; set; }
    public required string Scope { get; set; }
    public Guid AssetTypeId { get; set; }
    public required string AssetTypeName { get; set; }
    public required string CategoryName { get; set; }
    public Guid? AssetId { get; set; }
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
    public FleetEvaluationDto? Fleet { get; set; }
    public required string CorrelationId { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public string? InitiatedByEmail { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class AgentExecutionStepDto
{
    public Guid Id { get; set; }
    public required string Agent { get; set; }
    public int Sequence { get; set; }
    public string? InputHash { get; set; }
    public string? OutputSummary { get; set; }
    public int? DurationMs { get; set; }
    public required string Status { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class AgentApprovalDto
{
    public Guid Id { get; set; }
    public required string Decision { get; set; }
    public Guid DecidedByUserId { get; set; }
    public string? DecidedByEmail { get; set; }
    public required string Reason { get; set; }
    public DateTimeOffset DecidedAt { get; set; }
}

public class WorkflowExecutionSummaryDto
{
    public required AgentWorkflowDto Workflow { get; set; }
    public List<AgentExecutionStepDto> Steps { get; set; } = [];
    public List<AgentApprovalDto> Approvals { get; set; } = [];
}

public class AgentWorkflowQueryParameters : PagedQuery
{
    public AgentWorkflowQueryParameters()
    {
        SortDirection = "desc";
    }

    public string? Status { get; set; }
}
