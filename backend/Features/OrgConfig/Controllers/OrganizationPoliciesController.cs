using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.OrgConfig.DTOs;
using CoreGrid.Api.Features.OrgConfig.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using CoreGrid.Api.Features.Shared;

namespace CoreGrid.Api.Features.OrgConfig.Controllers;

// FR-015: Administrator-defined organisation policy parameters, consumed by
// lifecycle rules and the Policy Agent.
[ApiController]
[Route("api/organization-policies")]
[Authorize(Roles = nameof(CoreGridRole.Administrator))]
public class OrganizationPoliciesController : CoreGridControllerBase
{
    private readonly IOrganizationPolicyService _policyService;

    public OrganizationPoliciesController(
        IOrganizationPolicyService policyService,
        CoreGridDbContext db) : base(db)
    {
        _policyService = policyService;
    }

    // GET /api/organization-policies
    [HttpGet]
    public async Task<ActionResult<List<OrganizationPolicyDto>>> GetPolicies(
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var policies = await _policyService.GetPoliciesAsync(currentUser.OrganizationId);

        return Ok(policies);
    }

    // GET /api/organization-policies/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrganizationPolicyDto>> GetPolicyById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var policy = await _policyService.GetPolicyByIdAsync(currentUser.OrganizationId, id);

        if (policy is null)
        {
            return NotFound(new { message = "Policy not found." });
        }

        return Ok(policy);
    }

    // POST /api/organization-policies
    [HttpPost]
    public async Task<ActionResult<OrganizationPolicyDto>> CreatePolicy(
        [FromBody] SaveOrganizationPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var policy = await _policyService.CreatePolicyAsync(
                currentUser.OrganizationId,
                currentUser.Id,
                request);

            return CreatedAtAction(nameof(GetPolicyById), new { id = policy.Id }, policy);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT /api/organization-policies/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OrganizationPolicyDto>> UpdatePolicy(
        Guid id,
        [FromBody] SaveOrganizationPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var policy = await _policyService.UpdatePolicyAsync(
                currentUser.OrganizationId,
                id,
                currentUser.Id,
                request);

            if (policy is null)
            {
                return NotFound(new { message = "Policy not found." });
            }

            return Ok(policy);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
