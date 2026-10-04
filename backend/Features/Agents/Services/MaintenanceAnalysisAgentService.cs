using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

// POST /run-maintenance-agent: re-runs node 2 for an in-progress workflow.
// It assembles facts only — it never proposes a recommendation or moves the
// workflow's status.
public class MaintenanceAnalysisAgentService(IAgentWorkflowService workflowService) : IMaintenanceAnalysisAgentService
{
    public Task<AgentWorkflowDto?> RunAsync(Guid organizationId, Guid workflowId, CancellationToken cancellationToken) =>
        workflowService.RerunNodeAsync(organizationId, workflowId, AgentNames.MaintenanceAnalysis, cancellationToken);
}
