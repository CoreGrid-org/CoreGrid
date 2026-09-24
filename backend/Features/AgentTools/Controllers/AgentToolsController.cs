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

// Provides tools for agent workflow operations.
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
    // Returns a summary of an asset.
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
    // Returns the budget summary for a department.
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
   // Returns the applicable organization policies.
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
    // Returns the compliance state of an asset.
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
   // Returns the maintenance history of an asset.
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
    // Returns failure statistics for an asset.
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

    // Calculates asset depreciation.
    [HttpPost("api/agent-tools/compute-depreciation")]
    public ActionResult<ComputeDepreciationResponse> ComputeDepreciation(
        [FromBody] ComputeDepreciationRequest request)
    {
        var result = _agentToolsService.ComputeDepreciation(request);
        return Ok(result);
    }

    // Resolves the organization from the authenticated context.
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
