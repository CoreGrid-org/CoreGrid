using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Shared;

namespace CoreGrid.Api.Features.Agents.Services;

public interface IAgentWorkflowService
{
    Task<PagedResult<AgentWorkflowDto>> GetWorkflowsAsync(Guid organizationId, AgentWorkflowQueryParameters query, CancellationToken cancellationToken);

    Task<AgentWorkflowDto?> GetWorkflowByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    // Full auditable trace (SRS §9.6): plan, agent outputs, tool calls,
    // validation, decision — for one workflow.
    Task<WorkflowExecutionSummaryDto?> GetExecutionSummaryAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    // Creates the workflow and runs the Planner's plan end to end, so it
    // returns already routed: awaiting approval, advisory, or safely failed.
    Task<AgentWorkflowDto> CreateWorkflowAsync(Guid organizationId, Guid userId, CreateAgentWorkflowRequest request, CancellationToken cancellationToken);

    // Runs whatever the plan still has left for a workflow stuck in
    // PLANNING/ANALYZING/VALIDATING (e.g. created before the pipeline ran
    // end to end, or interrupted).
    Task<AgentWorkflowDto?> ResumeAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    // Re-runs one analysis node (Maintenance or Budget) and records it, without routing.
    Task<AgentWorkflowDto?> RerunNodeAsync(Guid organizationId, Guid id, string agent, CancellationToken cancellationToken);

    Task<AgentWorkflowDto?> EvaluatePolicyAsync(Guid organizationId, Guid id, EvaluatePolicyRequest request, CancellationToken cancellationToken);

    Task<AgentWorkflowDto?> RunBudgetAnalysisAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<AgentWorkflowDto?> DecideAsync(Guid organizationId, Guid id, Guid deciderUserId, DecideWorkflowRequest request, CancellationToken cancellationToken);
}
