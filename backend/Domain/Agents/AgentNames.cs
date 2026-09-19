namespace CoreGrid.Api.Domain;

// AgentExecutionStep.Agent identifiers (Phase 4, §6.1) — each one written
// by more than one call site (PlannerScopeGuard's deterministic plan and
// the node that actually executes it), so they need to agree exactly.
public static class AgentNames
{
    public const string Planner = "Planner";
    public const string MaintenanceAnalysis = "MaintenanceAnalysis";
    public const string BudgetAnalysis = "BudgetAnalysis";
    public const string PolicyCompliance = "PolicyCompliance";
    public const string PolicyComplianceRecommendation = "PolicyComplianceRecommendation";
    public const string DeterministicGate = "DeterministicGate";
}
