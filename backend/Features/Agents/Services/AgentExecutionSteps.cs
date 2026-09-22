using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

// Creates execution steps for maintenance analysis.
internal static class AgentExecutionSteps
{
    public static AgentExecutionStep MaintenanceAnalysisSucceeded(Guid workflowId, FailureStatisticsDto stats, int durationMs, DateTimeOffset createdAt) => new()
    {
        Id = Guid.NewGuid(),
        WorkflowId = workflowId,
        Agent = AgentNames.MaintenanceAnalysis,
        Sequence = 2,
        OutputSummary = $"RepairCount={stats.RepairCount}, CostTrend={stats.CostTrend}, "
            + $"Projected12moCost={stats.ProjectedNextTwelveMonthsCost}",
        DurationMs = durationMs,
        Status = "SUCCESS",
        CreatedAt = createdAt
    };

    public static AgentExecutionStep MaintenanceAnalysisFailed(Guid workflowId, string error, int durationMs, DateTimeOffset createdAt) => new()
    {
        Id = Guid.NewGuid(),
        WorkflowId = workflowId,
        Agent = AgentNames.MaintenanceAnalysis,
        Sequence = 2,
        OutputSummary = "Maintenance analysis failed.",
        DurationMs = durationMs,
        Status = "FAILED",
        Error = error,
        CreatedAt = createdAt
    };
}
