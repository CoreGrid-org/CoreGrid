using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

public interface IPlannerAgentClient
{
    Task<PlannerExecutionPlan> CreatePlanAsync(
        EvaluationScope scope,
        string objective,
        Guid initiatedBy,
        CancellationToken cancellationToken);
}
