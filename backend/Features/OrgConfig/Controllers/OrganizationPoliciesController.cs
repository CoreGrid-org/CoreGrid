using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.OrgConfig.DTOs;
using CoreGrid.Api.Features.OrgConfig.Services;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.OrgConfig.Controllers;

// Administrator-defined organisation policy parameters, consumed by
// lifecycle rules and the Policy Agent.
[ApiController]
[Route("api/organization-policies")]
[Authorize(Policy = Policies.CanManageConfiguration)]
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
    public async Task<ActionResult<PagedResult<OrganizationPolicyDto>>> GetPolicies(
        [FromQuery] PagedQuery query,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var policies = await _policyService.GetPoliciesAsync(currentUser.OrganizationId, query, cancellationToken);
        return Ok(policies);
    }

    // GET /api/organization-policies/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrganizationPolicyDto>> GetPolicyById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var policy = await _policyService.GetPolicyByIdAsync(currentUser.OrganizationId, id, cancellationToken);
        return policy is null
            ? throw NotFoundException.For(nameof(OrganizationPolicy), id)
            : Ok(policy);
    }

    // POST /api/organization-policies
    [HttpPost]
    public async Task<ActionResult<OrganizationPolicyDto>> CreatePolicy(
        [FromBody] SaveOrganizationPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var policy = await _policyService.CreatePolicyAsync(currentUser.OrganizationId, currentUser.Id, request, cancellationToken);
        return CreatedAtAction(nameof(GetPolicyById), new { id = policy.Id }, policy);
    }

    // PUT /api/organization-policies/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OrganizationPolicyDto>> UpdatePolicy(
        Guid id,
        [FromBody] SaveOrganizationPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var policy = await _policyService.UpdatePolicyAsync(currentUser.OrganizationId, id, currentUser.Id, request, cancellationToken);
        return policy is null
            ? throw NotFoundException.For(nameof(OrganizationPolicy), id)
            : Ok(policy);
    }
}
