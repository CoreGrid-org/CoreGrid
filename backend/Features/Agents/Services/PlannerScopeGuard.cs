using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

// Ports app/scope.py from the retired planner-agent Python service.
// Pure static — no I/O, deterministic, unit-testable without any DI.
internal static class PlannerScopeGuard
{
    private static readonly string[] AllowedAgents =
    [
        "MaintenanceAnalysis", "BudgetAnalysis", "PolicyCompliance", "DeterministicGate"
    ];

    // Terms that must appear in an in-scope objective (case-insensitive).
    private static readonly string[] AllowedTerms =
    [
        "repair", "replace", "transfer", "dispose", "retain",
        "lifecycle", "maintenance", "condition", "evaluate", "evaluation"
    ];

    // Phrases that are explicitly out-of-scope regardless of allowed terms.
    private static readonly string[] RejectedPhrases =
    [
        "create user", "delete database", "change password",
        "modify policy", "change policy",
        "approve disposal", "execute disposal", "delete asset"
    ];

    // Returns null when the objective is in scope; otherwise the rejection reason.
    internal static string? RejectionReason(string objectiveText)
    {
        var text = string.Join(" ", objectiveText.ToLowerInvariant().Split(' ',
            StringSplitOptions.RemoveEmptyEntries));

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

    // Validates a plan produced by the LLM; throws on any structural violation.
    internal static PlannerExecutionPlan ValidatePlan(PlannerExecutionPlan plan)
    {
        if (!plan.InScope)
        {
            if (plan.Steps.Count > 0)
                throw new InvalidOperationException(
                    "An out-of-scope plan must not contain executable steps.");
            if (string.IsNullOrWhiteSpace(plan.RejectionReason))
                throw new InvalidOperationException(
                    "An out-of-scope plan must contain a rejection reason.");
            return plan;
        }

        if (!string.IsNullOrWhiteSpace(plan.RejectionReason))
            throw new InvalidOperationException(
                "An in-scope plan cannot contain a rejection reason.");

        if (plan.Steps.Count is < 4 or > 6)
            throw new InvalidOperationException(
                "An in-scope plan must contain 4 to 6 steps.");

        var seqs = plan.Steps.Select(s => s.Seq).ToList();
        var expected = Enumerable.Range(1, seqs.Count).ToList();
        if (!seqs.SequenceEqual(expected))
            throw new InvalidOperationException(
                "Plan steps must have consecutive sequence numbers starting at 1.");

        if (plan.Steps.Any(step =>
                !AllowedAgents.Contains(step.Agent, StringComparer.Ordinal) ||
                string.IsNullOrWhiteSpace(step.Purpose) ||
                string.IsNullOrWhiteSpace(step.ExpectedOutput)))
            throw new InvalidOperationException(
                "Plan steps must use an allowed downstream agent and include purpose and expected output.");

        return plan;
    }

    // Deterministic four-step fallback used when the LLM fails.
    internal static PlannerExecutionPlan FallbackPlan() =>
        new()
        {
            InScope = true,
            Steps =
            [
                new() { Seq = 1, Agent = "MaintenanceAnalysis",
                    Purpose = "Analyse repair history and projected maintenance cost.",
                    ExpectedOutput = "MaintenanceAnalysis" },
                new() { Seq = 2, Agent = "BudgetAnalysis",
                    Purpose = "Compare repair, replacement, residual value, and budget facts.",
                    ExpectedOutput = "FinancialAssessment" },
                new() { Seq = 3, Agent = "PolicyCompliance",
                    Purpose = "Evaluate the proposed recommendation against organisation policy.",
                    ExpectedOutput = "PolicyValidation" },
                new() { Seq = 4, Agent = "DeterministicGate",
                    Purpose = "Validate schemas, business rules, and authorisation before action.",
                    ExpectedOutput = "GateResult" },
            ]
        };
}
