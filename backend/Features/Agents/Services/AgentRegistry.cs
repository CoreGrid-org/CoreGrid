using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Agents.Services;

// The downstream agents the Planner can schedule, their read-only tool
// allow-lists (§7.4), what each produces and what it depends on. The Planner's
// prompt, plan validation and the deterministic fallback plan are all built
// from this one table, so they can't drift apart.
internal static class AgentRegistry
{
    internal sealed record AgentSpec(string Name, string[] Tools, string Output, string? DependsOn, string DefaultPurpose);

    internal static readonly AgentSpec[] Agents =
    [
        new(AgentNames.MaintenanceAnalysis,
            ["get_maintenance_history", "compute_failure_statistics"],
            "MaintenanceAnalysis", null,
            "Quantify repair history, cost trend and 12-month cost per asset."),
        new(AgentNames.BudgetAnalysis,
            ["get_asset_financials", "get_department_budget_summary", "compute_depreciation"],
            "FinancialAssessment", AgentNames.MaintenanceAnalysis,
            "Compare repair cost with residual value and rank lifecycle actions."),
        new(AgentNames.PolicyCompliance,
            ["get_organization_policies", "get_asset_compliance_state"],
            "PolicyValidation", AgentNames.BudgetAnalysis,
            "Pick the best policy-permitted action per asset and validate it."),
        new(AgentNames.DeterministicGate,
            [],
            "GateResult", AgentNames.PolicyCompliance,
            "Route to approval, advisory completion or safe failure."),
    ];

    internal static AgentSpec? Find(string name) => Agents.FirstOrDefault(a => a.Name == name);

    // One line per agent — the whole catalogue costs the Planner ~100 tokens.
    internal static string Catalogue() => string.Join("\n", Agents.Select(a =>
        $"{a.Name}: tools[{string.Join(",", a.Tools)}] -> {a.Output}" + (a.DependsOn is null ? "" : $" (after {a.DependsOn})")));
}
