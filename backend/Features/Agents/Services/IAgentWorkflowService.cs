using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Shared;

namespace CoreGrid.Api.Features.Agents.Services;

public interface IAgentWorkflowService
{
    Task<PagedResult<AgentWorkflowDto>> GetWorkflowsAsync(Guid organizationId, AgentWorkflowQueryParameters query, CancellationToken cancellationToken);

    Task<AgentWorkflowDto?> GetWorkflowByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

   
    // tool calls, validation, decision — for one workflow.
    Task<WorkflowExecutionSummaryDto?> GetExecutionSummaryAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<AgentWorkflowDto> CreateWorkflowAsync(Guid organizationId, Guid userId, CreateAgentWorkflowRequest request, CancellationToken cancellationToken);

    Task<AgentWorkflowDto?> EvaluatePolicyAsync(Guid organizationId, Guid id, EvaluatePolicyRequest request, CancellationToken cancellationToken);

    Task<AgentWorkflowDto?> DecideAsync(Guid organizationId, Guid id, Guid deciderUserId, DecideWorkflowRequest request, CancellationToken cancellationToken);
}
