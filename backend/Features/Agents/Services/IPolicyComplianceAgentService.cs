using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

public interface IPolicyComplianceAgentService
{
   // Runs policy compliance evaluation for a workflow.
    Task<AgentWorkflowDto?> RunAsync(Guid organizationId, Guid workflowId, CancellationToken cancellationToken);
}
