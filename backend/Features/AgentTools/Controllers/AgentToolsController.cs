using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.AgentTools.Services;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.AgentTools.Controllers;

// §5.9 (B3-B5): a human caller can reach these tools as themselves (e.g. an
// Administrator exercising compute-depreciation directly), and so can the
// agent service principal — CanReadAssets already covers exactly that
// combination (every human role, plus the agent principal per SRS
// Appendix B), so it replaces the dead AgentToolsAuthMiddleware (B3,
// deleted) that never actually ran. Org scope is never taken from request
// content (SEC-ID-02, B5): a human caller's org comes from ICurrentUser; a
// service principal has no Users row, so its org is the M0 deployment's
// one Organization, resolved server-side, the same invariant
// SetupController relies on.
[ApiController]
[Authorize(Policy = Policies.CanReadAssets)]
public class AgentToolsController : CoreGridControllerBase
{
    private readonly IAgentToolsService _agentToolsService;
    private readonly IMaintenanceAnalysisToolsService _maintenanceAnalysisToolsService;

    public AgentToolsController(
        IAgentToolsService agentToolsService,
        IMaintenanceAnalysisToolsService maintenanceAnalysisToolsService,
        CoreGridDbContext db) : base(db)
    {
        _agentToolsService = agentToolsService;
        _maintenanceAnalysisToolsService = maintenanceAnalysisToolsService;
    }

    // GET /api/agent-tools/assets/{assetId}/summary — Planner Agent tool.
    [HttpGet("api/agent-tools/assets/{assetId:guid}/summary")]
    public async Task<ActionResult<AssetSummaryDto>> GetAssetSummary(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var orgId = await ResolveOrganizationIdAsync(cancellationToken);
        var result = await _agentToolsService.GetAssetSummaryAsync(orgId, assetId, cancellationToken);
        return result is null
            ? throw NotFoundException.For(nameof(Asset), assetId)
            : Ok(result);
    }

    [HttpGet("api/agent-tools/assets/{assetId:guid}/financials")]
    public async Task<ActionResult<AssetFinancialsDto>> GetAssetFinancials(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var orgId = await ResolveOrganizationIdAsync(cancellationToken);
        var result = await _agentToolsService.GetAssetFinancialsAsync(orgId, assetId, cancellationToken);
        return result is null
            ? throw NotFoundException.For(nameof(Asset), assetId)
            : Ok(result);
    }

    // GET /api/agent-tools/departments/{departmentId}/budget-summary?fiscalYear={year}
    [HttpGet("api/agent-tools/departments/{departmentId:guid}/budget-summary")]
    public async Task<ActionResult<DepartmentBudgetSummaryDto>> GetDepartmentBudgetSummary(
        Guid departmentId,
        [FromQuery] int? fiscalYear,
        CancellationToken cancellationToken)
    {
        var orgId = await ResolveOrganizationIdAsync(cancellationToken);
        var year = fiscalYear ?? DateTime.UtcNow.Year;

        var result = await _agentToolsService.GetDepartmentBudgetSummaryAsync(orgId, departmentId, year, cancellationToken);
        return result is null
            ? throw NotFoundException.For(nameof(Department), departmentId)
            : Ok(result);
    }

    // GET /api/agent-tools/organization-policies?assetTypeId={id}
    // Policy Compliance Agent tool (§7.4).
    [HttpGet("api/agent-tools/organization-policies")]
    public async Task<ActionResult<OrganizationPolicyFactsDto>> GetOrganizationPolicies(
        [FromQuery] Guid? assetTypeId,
        CancellationToken cancellationToken)
    {
        var orgId = await ResolveOrganizationIdAsync(cancellationToken);
        var result = await _agentToolsService.GetOrganizationPoliciesAsync(orgId, assetTypeId, cancellationToken);

        if (result is null)
        {
            throw new ValidationException(nameof(assetTypeId), "No organisation policy configured (neither asset-type-specific nor the org-wide default).");
        }

        return Ok(result);
    }

    // GET /api/agent-tools/assets/{assetId}/compliance-state
    // Policy Compliance Agent tool (§7.4).
    [HttpGet("api/agent-tools/assets/{assetId:guid}/compliance-state")]
    public async Task<ActionResult<AssetComplianceStateDto>> GetAssetComplianceState(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var orgId = await ResolveOrganizationIdAsync(cancellationToken);
        var result = await _agentToolsService.GetAssetComplianceStateAsync(orgId, assetId, cancellationToken);
        return result is null
            ? throw NotFoundException.For(nameof(Asset), assetId)
            : Ok(result);
    }

    // GET /api/agent-tools/assets/{assetId}/maintenance-history
    // Maintenance Analysis Agent tool (§7.4, node 2).
    [HttpGet("api/agent-tools/assets/{assetId:guid}/maintenance-history")]
    public async Task<ActionResult<MaintenanceHistoryDto>> GetMaintenanceHistory(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var orgId = await ResolveOrganizationIdAsync(cancellationToken);
        var result = await _maintenanceAnalysisToolsService.GetMaintenanceHistoryAsync(orgId, assetId, cancellationToken);
        return result is null
            ? throw NotFoundException.For(nameof(Asset), assetId)
            : Ok(result);
    }

    // GET /api/agent-tools/assets/{assetId}/failure-statistics
    // Maintenance Analysis Agent tool (§7.4, node 2).
    [HttpGet("api/agent-tools/assets/{assetId:guid}/failure-statistics")]
    public async Task<ActionResult<FailureStatisticsDto>> GetFailureStatistics(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var orgId = await ResolveOrganizationIdAsync(cancellationToken);
        var result = await _maintenanceAnalysisToolsService.ComputeFailureStatisticsAsync(orgId, assetId, cancellationToken);
        return result is null
            ? throw NotFoundException.For(nameof(Asset), assetId)
            : Ok(result);
    }

    // POST /api/agent-tools/compute-depreciation — pure computation, no DB
    // access. B4: no longer [AllowAnonymous] — it requires AgentToolAccess
    // like every other route on this controller now.
    [HttpPost("api/agent-tools/compute-depreciation")]
    public ActionResult<ComputeDepreciationResponse> ComputeDepreciation(
        [FromBody] ComputeDepreciationRequest request)
    {
        var result = _agentToolsService.ComputeDepreciation(request);
        return Ok(result);
    }

    // B5 / SEC-ID-02: organisation is never taken from request content
    // (query string or claim). A human caller's org comes from
    // ICurrentUser. A service principal has no Users row, so its org is
    // the M0 deployment's one Organization, resolved server-side (SRS
    // §4.2's single-tenant invariant).
    private async Task<Guid> ResolveOrganizationIdAsync(CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is not null)
        {
            return currentUser.OrganizationId;
        }

        return await Db.Organizations.Select(o => o.Id).SingleAsync(cancellationToken);
    }
}
