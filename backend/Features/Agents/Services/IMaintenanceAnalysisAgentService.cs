using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

// Defines the interface for maintenance analysis.
public interface IMaintenanceAnalysisAgentService
{
    Task<AgentWorkflowDto?> RunAsync(Guid organizationId, Guid workflowId, CancellationToken cancellationToken);
}
