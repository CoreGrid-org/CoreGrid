using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

public interface IAgentWorkflowService
{
    Task<List<AgentWorkflowDto>> GetWorkflowsAsync(Guid organizationId, string? status, CancellationToken cancellationToken);

    Task<AgentWorkflowDto?> GetWorkflowByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<AgentWorkflowDto> CreateWorkflowAsync(Guid organizationId, Guid userId, CreateAgentWorkflowRequest request, CancellationToken cancellationToken);

    Task<AgentWorkflowDto?> EvaluatePolicyAsync(Guid organizationId, Guid id, EvaluatePolicyRequest request, CancellationToken cancellationToken);

    Task<AgentWorkflowDto?> DecideAsync(Guid organizationId, Guid id, Guid deciderUserId, DecideWorkflowRequest request, CancellationToken cancellationToken);
}
