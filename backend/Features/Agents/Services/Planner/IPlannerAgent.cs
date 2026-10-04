using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services.Planner;

public interface IPlannerAgent
{
    Task<PlannerExecutionPlan> CreatePlanAsync(EvaluationScope scope, string objective, CancellationToken cancellationToken);
}
