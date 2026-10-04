using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.AgentTools.Services;

public sealed class MaintenanceTools(CoreGridDbContext db, IFailureStatisticsEngine statistics) : IMaintenanceTools
{
    public async Task<MaintenanceHistoryDto?> GetMaintenanceHistoryAsync(Guid organizationId, Guid assetId, CancellationToken cancellationToken = default)
    {
        var asset = await db.Assets.AsNoTracking()
            .Matching(AssetSelection.Single(organizationId, assetId))
            .Select(a => new { a.Id, a.AssetCode })
            .FirstOrDefaultAsync(cancellationToken);
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
                Description = m.Description
            })
            .ToListAsync(cancellationToken);

        return new MaintenanceHistoryDto { AssetId = asset.Id, AssetCode = asset.AssetCode, Records = records };
    }

    public async Task<IReadOnlyList<FailureStatisticsDto>> GetFailureStatisticsAsync(AssetSelection selection, CancellationToken cancellationToken = default)
    {
        var assets = await db.Assets.AsNoTracking()
            .Matching(selection)
            .Select(a => new { a.Id, a.AssetCode })
            .ToListAsync(cancellationToken);
        if (assets.Count == 0) return [];

        var assetIds = assets.Select(a => a.Id).ToList();
        var repairsByAsset = (await db.MaintenanceRecords.AsNoTracking()
                .Where(m => assetIds.Contains(m.AssetId)
                    && m.OrganizationId == selection.OrganizationId
                    && m.Type == MaintenanceType.CORRECTIVE
                    && m.Status == MaintenanceStatus.COMPLETED
                    && m.CompletionDate != null
                    && m.ActualCost != null)
                .Select(m => new { m.AssetId, Repair = new CompletedRepair(m.CompletionDate!.Value, m.ActualCost!.Value) })
                .ToListAsync(cancellationToken))
            .ToLookup(r => r.AssetId, r => r.Repair);

        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        return assets
            .Select(a =>
            {
                var result = statistics.Compute(repairsByAsset[a.Id].ToList(), asOf);
                return new FailureStatisticsDto
                {
                    AssetId = a.Id,
                    AssetCode = a.AssetCode,
                    AssetsWithRepairs = result.RepairCount > 0 ? 1 : 0,
                    RepairCount = result.RepairCount,
                    MeanTimeBetweenFailuresDays = result.MeanTimeBetweenFailuresDays,
                    CostTrend = result.CostTrend,
                    ProjectedNextTwelveMonthsCost = result.ProjectedNextTwelveMonthsCost,
                    EvaluatedAsOf = asOf
                };
            })
            .ToList();
    }
}
