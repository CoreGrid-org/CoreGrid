using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.AgentTools.Services;

// Maintenance Analysis Agent tools (SRS §7.3 node 2, §7.4): get_maintenance_history
// and compute_failure_statistics. DB-facing wrapper around the pure
// IFailureStatisticsEngine — mirrors AgentToolsService/PolicyComplianceAgentService's
// split of "load facts from the DB" vs. "pure, unit-tested computation."
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
}
