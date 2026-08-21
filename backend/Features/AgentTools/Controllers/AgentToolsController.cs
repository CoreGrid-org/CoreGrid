using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreGrid.Api.Data;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.AgentTools.Services;
using CoreGrid.Api.Features.Shared;

namespace CoreGrid.Api.Features.AgentTools.Controllers;

[ApiController]
[Authorize]
public class AgentToolsController : CoreGridControllerBase
{
    private readonly IAgentToolsService _agentToolsService;

    public AgentToolsController(IAgentToolsService agentToolsService, CoreGridDbContext db) : base(db)
    {
        _agentToolsService = agentToolsService;
    }

    // =========================================================
    // GET /api/agent-tools/assets/{assetId}/financials
    // =========================================================
    [HttpGet("api/agent-tools/assets/{assetId:guid}/financials")]
    public async Task<ActionResult<AssetFinancialsDto>> GetAssetFinancials(
        Guid assetId,
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken)
    {
        var orgId = await ResolveOrganizationIdAsync(organizationId, cancellationToken);
        if (orgId is null) return Unauthorized(new { message = "Unable to resolve organization context." });

        var result = await _agentToolsService.GetAssetFinancialsAsync(
            orgId.Value,
            assetId,
            cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = $"Asset with ID {assetId} not found." });
        }

        return Ok(result);
    }

    // =========================================================
    // GET /api/agent-tools/departments/{departmentId}/budget-summary?fiscalYear={year}
    // =========================================================
    [HttpGet("api/agent-tools/departments/{departmentId:guid}/budget-summary")]
    public async Task<ActionResult<DepartmentBudgetSummaryDto>> GetDepartmentBudgetSummary(
        Guid departmentId,
        [FromQuery] int? fiscalYear,
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken)
    {
        var orgId = await ResolveOrganizationIdAsync(organizationId, cancellationToken);
        if (orgId is null) return Unauthorized(new { message = "Unable to resolve organization context." });

        var year = fiscalYear ?? DateTime.UtcNow.Year;

        var result = await _agentToolsService.GetDepartmentBudgetSummaryAsync(
            orgId.Value,
            departmentId,
            year,
            cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = $"Department with ID {departmentId} not found." });
        }

        return Ok(result);
    }

    // =========================================================
    // GET /api/agent-tools/organization-policies?assetTypeId={id}
    // Policy Compliance Agent tool (§7.4).
    // =========================================================
    [HttpGet("api/agent-tools/organization-policies")]
    public async Task<ActionResult<OrganizationPolicyFactsDto>> GetOrganizationPolicies(
        [FromQuery] Guid? assetTypeId,
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken)
    {
        var orgId = await ResolveOrganizationIdAsync(organizationId, cancellationToken);
        if (orgId is null) return Unauthorized(new { message = "Unable to resolve organization context." });

        var result = await _agentToolsService.GetOrganizationPoliciesAsync(orgId.Value, assetTypeId, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "No organisation policy configured (neither asset-type-specific nor the org-wide default)." });
        }

        return Ok(result);
    }

    // =========================================================
    // GET /api/agent-tools/assets/{assetId}/compliance-state
    // Policy Compliance Agent tool (§7.4).
    // =========================================================
    [HttpGet("api/agent-tools/assets/{assetId:guid}/compliance-state")]
    public async Task<ActionResult<AssetComplianceStateDto>> GetAssetComplianceState(
        Guid assetId,
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken)
    {
        var orgId = await ResolveOrganizationIdAsync(organizationId, cancellationToken);
        if (orgId is null) return Unauthorized(new { message = "Unable to resolve organization context." });

        var result = await _agentToolsService.GetAssetComplianceStateAsync(orgId.Value, assetId, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = $"Asset with ID {assetId} not found." });
        }

        return Ok(result);
    }

    private async Task<Guid?> ResolveOrganizationIdAsync(Guid? queryOrgId, CancellationToken cancellationToken)
    {
        // 1. Try human user
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is not null)
        {
            return currentUser.OrganizationId;
        }

        // 2. Try service account token (org claim or query parameter per AI-05)
        var orgClaim = User.FindFirst("org_id")?.Value ?? User.FindFirst("organization_id")?.Value;
        if (Guid.TryParse(orgClaim, out var claimOrgId))
        {
            return claimOrgId;
        }

        if (queryOrgId.HasValue && queryOrgId.Value != Guid.Empty)
        {
            return queryOrgId.Value;
        }

        // In single-tenant M0 (SRS §4.2), fallback to the single deployment Organization
        var singleOrg = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            Db.Organizations.Select(o => (Guid?)o.Id), cancellationToken);
        return singleOrg;
    }

    // =========================================================
    // POST /api/agent-tools/compute-depreciation
    // Pure computation endpoint — no DB access
    // =========================================================
    [HttpPost("api/agent-tools/compute-depreciation")]
    [AllowAnonymous]
    public ActionResult<ComputeDepreciationResponse> ComputeDepreciation(
        [FromBody] ComputeDepreciationRequest request)
    {
        var result = _agentToolsService.ComputeDepreciation(request);
        return Ok(result);
    }
}
