using System.Text.Json;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services.Orchestration;

internal static class WorkflowMapper
{
    public static T? Read<T>(string? json) where T : class =>
        string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<T>(json);

    public static string Write<T>(T value) => JsonSerializer.Serialize(value);

    public static AgentWorkflowDto ToDto(AgentWorkflow workflow)
    {
        var budget = Read<FinancialAssessmentResultDto>(workflow.BudgetAnalysis);
        if (budget is not null) budget.Assets = null;

        return new AgentWorkflowDto
        {
            Id = workflow.Id,
            Scope = workflow.AssetId.HasValue ? "ASSET" : "ASSET_TYPE",
            AssetTypeId = workflow.AssetTypeId,
            AssetTypeName = workflow.AssetType?.Name ?? string.Empty,
            CategoryName = workflow.AssetType?.AssetCategory?.Name ?? string.Empty,
            AssetId = workflow.AssetId,
            AssetCode = workflow.Asset?.AssetCode ?? string.Empty,
            Objective = workflow.Objective,
            Status = workflow.Status.ToString(),
            Recommendation = workflow.Recommendation,
            IsHighImpact = workflow.IsHighImpact,
            ApprovalStatus = workflow.ApprovalStatus.ToString(),
            RevisionCount = workflow.RevisionCount,
            FailureReason = workflow.FailureReason,
            Plan = Read<PlannerExecutionPlan>(workflow.Plan),
            ValidationResult = Read<PolicyValidation>(workflow.ValidationResult),
            MaintenanceAnalysis = Read<FailureStatisticsDto>(workflow.MaintenanceAnalysis),
            BudgetAnalysis = budget,
            Fleet = Read<FleetEvaluationDto>(workflow.AgentOutputs),
            CorrelationId = workflow.CorrelationId,
            InitiatedByUserId = workflow.InitiatedByUserId,
            InitiatedByEmail = workflow.InitiatedByUser?.Email,
            StartedAt = workflow.StartedAt,
            CompletedAt = workflow.CompletedAt,
            CreatedAt = workflow.CreatedAt
        };
    }

    public static WorkflowExecutionSummaryDto ToExecutionSummary(AgentWorkflow workflow) => new()
    {
        Workflow = ToDto(workflow),
        Steps = workflow.Steps
            .OrderBy(s => s.Sequence)
            .ThenBy(s => s.CreatedAt)
            .Select(s => new AgentExecutionStepDto
            {
                Id = s.Id,
                Agent = s.Agent,
                Sequence = s.Sequence,
                InputHash = s.InputHash,
                OutputSummary = s.OutputSummary,
                DurationMs = s.DurationMs,
                Status = s.Status,
                Error = s.Error,
                CreatedAt = s.CreatedAt
            })
            .ToList(),
        Approvals = workflow.Approvals
            .OrderBy(a => a.DecidedAt)
            .Select(a => new AgentApprovalDto
            {
                Id = a.Id,
                Decision = a.Decision,
                DecidedByUserId = a.DecidedByUserId,
                DecidedByEmail = a.DecidedByUser?.Email,
                Reason = a.Reason,
                DecidedAt = a.DecidedAt
            })
            .ToList()
    };
}
