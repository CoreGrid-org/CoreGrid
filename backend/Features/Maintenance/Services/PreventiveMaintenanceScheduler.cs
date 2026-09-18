using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Maintenance.Services;

public class PreventiveMaintenanceScheduler(CoreGridDbContext db) : IPreventiveMaintenanceScheduler
{
    public async Task<int> ScheduleDueMaintenanceAsync(DateOnly asOfDate, CancellationToken cancellationToken)
    {
        // Cross-tenant by design — this runs unattended with no caller
        // identity, so the OrganizationId query filter (FR-006) is a no-op
        // here regardless; every asset's own OrganizationId still flows
        // onto the record it creates.
        var candidateAssets = await db.Assets
            .Include(a => a.AssetType)
            .Where(a => a.Status == "ACTIVE" && a.AssetType != null && a.AssetType.DefaultMaintenanceIntervalDays.HasValue)
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

            // Don't duplicate an already-open preventive record for this
            // asset — AssetId alone is enough to scope this (it's a Guid,
            // globally unique across organisations).
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
