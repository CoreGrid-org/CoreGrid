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

[ApiController]
[Route("api/agent-tools")]
[Authorize(Policy = Policies.CanReadAssets)]
public class AgentToolsController(
    IPlannerTools plannerTools,
    IMaintenanceTools maintenanceTools,
    IBudgetTools budgetTools,
    IPolicyTools policyTools,
    CoreGridDbContext db) : CoreGridControllerBase(db)
{
    [HttpGet("assets/{assetId:guid}/summary")]
    public async Task<ActionResult<AssetSummaryDto>> GetAssetSummary(Guid assetId, CancellationToken cancellationToken)
    {
        var organizationId = await ResolveOrganizationIdAsync(cancellationToken);
        return FoundAsset(await plannerTools.GetAssetSummaryAsync(organizationId, assetId, cancellationToken), assetId);
    }

    [HttpGet("assets/{assetId:guid}/financials")]
    public async Task<ActionResult<AssetFinancialsDto>> GetAssetFinancials(Guid assetId, CancellationToken cancellationToken)
    {
        var selection = await SingleAssetAsync(assetId, cancellationToken);
        return FoundAsset((await budgetTools.GetFinancialsAsync(selection, cancellationToken)).FirstOrDefault(), assetId);
    }

    [HttpGet("assets/{assetId:guid}/compliance-state")]
    public async Task<ActionResult<AssetComplianceStateDto>> GetAssetComplianceState(Guid assetId, CancellationToken cancellationToken)
    {
        var selection = await SingleAssetAsync(assetId, cancellationToken);
        return FoundAsset((await policyTools.GetComplianceStateAsync(selection, cancellationToken)).FirstOrDefault(), assetId);
    }

    [HttpGet("assets/{assetId:guid}/maintenance-history")]
    public async Task<ActionResult<MaintenanceHistoryDto>> GetMaintenanceHistory(Guid assetId, CancellationToken cancellationToken)
    {
        var organizationId = await ResolveOrganizationIdAsync(cancellationToken);
        return FoundAsset(await maintenanceTools.GetMaintenanceHistoryAsync(organizationId, assetId, cancellationToken), assetId);
    }

    [HttpGet("assets/{assetId:guid}/failure-statistics")]
    public async Task<ActionResult<FailureStatisticsDto>> GetFailureStatistics(Guid assetId, CancellationToken cancellationToken)
    {
        var selection = await SingleAssetAsync(assetId, cancellationToken);
        return FoundAsset((await maintenanceTools.GetFailureStatisticsAsync(selection, cancellationToken)).FirstOrDefault(), assetId);
    }

    [HttpGet("departments/{departmentId:guid}/budget-summary")]
    public async Task<ActionResult<DepartmentBudgetSummaryDto>> GetDepartmentBudgetSummary(
        Guid departmentId, [FromQuery] int? fiscalYear, CancellationToken cancellationToken)
    {
        var organizationId = await ResolveOrganizationIdAsync(cancellationToken);
        var summary = await budgetTools.GetDepartmentBudgetSummaryAsync(
            organizationId, departmentId, fiscalYear ?? DateTime.UtcNow.Year, cancellationToken);

        return summary ?? throw NotFoundException.For(nameof(Department), departmentId);
    }

    [HttpGet("organization-policies")]
    public async Task<ActionResult<OrganizationPolicyFactsDto>> GetOrganizationPolicies(
        [FromQuery] Guid? assetTypeId, CancellationToken cancellationToken)
    {
        var organizationId = await ResolveOrganizationIdAsync(cancellationToken);
        var policy = await policyTools.GetOrganizationPoliciesAsync(organizationId, assetTypeId, cancellationToken);

        return policy ?? throw new ValidationException(
            nameof(assetTypeId), "No organisation policy configured (neither asset-type-specific nor the org-wide default).");
    }

    [HttpPost("compute-depreciation")]
    public ActionResult<ComputeDepreciationResponse> ComputeDepreciation([FromBody] ComputeDepreciationRequest request) =>
        budgetTools.ComputeDepreciation(request);

    private static T FoundAsset<T>(T? result, Guid assetId) where T : class =>
        result ?? throw NotFoundException.For(nameof(Asset), assetId);

    private async Task<AssetSelection> SingleAssetAsync(Guid assetId, CancellationToken cancellationToken) =>
        AssetSelection.Single(await ResolveOrganizationIdAsync(cancellationToken), assetId);

    private async Task<Guid> ResolveOrganizationIdAsync(CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        return currentUser?.OrganizationId ?? await Db.Organizations.Select(o => o.Id).SingleAsync(cancellationToken);
    }
}
