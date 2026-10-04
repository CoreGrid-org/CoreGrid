using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services.Planner;

internal static class PlannerScopeGuard
{
    private static readonly string[] AllowedTerms =
    [
        "repair", "replace", "transfer", "dispose", "retain",
        "lifecycle", "maintenance", "condition", "evaluate", "evaluation"
    ];

    private static readonly string[] RejectedPhrases =
    [
        "create user", "delete database", "change password",
        "modify policy", "change policy",
        "approve disposal", "execute disposal", "delete asset"
    ];

    internal static string? RejectionReason(string objective)
    {
        var text = string.Join(" ", objective.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (string.IsNullOrWhiteSpace(text))
            return "An evaluation objective is required.";

        if (RejectedPhrases.Any(text.Contains))
            return "The Planner only plans asset lifecycle evaluation; it cannot " +
                   "administer users, policies, databases, or approve actions.";

        if (!AllowedTerms.Any(text.Contains))
            return "The objective is outside asset lifecycle evaluation scope.";

        return null;
    }

    internal static PlannerExecutionPlan RejectedPlan(string reason) =>
        new() { InScope = false, RejectionReason = reason, Steps = [] };

    internal static PlannerExecutionPlan FallbackPlan() =>
        new() { InScope = true, Steps = AgentRegistry.Agents.Select((a, i) => Step(a.Name, null, i + 1)).ToList() };

    internal static PlannerExecutionPlan ValidatePlan(PlannerExecutionPlan plan)
    {
        if (!plan.InScope)
        {
            if (plan.Steps.Count > 0)
                throw new InvalidOperationException("An out-of-scope plan must not contain executable steps.");
            if (string.IsNullOrWhiteSpace(plan.RejectionReason))
                throw new InvalidOperationException("An out-of-scope plan must contain a rejection reason.");
            return plan;
        }

        if (!string.IsNullOrWhiteSpace(plan.RejectionReason))
            throw new InvalidOperationException("An in-scope plan cannot contain a rejection reason.");

        if (plan.Steps.Any(step => AgentRegistry.Find(step.Agent) is null))
            throw new InvalidOperationException("Plan steps must only use agents from the registry.");

        var ordered = plan.Steps
            .OrderBy(s => s.Seq)
            .DistinctBy(s => s.Agent)
            .Where(s => s.Agent != AgentNames.DeterministicGate)
            .ToList();

        EnsureEveryAnalysisAgentIsScheduled(ordered);
        EnsureDependenciesAreRespected(ordered);

        var gate = plan.Steps.FirstOrDefault(s => s.Agent == AgentNames.DeterministicGate);
        return new PlannerExecutionPlan
        {
            InScope = true,
            Steps = ordered.Append(gate ?? Step(AgentNames.DeterministicGate, null))
                .Select((s, i) => Step(s.Agent, s.Purpose, i + 1))
                .ToList()
        };
    }

    private static void EnsureEveryAnalysisAgentIsScheduled(List<PlannerPlanStep> steps)
    {
        var required = AgentRegistry.Agents.Select(a => a.Name).Where(name => name != AgentNames.DeterministicGate).ToList();
        if (!required.All(name => steps.Any(s => s.Agent == name)))
            throw new InvalidOperationException($"An in-scope plan must schedule {string.Join(", ", required)}.");
    }

    private static void EnsureDependenciesAreRespected(List<PlannerPlanStep> steps)
    {
        var position = steps.Select((s, i) => (s.Agent, i)).ToDictionary(x => x.Agent, x => x.i);
        foreach (var step in steps)
        {
            var dependsOn = AgentRegistry.Find(step.Agent)!.DependsOn;
            if (dependsOn is not null && position.TryGetValue(dependsOn, out var dependencyIndex) && dependencyIndex > position[step.Agent])
                throw new InvalidOperationException($"{step.Agent} must run after {dependsOn}.");
        }
    }

    private static PlannerPlanStep Step(string agent, string? purpose, int seq = 0)
    {
        var spec = AgentRegistry.Find(agent)!;
        return new PlannerPlanStep
        {
            Seq = seq,
            Agent = agent,
            Purpose = string.IsNullOrWhiteSpace(purpose) ? spec.DefaultPurpose : purpose.Trim(),
            ExpectedOutput = spec.Output
        };
    }
}
