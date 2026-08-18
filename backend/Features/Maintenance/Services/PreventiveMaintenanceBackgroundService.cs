using System;
using System.Threading;
using System.Threading.Tasks;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreGrid.Api.Features.Maintenance.Services;

public class PreventiveMaintenanceBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PreventiveMaintenanceBackgroundService> _logger;

    public PreventiveMaintenanceBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<PreventiveMaintenanceBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Preventive Maintenance Background Service is starting.");

        // Run periodically, e.g., once every 24 hours
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Preventive Maintenance Background Service is working.");

            try
            {
                await SchedulePreventiveMaintenanceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scheduling preventive maintenance.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }

        _logger.LogInformation("Preventive Maintenance Background Service is stopping.");
    }

    private async Task SchedulePreventiveMaintenanceAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreGridDbContext>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Find active assets that need preventive maintenance
        var activeAssets = await dbContext.Assets
            .Include(a => a.AssetType)
            .Where(a => a.Status == "ACTIVE" && a.AssetType != null && a.AssetType.DefaultMaintenanceIntervalDays.HasValue)
            .ToListAsync(stoppingToken);

        foreach (var asset in activeAssets)
        {
            var interval = asset.AssetType!.DefaultMaintenanceIntervalDays!.Value;
            var referenceDate = asset.LastRepairDate ?? asset.AcquisitionDate;
            var nextMaintenanceDate = referenceDate.AddDays(interval);

            if (today >= nextMaintenanceDate)
            {
                // Check if a PREVENTIVE record is already OPEN (REQUESTED, APPROVED, IN_PROGRESS)
                var hasOpenRecord = await dbContext.MaintenanceRecords
                    .AnyAsync(m => m.AssetId == asset.Id 
                                && m.Type == MaintenanceType.PREVENTIVE 
                                && (m.Status == MaintenanceStatus.REQUESTED 
                                    || m.Status == MaintenanceStatus.APPROVED 
                                    || m.Status == MaintenanceStatus.IN_PROGRESS), 
                              stoppingToken);

                if (!hasOpenRecord)
                {
                    var record = new MaintenanceRecord
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = asset.OrganizationId,
                        AssetId = asset.Id,
                        Description = $"Scheduled Preventive Maintenance (Interval: {interval} days)",
                        ObservedCondition = asset.Condition,
                        Type = MaintenanceType.PREVENTIVE,
                        Priority = MaintenancePriority.MEDIUM,
                        Status = MaintenanceStatus.REQUESTED,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow,
                    };

                    dbContext.MaintenanceRecords.Add(record);
                }
            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }
}
