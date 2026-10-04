using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services.Orchestration;

internal static class ExecutionTrace
{
    private const string Succeeded = "SUCCESS";
    private const string Failed = "FAILED";

    public static AgentExecutionStep Success(Guid workflowId, string agent, int sequence, string summary, DateTimeOffset started) =>
        Step(workflowId, agent, sequence, Succeeded, summary, error: null, started);

    public static AgentExecutionStep Failure(Guid workflowId, string agent, int sequence, string error, DateTimeOffset started) =>
        Step(workflowId, agent, sequence, Failed, $"{Label(agent)} failed.", error, started);

    public static string Label(string agent) => agent switch
    {
        AgentNames.Planner => "Planner",
        AgentNames.MaintenanceAnalysis => "Maintenance Analysis",
        AgentNames.BudgetAnalysis => "Budget Analysis",
        AgentNames.PolicyCompliance => "Policy Compliance",
        AgentNames.DeterministicGate => "Deterministic Gate",
        _ => agent
    };

    public static string PlanSummary(PlannerExecutionPlan plan) => plan.InScope
        ? "Plan: " + string.Join(" → ", plan.Steps.OrderBy(s => s.Seq).Select(s => Label(s.Agent))) + "."
        : $"Rejected: {plan.RejectionReason}";

    public static string MaintenanceSummary(FailureStatisticsDto stats)
    {
        var parts = new List<string>();
        if (stats.AssetCount > 1) parts.Add($"{stats.AssetCount} assets, {stats.AssetsWithRepairs} with repairs");
        parts.Add($"{stats.RepairCount} repair{(stats.RepairCount == 1 ? "" : "s")}");
        if (stats.MeanTimeBetweenFailuresDays.HasValue) parts.Add($"MTBF {stats.MeanTimeBetweenFailuresDays:0.#} days");
        parts.Add(stats.CostTrend == CostTrends.InsufficientData
            ? "not enough history for a cost trend"
            : $"cost trend {stats.CostTrend.ToLowerInvariant()}");
        parts.Add($"LKR {stats.ProjectedNextTwelveMonthsCost:N0} projected next 12 months");
        return string.Join(" · ", parts) + ".";
    }

    public static string BudgetSummary(FinancialAssessmentResultDto assessment)
    {
        var triage = assessment.Assets is { Count: > 1 }
            ? " · triage " + string.Join(", ", assessment.Assets
                .GroupBy(a => a.Action)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Count()} {g.Key.ToLowerInvariant()}"))
            : "";
        var source = assessment.Source == AssessmentSources.Model ? "model-ranked" : "deterministic";

        return $"Favours {assessment.ProposedRecommendation} ({source}) · repair-to-residual {assessment.RepairToReplaceRatio:0.00} · "
            + $"LKR {assessment.ProjectedRepairCost:N0} projected vs LKR {assessment.ResidualValue:N0} residual{triage}.";
    }

    private static AgentExecutionStep Step(
        Guid workflowId, string agent, int sequence, string status, string summary, string? error, DateTimeOffset started) => new()
    {
        Id = Guid.NewGuid(),
        WorkflowId = workflowId,
        Agent = agent,
        Sequence = sequence,
        OutputSummary = summary,
        DurationMs = (int)Math.Max(0, (DateTimeOffset.UtcNow - started).TotalMilliseconds),
        Status = status,
        Error = error,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
