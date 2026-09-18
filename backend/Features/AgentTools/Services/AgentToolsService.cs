using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.AgentTools.Services;

public class AgentToolsService : IAgentToolsService
{
    private readonly CoreGridDbContext _dbContext;

    public AgentToolsService(CoreGridDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AssetSummaryDto?> GetAssetSummaryAsync(
        Guid organizationId,
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        var asset = await _dbContext.Assets
            .AsNoTracking()
            .Include(a => a.AssetType)
                .ThenInclude(t => t!.AssetCategory)
            .Include(a => a.Department)
            .Include(a => a.Location)
            .FirstOrDefaultAsync(
                a => a.Id == assetId && a.OrganizationId == organizationId,
                cancellationToken);

        if (asset is null) return null;

        return new AssetSummaryDto
        {
            AssetId = asset.Id,
            AssetCode = asset.AssetCode,
            Name = asset.Name,
            AssetType = asset.AssetType?.Name ?? string.Empty,
            Category = asset.AssetType?.AssetCategory?.Name ?? string.Empty,
            Status = asset.Status,
            Condition = asset.Condition,
            Department = asset.Department?.Name ?? string.Empty,
            Location = asset.Location?.Name ?? string.Empty,
            AcquisitionDate = asset.AcquisitionDate,
            AcquisitionCost = asset.AcquisitionCost
        };
    }

    public async Task<AssetFinancialsDto?> GetAssetFinancialsAsync(
        Guid organizationId,
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        var asset = await _dbContext.Assets
            .AsNoTracking()
            .Include(a => a.AssetType)
            .FirstOrDefaultAsync(a => a.Id == assetId && a.OrganizationId == organizationId, cancellationToken);

        if (asset == null) return null;

        var usefulLife = asset.AssetType?.UsefulLifeYears ?? 0;
        var deprResult = ComputeDepreciation(new ComputeDepreciationRequest
        {
            AcquisitionCost = asset.AcquisitionCost,
            AcquisitionDate = asset.AcquisitionDate,
            UsefulLifeYears = usefulLife,
            AsOfDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        return new AssetFinancialsDto
        {
            AssetId = asset.Id,
            AssetCode = asset.AssetCode,
            AcquisitionCost = asset.AcquisitionCost,
            AcquisitionDate = asset.AcquisitionDate,
            UsefulLifeYears = usefulLife,
            AccumulatedDepreciation = deprResult.AccumulatedDepreciation,
            ResidualBookValue = deprResult.CurrentValue,
            CumulativeMaintenanceCost = asset.CumulativeMaintenanceCost,
            ReplacementEstimate = null,
            ReplacementEstimateNote = "Replacement value estimation data source is not yet available in the database schema."
        };
    }

    public async Task<DepartmentBudgetSummaryDto?> GetDepartmentBudgetSummaryAsync(
        Guid organizationId,
        Guid departmentId,
        int fiscalYear,
        CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == departmentId && d.OrganizationId == organizationId, cancellationToken);

        if (department == null) return null;

        // Note: Department-level budget allocation/committed/spent tracking is not part of the current schema
        return new DepartmentBudgetSummaryDto
        {
            DepartmentId = department.Id,
            DepartmentCode = department.Code,
            DepartmentName = department.Name,
            FiscalYear = fiscalYear,
            AllocatedMaintenanceBudget = null,
            CommittedAmount = null,
            SpentAmount = null,
            RemainingAmount = null,
            Status = "NOT_CONFIGURED",
            Note = "Department budget allocation and committed/spent tracking tables do not exist in the database schema."
        };
    }

    // get_organization_policies (§7.4) — falls back to the org-wide default
    // policy (AssetTypeId == null) when no asset-type-specific one exists,
    // matching OrganizationPoliciesController's "at most one policy per
    // asset type, including the org-wide default" rule.
    public async Task<OrganizationPolicyFactsDto?> GetOrganizationPoliciesAsync(
        Guid organizationId,
        Guid? assetTypeId,
        CancellationToken cancellationToken = default)
    {
        var policy = await _dbContext.OrganizationPolicies.AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.AssetTypeId == assetTypeId, cancellationToken);

        policy ??= await _dbContext.OrganizationPolicies.AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.AssetTypeId == null, cancellationToken);

        if (policy is null) return null;

        return new OrganizationPolicyFactsDto
        {
            AssetTypeId = policy.AssetTypeId,
            RepairToReplaceCostThreshold = policy.RepairToReplaceCostThreshold,
            MinimumServiceLifeYears = policy.MinimumServiceLifeYears,
            MaxAcceptableFailureFrequency = policy.MaxAcceptableFailureFrequency,
            ValuationValidityWindowDays = policy.ValuationValidityWindowDays,
            ConfidenceFloor = policy.ConfidenceFloor
        };
    }

    // get_asset_compliance_state (§7.4). Valuation is read from the most
    // recent DisposalRequest with a recorded ValuationDate for this asset —
    // the schema's only source of an asset valuation.
    public async Task<AssetComplianceStateDto?> GetAssetComplianceStateAsync(
        Guid organizationId,
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        var asset = await _dbContext.Assets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assetId && a.OrganizationId == organizationId, cancellationToken);
        if (asset is null) return null;

        var latestValuationDate = await _dbContext.DisposalRequests.AsNoTracking()
            .Where(d => d.AssetId == assetId && d.OrganizationId == organizationId && d.ValuationDate != null)
            .OrderByDescending(d => d.ValuationDate)
            .Select(d => d.ValuationDate)
            .FirstOrDefaultAsync(cancellationToken);

        var openMaintenanceCount = await _dbContext.MaintenanceRecords.AsNoTracking().CountAsync(
            m => m.AssetId == assetId && m.OrganizationId == organizationId
                && m.Status != MaintenanceStatus.COMPLETED && m.Status != MaintenanceStatus.CANCELLED,
            cancellationToken);

        var openTransferCount = await _dbContext.AssetTransfers.AsNoTracking().CountAsync(
            t => t.AssetId == assetId && t.OrganizationId == organizationId
                && t.Status != TransferStatus.COMPLETED && t.Status != TransferStatus.REJECTED && t.Status != TransferStatus.CANCELLED,
            cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var elapsedYears = (today.DayNumber - asset.AcquisitionDate.DayNumber) / 365.25m;

        return new AssetComplianceStateDto
        {
            AssetId = asset.Id,
            AssetCode = asset.AssetCode,
            CurrentStatus = asset.Status,
            CurrentCondition = asset.Condition,
            IsCondemned = asset.Status == AssetStatusConstants.Condemned,
            HasValuation = latestValuationDate.HasValue,
            ValuationDate = latestValuationDate,
            OpenMaintenanceCount = openMaintenanceCount,
            OpenTransferCount = openTransferCount,
            ElapsedServiceLifeYears = Math.Round(elapsedYears, 1)
        };
    }

    public ComputeDepreciationResponse ComputeDepreciation(ComputeDepreciationRequest request)
    {
        // Required/Range on the DTO makes these 400 for an HTTP caller before
        // this method ever runs; the internal in-process caller below
        // (GetAssetFinancialsAsync) always supplies concrete values too, so
        // .Value is safe here — but the useful-life/cost sanity check stays
        // as a defensive fallback for that same internal caller, which can
        // legitimately pass UsefulLifeYears = 0 when an asset has no
        // AssetType (bypasses ModelState entirely, since it's a direct C#
        // call, not a model-bound request).
        var acquisitionCost = request.AcquisitionCost!.Value;
        var acquisitionDate = request.AcquisitionDate!.Value;
        var usefulLifeYears = request.UsefulLifeYears!.Value;

        if (usefulLifeYears <= 0 || acquisitionCost <= 0)
        {
            return new ComputeDepreciationResponse
            {
                AcquisitionCost = acquisitionCost,
                AcquisitionDate = acquisitionDate,
                UsefulLifeYears = usefulLifeYears,
                AsOfDate = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                AnnualDepreciation = 0,
                AccumulatedDepreciation = 0,
                CurrentValue = Math.Max(0, acquisitionCost),
                DepreciationMethod = "straight-line"
            };
        }

        var asOf = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // Calculate full elapsed years
        int yearsElapsed = asOf.Year - acquisitionDate.Year;
        if (asOf < acquisitionDate.AddYears(yearsElapsed))
        {
            yearsElapsed--;
        }
        if (yearsElapsed < 0)
        {
            yearsElapsed = 0;
        }

        decimal annualDepreciation = acquisitionCost / usefulLifeYears;
        decimal accumulated = Math.Min(annualDepreciation * yearsElapsed, acquisitionCost);
        decimal currentValue = Math.Max(0, acquisitionCost - accumulated);

        return new ComputeDepreciationResponse
        {
            AcquisitionCost = acquisitionCost,
            AcquisitionDate = acquisitionDate,
            UsefulLifeYears = usefulLifeYears,
            AsOfDate = asOf,
            AnnualDepreciation = Math.Round(annualDepreciation, 2),
            AccumulatedDepreciation = Math.Round(accumulated, 2),
            CurrentValue = Math.Round(currentValue, 2),
            DepreciationMethod = "straight-line"
        };
    }
}
