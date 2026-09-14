using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

public interface IPolicyComplianceAgentService
{
    // Runs the Policy Compliance Agent's recommendation node end to end for
    // an in-flight workflow: assembles facts via the agent's own tool
    // allow-list, runs the deterministic heuristic to propose a
    // recommendation, then hands that recommendation to the existing
    // deterministic gate (IAgentWorkflowService.EvaluatePolicyAsync) exactly
    // as a human using "Evaluate policy compliance" would have. No LLM is
    // involved anywhere in this path — see AssetActionRecommendationEngine.
    Task<AgentWorkflowDto?> RunAsync(Guid organizationId, Guid workflowId, CancellationToken cancellationToken);
}
