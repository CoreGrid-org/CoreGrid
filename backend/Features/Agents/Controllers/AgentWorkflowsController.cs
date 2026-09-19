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

// SRS §7, FR-067 to FR-076: the Asset Lifecycle Decision workflow's
// initiation, status, policy-gate evaluation and human-approval checkpoint.
[ApiController]
[Route("api/agent-workflows")]
[Authorize]
public class AgentWorkflowsController : CoreGridControllerBase
{
    // FR-069: read access stays Officer/Auditor/Administrator — SRS §4.6's
    // workflow:read row also grants Staff a "status only" view, but that
    // needs a field-level restriction this refactor doesn't add; kept as-is
    // (stricter than the matrix) and recorded, same posture as Maintenance's
    // CreateMaintenance/CompleteMaintenance staying Officer-only (plan §5.4).
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

    // GET /api/agent-workflows/{id}/execution-summary — SRS §9.6: the full
    // auditable trace (plan, agent outputs, tool calls, validation,
    // decision), same read access as GetWorkflowById.
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

    // FR-067/FR-068: Officer or Administrator initiates an evaluation.
    // AI-27: rate-limited per user+org — initiation drives real agent work.
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

    // Stands in for nodes 2-4 having run and handed off to the Policy
    // Compliance node — see AgentWorkflowService.EvaluatePolicyAsync. Same
    // roles as CreateWorkflow (CanInitiateWorkflow): a continuation of an
    // evaluation the caller was already allowed to start.
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

    // Runs the real Policy Compliance Agent (SRS §7.3, node 4): assembles
    // policy/compliance facts via its own tool allow-list, runs a
    // deterministic heuristic for a proposed recommendation (no LLM), then
    // runs that through the same deterministic gate /evaluate does. Same
    // roles as /evaluate — this replaces manually typing a recommendation,
    // not who may trigger it.
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

    // Runs the real Maintenance Analysis Agent (SRS §7.3, node 2): assembles
    // repair count / MTBF / cost trend / 12-month cost projection for the
    // workflow's asset (deterministic, no LLM) and records them on the
    // workflow for nodes 3/4 or a human reviewer to read. Same roles as
    // /run-policy-agent — it produces facts, not a recommendation, so it
    // never advances the workflow's status or approval state.
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

    // AI-14: only an Administrator (`workflow:approve`) may decide a paused workflow.
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
