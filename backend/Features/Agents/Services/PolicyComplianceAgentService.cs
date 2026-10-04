using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

// POST /run-policy-agent: node 4 now runs inside the orchestrated pipeline
// (PolicyComplianceEvaluator), so running it on demand means resuming the
// workflow's remaining plan — Policy Compliance and then the gate.
public class PolicyComplianceAgentService(IAgentWorkflowService workflowService) : IPolicyComplianceAgentService
{
    public Task<AgentWorkflowDto?> RunAsync(Guid organizationId, Guid workflowId, CancellationToken cancellationToken) =>
        workflowService.ResumeAsync(organizationId, workflowId, cancellationToken);
}
