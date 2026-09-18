using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

public interface IPlannerAgentClient
{
    Task<PlannerExecutionPlan> CreatePlanAsync(
        Guid assetId,
        string objective,
        Guid initiatedBy,
        Guid organizationId,
        CancellationToken cancellationToken);
}