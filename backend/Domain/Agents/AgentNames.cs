namespace CoreGrid.Api.Domain;

// Defines the agent identifiers used by workflow execution.
public static class AgentNames
{
    public const string Planner = "Planner";
    public const string MaintenanceAnalysis = "MaintenanceAnalysis";
    public const string BudgetAnalysis = "BudgetAnalysis";
    public const string PolicyCompliance = "PolicyCompliance";
    public const string PolicyComplianceRecommendation = "PolicyComplianceRecommendation";
    public const string DeterministicGate = "DeterministicGate";
}
