using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Agents.Services;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreGrid.Api.Features.Agents.Controllers;

// Handles asset lifecycle decision workflows.
[ApiController]
[Route("api/agent-workflows")]
[Authorize]
public class AgentWorkflowsController : CoreGridControllerBase
{
    // Roles allowed to view workflow details.
    private const string ReadRoles = $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";

    private readonly IAgentWorkflowService _workflowService;
    private readonly IPolicyComplianceAgentService _policyComplianceAgent;
    private readonly IMaintenanceAnalysisAgentService _maintenanceAnalysisAgent;

    public AgentWorkflowsController(
        IAgentWorkflowService workflowService,
        IPolicyComplianceAgentService policyComplianceAgent,
        IMaintenanceAnalysisAgentService maintenanceAnalysisAgent,
        CoreGridDbContext db) : base(db)
    {
        _workflowService = workflowService;
        _policyComplianceAgent = policyComplianceAgent;
        _maintenanceAnalysisAgent = maintenanceAnalysisAgent;
    }

    [HttpGet]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<PagedResult<AgentWorkflowDto>>> GetWorkflows(
        [FromQuery] AgentWorkflowQueryParameters query, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        return Ok(await _workflowService.GetWorkflowsAsync(currentUser.OrganizationId, query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<AgentWorkflowDto>> GetWorkflowById(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var workflow = await _workflowService.GetWorkflowByIdAsync(currentUser.OrganizationId, id, cancellationToken);
        return workflow is null
            ? throw NotFoundException.For(nameof(AgentWorkflow), id)
            : Ok(workflow);
    }

    // GET /api/agent-workflows/{id}/execution-summary 
    // Returns the execution summary for a workflow.

    [HttpGet("{id:guid}/execution-summary")]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<WorkflowExecutionSummaryDto>> GetExecutionSummary(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var summary = await _workflowService.GetExecutionSummaryAsync(currentUser.OrganizationId, id, cancellationToken);
        return summary is null
            ? throw NotFoundException.For(nameof(AgentWorkflow), id)
            : Ok(summary);
    }

    // Initiates a new asset lifecycle evaluation.
    [HttpPost]
    [Authorize(Policy = Policies.CanInitiateWorkflow)]
    [EnableRateLimiting(RateLimitPolicies.AgentWorkflowInitiate)]
    public async Task<ActionResult<AgentWorkflowDto>> CreateWorkflow(
        [FromBody] CreateAgentWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var workflow = await _workflowService.CreateWorkflowAsync(currentUser.OrganizationId, currentUser.Id, request, cancellationToken);
        return CreatedAtAction(nameof(GetWorkflowById), new { id = workflow.Id }, workflow);
    }

    // Evaluates the workflow against applicable policies.
    [HttpPost("{id:guid}/evaluate")]
    [Authorize(Policy = Policies.CanInitiateWorkflow)]
    public async Task<ActionResult<AgentWorkflowDto>> EvaluatePolicy(
        Guid id,
        [FromBody] EvaluatePolicyRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var workflow = await _workflowService.EvaluatePolicyAsync(currentUser.OrganizationId, id, request, cancellationToken);
        return workflow is null
            ? throw NotFoundException.For(nameof(AgentWorkflow), id)
            : Ok(workflow);
    }

   // Runs the policy compliance agent for the workflow.
    [HttpPost("{id:guid}/run-policy-agent")]
    [Authorize(Policy = Policies.CanInitiateWorkflow)]
    public async Task<ActionResult<AgentWorkflowDto>> RunPolicyAgent(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var workflow = await _policyComplianceAgent.RunAsync(currentUser.OrganizationId, id, cancellationToken);
        return workflow is null
            ? throw NotFoundException.For(nameof(AgentWorkflow), id)
            : Ok(workflow);
    }

   // Runs the maintenance analysis agent for the workflow.
    [HttpPost("{id:guid}/run-maintenance-agent")]
    [Authorize(Policy = Policies.CanInitiateWorkflow)]
    public async Task<ActionResult<AgentWorkflowDto>> RunMaintenanceAnalysisAgent(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var workflow = await _maintenanceAnalysisAgent.RunAsync(currentUser.OrganizationId, id, cancellationToken);
        return workflow is null
            ? throw NotFoundException.For(nameof(AgentWorkflow), id)
            : Ok(workflow);
    }

    // Runs the budget analysis agent for the workflow.
    [HttpPost("{id:guid}/run-budget-agent")]
    [Authorize(Policy = Policies.CanInitiateWorkflow)]
    public async Task<ActionResult<AgentWorkflowDto>> RunBudgetAgent(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var workflow = await _workflowService.RunBudgetAnalysisAsync(currentUser.OrganizationId, id, cancellationToken);
        return workflow is null
            ? throw NotFoundException.For(nameof(AgentWorkflow), id)
            : Ok(workflow);
    }

   // Records the administrator's decision for the workflow.
    [HttpPatch("{id:guid}/decide")]
    [Authorize(Policy = Policies.CanApproveWorkflow)]
    public async Task<ActionResult<AgentWorkflowDto>> Decide(
        Guid id,
        [FromBody] DecideWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var workflow = await _workflowService.DecideAsync(currentUser.OrganizationId, id, currentUser.Id, request, cancellationToken);
        return workflow is null
            ? throw NotFoundException.For(nameof(AgentWorkflow), id)
            : Ok(workflow);
    }
}
