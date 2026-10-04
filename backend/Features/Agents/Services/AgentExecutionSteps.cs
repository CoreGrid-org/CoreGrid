using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

// Creates execution-trace rows and the one-line, human-readable summary each
// node leaves behind (the trace UI renders the structured detail itself from
// the workflow's own fields).
internal static class AgentExecutionSteps
{
    public static AgentExecutionStep Succeeded(Guid workflowId, string agent, int sequence, string summary, int durationMs) => new()
    {
        Id = Guid.NewGuid(),
        WorkflowId = workflowId,
        Agent = agent,
        Sequence = sequence,
        OutputSummary = summary,
        DurationMs = durationMs,
        Status = "SUCCESS",
        CreatedAt = DateTimeOffset.UtcNow
    };

    public static AgentExecutionStep Failed(Guid workflowId, string agent, int sequence, string error, int durationMs) => new()
    {
        Id = Guid.NewGuid(),
        WorkflowId = workflowId,
        Agent = agent,
        Sequence = sequence,
        OutputSummary = $"{Label(agent)} failed.",
        DurationMs = durationMs,
        Status = "FAILED",
        Error = error,
        CreatedAt = DateTimeOffset.UtcNow
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
        parts.Add(stats.CostTrend == "INSUFFICIENT_DATA" ? "not enough history for a cost trend" : $"cost trend {stats.CostTrend.ToLowerInvariant()}");
        parts.Add($"LKR {stats.ProjectedNextTwelveMonthsCost:N0} projected next 12 months");
        return string.Join(" · ", parts) + ".";
    }

    public static string BudgetSummary(FinancialAssessmentResultDto assessment)
    {
        var triage = assessment.Assets is { Count: > 1 }
            ? " · triage " + string.Join(", ", assessment.Assets.GroupBy(a => a.Action).OrderByDescending(g => g.Count()).Select(g => $"{g.Count()} {g.Key.ToLowerInvariant()}"))
            : "";
        var source = assessment.Source == "MODEL" ? "model-ranked" : "deterministic";
        return $"Favours {assessment.ProposedRecommendation} ({source}) · repair-to-residual {assessment.RepairToReplaceRatio:0.00} · "
            + $"LKR {assessment.ProjectedRepairCost:N0} projected vs LKR {assessment.ResidualValue:N0} residual{triage}.";
    }

    public static string Label(string agent) => agent switch
    {
        AgentNames.Planner => "Planner",
        AgentNames.MaintenanceAnalysis => "Maintenance Analysis",
        AgentNames.BudgetAnalysis => "Budget Analysis",
        AgentNames.PolicyCompliance => "Policy Compliance",
        AgentNames.DeterministicGate => "Deterministic Gate",
        _ => agent
    };

    public static int Elapsed(DateTimeOffset started) => (int)Math.Max(0, (DateTimeOffset.UtcNow - started).TotalMilliseconds);
}
