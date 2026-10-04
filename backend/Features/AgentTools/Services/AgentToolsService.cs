using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Shared.Finance;

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
            DepartmentId = asset.DepartmentId,
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

// Loads the asset-specific policy or organization-wide default.
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

  // Loads the asset compliance state and latest valuation.
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
            IsCondemned = asset.Status == AssetStatuses.Condemned,
            HasValuation = latestValuationDate.HasValue,
            ValuationDate = latestValuationDate,
            OpenMaintenanceCount = openMaintenanceCount,
            OpenTransferCount = openTransferCount,
            ElapsedServiceLifeYears = Math.Round(elapsedYears, 1)
        };
    }

    public async Task<AssetTypeSummaryDto?> GetAssetTypeSummaryAsync(
        Guid organizationId,
        Guid assetTypeId,
        CancellationToken cancellationToken = default)
    {
        var type = await _dbContext.AssetTypes.AsNoTracking()
            .Where(t => t.Id == assetTypeId && t.OrganizationId == organizationId)
            .Select(t => new { t.Id, t.Name, Category = t.AssetCategory!.Name, t.UsefulLifeYears })
            .FirstOrDefaultAsync(cancellationToken);
        if (type is null) return null;

        var conditionCounts = await _dbContext.Assets.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId && a.AssetTypeId == assetTypeId && a.Status != AssetStatuses.Disposed)
            .GroupBy(a => a.Condition)
            .Select(g => new { Condition = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Condition, g => g.Count, cancellationToken);

        return new AssetTypeSummaryDto
        {
            AssetTypeId = type.Id,
            AssetType = type.Name,
            Category = type.Category,
            UsefulLifeYears = type.UsefulLifeYears,
            ActiveAssetCount = conditionCounts.Values.Sum(),
            ConditionCounts = conditionCounts
        };
    }

    public async Task<IReadOnlyList<AssetFinancialsDto>> GetFleetFinancialsAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? assetId,
        CancellationToken cancellationToken = default)
    {
        var assets = await FleetQuery(organizationId, assetTypeId, assetId)
            .Select(a => new
            {
                a.Id, a.AssetCode, a.DepartmentId, a.AcquisitionCost, a.AcquisitionDate, a.CumulativeMaintenanceCost,
                UsefulLifeYears = a.AssetType != null ? a.AssetType.UsefulLifeYears : 0
            })
            .ToListAsync(cancellationToken);

        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        return assets.Select(a =>
        {
            var schedule = a.UsefulLifeYears > 0
                ? StraightLineDepreciation.ComputeSchedule(a.AcquisitionCost, a.AcquisitionDate, a.UsefulLifeYears, asOf)
                : null;
            return new AssetFinancialsDto
            {
                AssetId = a.Id,
                AssetCode = a.AssetCode,
                DepartmentId = a.DepartmentId,
                AcquisitionCost = a.AcquisitionCost,
                AcquisitionDate = a.AcquisitionDate,
                UsefulLifeYears = a.UsefulLifeYears,
                AccumulatedDepreciation = schedule?.AccumulatedDepreciation ?? 0m,
                ResidualBookValue = schedule?.CurrentValue ?? a.AcquisitionCost,
                CumulativeMaintenanceCost = a.CumulativeMaintenanceCost
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<AssetComplianceStateDto>> GetFleetComplianceStateAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? assetId,
        CancellationToken cancellationToken = default)
    {
        var assets = await FleetQuery(organizationId, assetTypeId, assetId)
            .Select(a => new { a.Id, a.AssetCode, a.Status, a.Condition, a.AcquisitionDate })
            .ToListAsync(cancellationToken);
        if (assets.Count == 0) return [];

        var ids = assets.Select(a => a.Id).ToList();

        var valuations = await _dbContext.DisposalRequests.AsNoTracking()
            .Where(d => ids.Contains(d.AssetId) && d.OrganizationId == organizationId && d.ValuationDate != null)
            .GroupBy(d => d.AssetId)
            .Select(g => new { AssetId = g.Key, Latest = g.Max(d => d.ValuationDate) })
            .ToDictionaryAsync(g => g.AssetId, g => g.Latest, cancellationToken);

        var openMaintenance = await _dbContext.MaintenanceRecords.AsNoTracking()
            .Where(m => ids.Contains(m.AssetId) && m.OrganizationId == organizationId
                && m.Status != MaintenanceStatus.COMPLETED && m.Status != MaintenanceStatus.CANCELLED)
            .GroupBy(m => m.AssetId)
            .Select(g => new { AssetId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.AssetId, g => g.Count, cancellationToken);

        var openTransfers = await _dbContext.AssetTransfers.AsNoTracking()
            .Where(t => ids.Contains(t.AssetId) && t.OrganizationId == organizationId
                && t.Status != TransferStatus.COMPLETED && t.Status != TransferStatus.REJECTED && t.Status != TransferStatus.CANCELLED)
            .GroupBy(t => t.AssetId)
            .Select(g => new { AssetId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.AssetId, g => g.Count, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return assets.Select(a =>
        {
            var valuationDate = valuations.GetValueOrDefault(a.Id);
            return new AssetComplianceStateDto
            {
                AssetId = a.Id,
                AssetCode = a.AssetCode,
                CurrentStatus = a.Status,
                CurrentCondition = a.Condition,
                IsCondemned = a.Status == AssetStatuses.Condemned,
                HasValuation = valuationDate.HasValue,
                ValuationDate = valuationDate,
                OpenMaintenanceCount = openMaintenance.GetValueOrDefault(a.Id),
                OpenTransferCount = openTransfers.GetValueOrDefault(a.Id),
                ElapsedServiceLifeYears = Math.Round((today.DayNumber - a.AcquisitionDate.DayNumber) / 365.25m, 1)
            };
        }).ToList();
    }

    // Every active (non-disposed) asset of the type, or just the one asset.
    private IQueryable<Asset> FleetQuery(Guid organizationId, Guid assetTypeId, Guid? assetId) =>
        _dbContext.Assets.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId
                && a.AssetTypeId == assetTypeId
                && a.Status != AssetStatuses.Disposed
                && (assetId == null || a.Id == assetId));

    public ComputeDepreciationResponse ComputeDepreciation(ComputeDepreciationRequest request)
    {
       // Uses validated depreciation inputs.
        var acquisitionCost = request.AcquisitionCost!.Value;
        var acquisitionDate = request.AcquisitionDate!.Value;
        var usefulLifeYears = request.UsefulLifeYears!.Value;
        var asOf = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

       // Uses the shared straight-line depreciation calculation.
        var schedule = StraightLineDepreciation.ComputeSchedule(acquisitionCost, acquisitionDate, usefulLifeYears, asOf);

        return new ComputeDepreciationResponse
        {
            AcquisitionCost = acquisitionCost,
            AcquisitionDate = acquisitionDate,
            UsefulLifeYears = usefulLifeYears,
            AsOfDate = asOf,
            AnnualDepreciation = schedule.AnnualDepreciation,
            AccumulatedDepreciation = schedule.AccumulatedDepreciation,
            CurrentValue = schedule.CurrentValue,
            DepreciationMethod = "straight-line"
        };
    }
}
