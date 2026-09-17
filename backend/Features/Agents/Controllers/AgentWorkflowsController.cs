using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Agents.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Agents.Controllers;

// SRS §7, FR-067 to FR-076: the Asset Lifecycle Decision workflow's
// initiation, status, policy-gate evaluation and human-approval checkpoint.
[ApiController]
[Route("api/agent-workflows")]
[Authorize]
public class AgentWorkflowsController : CoreGridControllerBase
{
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

    // FR-069: Officer, Auditor, Administrator may view workflow status.
    [HttpGet]
    [Authorize(Roles = $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<List<AgentWorkflowDto>>> GetWorkflows([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        return Ok(await _workflowService.GetWorkflowsAsync(currentUser.OrganizationId, status, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<AgentWorkflowDto>> GetWorkflowById(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var workflow = await _workflowService.GetWorkflowByIdAsync(currentUser.OrganizationId, id, cancellationToken);
        if (workflow is null) return NotFound(new { message = "Workflow not found." });

        return Ok(workflow);
    }

    // FR-067/FR-068: Officer or Administrator initiates an evaluation.
    [HttpPost]
    [Authorize(Roles = $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<AgentWorkflowDto>> CreateWorkflow(
        [FromBody] CreateAgentWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        try
        {
            var workflow = await _workflowService.CreateWorkflowAsync(currentUser.OrganizationId, currentUser.Id, request, cancellationToken);
            return CreatedAtAction(nameof(GetWorkflowById), new { id = workflow.Id }, workflow);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Stands in for nodes 2-4 having run and handed off to the Policy
    // Compliance node — see AgentWorkflowService.EvaluatePolicyAsync.
    [HttpPost("{id:guid}/evaluate")]
    [Authorize(Roles = $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<AgentWorkflowDto>> EvaluatePolicy(
        Guid id,
        [FromBody] EvaluatePolicyRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        try
        {
            var workflow = await _workflowService.EvaluatePolicyAsync(currentUser.OrganizationId, id, request, cancellationToken);
            if (workflow is null) return NotFound(new { message = "Workflow not found." });
            return Ok(workflow);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Runs the real Policy Compliance Agent (SRS §7.3, node 4): assembles
    // policy/compliance facts via its own tool allow-list, runs a
    // deterministic heuristic for a proposed recommendation (no LLM), then
    // runs that through the same deterministic gate /evaluate does. Same
    // roles as /evaluate — this replaces manually typing a recommendation,
    // not who may trigger it.
    [HttpPost("{id:guid}/run-policy-agent")]
    [Authorize(Roles = $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<AgentWorkflowDto>> RunPolicyAgent(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        try
        {
            var workflow = await _policyComplianceAgent.RunAsync(currentUser.OrganizationId, id, cancellationToken);
            if (workflow is null) return NotFound(new { message = "Workflow not found." });
            return Ok(workflow);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Runs the real Maintenance Analysis Agent (SRS §7.3, node 2): assembles
    // repair count / MTBF / cost trend / 12-month cost projection for the
    // workflow's asset (deterministic, no LLM) and records them on the
    // workflow for nodes 3/4 or a human reviewer to read. Same roles as
    // /run-policy-agent — it produces facts, not a recommendation, so it
    // never advances the workflow's status or approval state.
    [HttpPost("{id:guid}/run-maintenance-agent")]
    [Authorize(Roles = $"{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<AgentWorkflowDto>> RunMaintenanceAnalysisAgent(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        try
        {
            var workflow = await _maintenanceAnalysisAgent.RunAsync(currentUser.OrganizationId, id, cancellationToken);
            if (workflow is null) return NotFound(new { message = "Workflow not found." });
            return Ok(workflow);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // AI-14: only an Administrator (`workflow:approve`) may decide a paused workflow.
    [HttpPatch("{id:guid}/decide")]
    [Authorize(Roles = $"{nameof(CoreGridRole.Administrator)}")]
    public async Task<ActionResult<AgentWorkflowDto>> Decide(
        Guid id,
        [FromBody] DecideWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        try
        {
            var workflow = await _workflowService.DecideAsync(currentUser.OrganizationId, id, currentUser.Id, request, cancellationToken);
            if (workflow is null) return NotFound(new { message = "Workflow not found." });
            return Ok(workflow);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
