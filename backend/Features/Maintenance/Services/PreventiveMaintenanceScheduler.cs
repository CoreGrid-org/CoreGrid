using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Maintenance.Services;

public class PreventiveMaintenanceScheduler(CoreGridDbContext db) : IPreventiveMaintenanceScheduler
{
    public async Task<int> ScheduleDueMaintenanceAsync(DateOnly asOfDate, CancellationToken cancellationToken)
    {
        // Processes active assets across all organizations.
        var candidateAssets = await db.Assets
            .Include(a => a.AssetType)
            .Where(a => a.Status == AssetStatuses.Active && a.AssetType != null && a.AssetType.DefaultMaintenanceIntervalDays.HasValue)
            .ToListAsync(cancellationToken);

        var created = 0;

        foreach (var asset in candidateAssets)
        {
            var interval = asset.AssetType!.DefaultMaintenanceIntervalDays!.Value;
            var referenceDate = asset.LastRepairDate ?? asset.AcquisitionDate;
            var nextMaintenanceDate = referenceDate.AddDays(interval);

            if (asOfDate < nextMaintenanceDate)
            {
                continue;
            }

            // Prevents duplicate open preventive maintenance records.
            var hasOpenRecord = await db.MaintenanceRecords.AnyAsync(
                m => m.AssetId == asset.Id
                    && m.Type == MaintenanceType.PREVENTIVE
                    && (m.Status == MaintenanceStatus.REQUESTED
                        || m.Status == MaintenanceStatus.APPROVED
                        || m.Status == MaintenanceStatus.IN_PROGRESS),
                cancellationToken);

            if (hasOpenRecord)
            {
                continue;
            }

            db.MaintenanceRecords.Add(new MaintenanceRecord
            {
                Id = Guid.NewGuid(),
                OrganizationId = asset.OrganizationId,
                AssetId = asset.Id,
                Description = $"Scheduled preventive maintenance (interval: {interval} days).",
                ObservedCondition = asset.Condition,
                Type = MaintenanceType.PREVENTIVE,
                Priority = MaintenancePriority.MEDIUM,
                Status = MaintenanceStatus.REQUESTED,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });

            created++;
        }

        if (created > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return created;
    }
}
