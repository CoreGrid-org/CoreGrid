using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Agents.Services.Orchestration;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreGrid.Api.Features.Agents.Controllers;

[ApiController]
[Route("api/agent-workflows")]
[Authorize]
public class AgentWorkflowsController(IAgentWorkflowService workflows, CoreGridDbContext db) : CoreGridControllerBase(db)
{
    private const string ReadRoles =
        $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";

    [HttpGet]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<PagedResult<AgentWorkflowDto>>> GetWorkflows(
        [FromQuery] AgentWorkflowQueryParameters query, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null) return Unauthorized();

        return await workflows.GetWorkflowsAsync(user.OrganizationId, query, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = ReadRoles)]
    public Task<ActionResult<AgentWorkflowDto>> GetWorkflowById(Guid id, CancellationToken cancellationToken) =>
        ForWorkflowAsync(id, (organizationId, _) => workflows.GetWorkflowByIdAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpGet("{id:guid}/execution-summary")]
    [Authorize(Roles = ReadRoles)]
    public Task<ActionResult<WorkflowExecutionSummaryDto>> GetExecutionSummary(Guid id, CancellationToken cancellationToken) =>
        ForWorkflowAsync(id, (organizationId, _) => workflows.GetExecutionSummaryAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpPost]
    [Authorize(Policy = Policies.CanInitiateWorkflow)]
    [EnableRateLimiting(RateLimitPolicies.AgentWorkflowInitiate)]
    public async Task<ActionResult<AgentWorkflowDto>> CreateWorkflow(
        [FromBody] CreateAgentWorkflowRequest request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null) return Unauthorized();

        var workflow = await workflows.CreateWorkflowAsync(user.OrganizationId, user.Id, request, cancellationToken);
        return CreatedAtAction(nameof(GetWorkflowById), new { id = workflow.Id }, workflow);
    }

    [HttpPost("{id:guid}/evaluate")]
    [Authorize(Policy = Policies.CanInitiateWorkflow)]
    public Task<ActionResult<AgentWorkflowDto>> EvaluatePolicy(
        Guid id, [FromBody] EvaluatePolicyRequest request, CancellationToken cancellationToken) =>
        ForWorkflowAsync(id, (organizationId, _) => workflows.EvaluatePolicyAsync(organizationId, id, request, cancellationToken), cancellationToken);

    [HttpPost("{id:guid}/resume")]
    [Authorize(Policy = Policies.CanInitiateWorkflow)]
    public Task<ActionResult<AgentWorkflowDto>> Resume(Guid id, CancellationToken cancellationToken) =>
        ForWorkflowAsync(id, (organizationId, _) => workflows.ResumeAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpPost("{id:guid}/run-policy-agent")]
    [Authorize(Policy = Policies.CanInitiateWorkflow)]
    public Task<ActionResult<AgentWorkflowDto>> RunPolicyAgent(Guid id, CancellationToken cancellationToken) =>
        ForWorkflowAsync(id, (organizationId, _) => workflows.ResumeAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpPost("{id:guid}/run-maintenance-agent")]
    [Authorize(Policy = Policies.CanInitiateWorkflow)]
    public Task<ActionResult<AgentWorkflowDto>> RunMaintenanceAnalysisAgent(Guid id, CancellationToken cancellationToken) =>
        ForWorkflowAsync(id, (organizationId, _) =>
            workflows.RerunNodeAsync(organizationId, id, AgentNames.MaintenanceAnalysis, cancellationToken), cancellationToken);

    [HttpPost("{id:guid}/run-budget-agent")]
    [Authorize(Policy = Policies.CanInitiateWorkflow)]
    public Task<ActionResult<AgentWorkflowDto>> RunBudgetAgent(Guid id, CancellationToken cancellationToken) =>
        ForWorkflowAsync(id, (organizationId, _) =>
            workflows.RerunNodeAsync(organizationId, id, AgentNames.BudgetAnalysis, cancellationToken), cancellationToken);

    [HttpPatch("{id:guid}/decide")]
    [Authorize(Policy = Policies.CanApproveWorkflow)]
    public Task<ActionResult<AgentWorkflowDto>> Decide(
        Guid id, [FromBody] DecideWorkflowRequest request, CancellationToken cancellationToken) =>
        ForWorkflowAsync(id, (organizationId, userId) =>
            workflows.DecideAsync(organizationId, id, userId, request, cancellationToken), cancellationToken);

    private async Task<ActionResult<T>> ForWorkflowAsync<T>(
        Guid id, Func<Guid, Guid, Task<T?>> action, CancellationToken cancellationToken) where T : class
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null) return Unauthorized();

        return await action(user.OrganizationId, user.Id) ?? throw NotFoundException.For(nameof(AgentWorkflow), id);
    }
}
