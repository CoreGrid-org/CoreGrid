using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.AgentTools.Services;

// Provides maintenance history and failure statistics tools.
public class MaintenanceAnalysisToolsService(CoreGridDbContext db, IFailureStatisticsEngine statisticsEngine)
    : IMaintenanceAnalysisToolsService
{
    public async Task<MaintenanceHistoryDto?> GetMaintenanceHistoryAsync(
        Guid organizationId, Guid assetId, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assetId && a.OrganizationId == organizationId, cancellationToken);
        if (asset is null) return null;

        var records = await db.MaintenanceRecords.AsNoTracking()
            .Where(m => m.AssetId == assetId && m.OrganizationId == organizationId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MaintenanceHistoryEntryDto
            {
                Id = m.Id,
                Type = m.Type.ToString(),
                Status = m.Status.ToString(),
                CompletionDate = m.CompletionDate,
                ActualCost = m.ActualCost,
                ResultingCondition = m.ResultingCondition,
                Description = m.Description,
            })
            .ToListAsync(cancellationToken);

        return new MaintenanceHistoryDto { AssetId = asset.Id, AssetCode = asset.AssetCode, Records = records };
    }

    public async Task<FailureStatisticsDto?> ComputeFailureStatisticsAsync(
        Guid organizationId, Guid assetId, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assetId && a.OrganizationId == organizationId, cancellationToken);
        if (asset is null) return null;

        var completedCorrectiveRepairs = await db.MaintenanceRecords.AsNoTracking()
            .Where(m => m.AssetId == assetId
                && m.OrganizationId == organizationId
                && m.Type == MaintenanceType.CORRECTIVE
                && m.Status == MaintenanceStatus.COMPLETED
                && m.CompletionDate != null
                && m.ActualCost != null)
            .Select(m => new CompletedRepair(m.CompletionDate!.Value, m.ActualCost!.Value))
            .ToListAsync(cancellationToken);

        var asOfDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = statisticsEngine.Compute(completedCorrectiveRepairs, asOfDate);

        return new FailureStatisticsDto
        {
            AssetId = asset.Id,
            AssetCode = asset.AssetCode,
            RepairCount = result.RepairCount,
            MeanTimeBetweenFailuresDays = result.MeanTimeBetweenFailuresDays,
            CostTrend = result.CostTrend,
            ProjectedNextTwelveMonthsCost = result.ProjectedNextTwelveMonthsCost,
            EvaluatedAsOf = asOfDate,
        };
    }

    public async Task<IReadOnlyList<FailureStatisticsDto>> ComputeFleetFailureStatisticsAsync(
        Guid organizationId, Guid assetTypeId, Guid? assetId, CancellationToken cancellationToken)
    {
        var assets = await db.Assets.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId
                && a.AssetTypeId == assetTypeId
                && a.Status != AssetStatuses.Disposed
                && (assetId == null || a.Id == assetId))
            .Select(a => new { a.Id, a.AssetCode })
            .ToListAsync(cancellationToken);
        if (assets.Count == 0) return [];

        var assetIds = assets.Select(a => a.Id).ToList();
        var repairsByAsset = (await db.MaintenanceRecords.AsNoTracking()
                .Where(m => assetIds.Contains(m.AssetId)
                    && m.OrganizationId == organizationId
                    && m.Type == MaintenanceType.CORRECTIVE
                    && m.Status == MaintenanceStatus.COMPLETED
                    && m.CompletionDate != null
                    && m.ActualCost != null)
                .Select(m => new { m.AssetId, Repair = new CompletedRepair(m.CompletionDate!.Value, m.ActualCost!.Value) })
                .ToListAsync(cancellationToken))
            .ToLookup(r => r.AssetId, r => r.Repair);

        var asOfDate = DateOnly.FromDateTime(DateTime.UtcNow);
        return assets
            .Select(a =>
            {
                var result = statisticsEngine.Compute(repairsByAsset[a.Id].ToList(), asOfDate);
                return new FailureStatisticsDto
                {
                    AssetId = a.Id,
                    AssetCode = a.AssetCode,
                    AssetsWithRepairs = result.RepairCount > 0 ? 1 : 0,
                    RepairCount = result.RepairCount,
                    MeanTimeBetweenFailuresDays = result.MeanTimeBetweenFailuresDays,
                    CostTrend = result.CostTrend,
                    ProjectedNextTwelveMonthsCost = result.ProjectedNextTwelveMonthsCost,
                    EvaluatedAsOf = asOfDate,
                };
            })
            .ToList();
    }
}
