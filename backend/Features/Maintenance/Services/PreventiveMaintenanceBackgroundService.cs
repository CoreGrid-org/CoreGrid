using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreGrid.Api.Features.Maintenance.Services;

//  thin timer wrapper around IPreventiveMaintenanceScheduler — the
// actual scheduling logic lives there, DB-scoped and unit-testable on its
// own, without a 24-hour Task.Delay or a hosted-service lifetime in the way.
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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<IPreventiveMaintenanceScheduler>();

                var created = await scheduler.ScheduleDueMaintenanceAsync(
                    DateOnly.FromDateTime(DateTime.UtcNow), stoppingToken);

                if (created > 0)
                {
                    _logger.LogInformation("Preventive Maintenance Background Service scheduled {Count} new record(s).", created);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scheduling preventive maintenance.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }

        _logger.LogInformation("Preventive Maintenance Background Service is stopping.");
    }
}
