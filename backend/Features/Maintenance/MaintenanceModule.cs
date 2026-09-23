using CoreGrid.Api.Features.Maintenance.Services;

namespace CoreGrid.Api.Features.Maintenance;

public static class MaintenanceModule
{
    public static IServiceCollection AddMaintenanceFeature(this IServiceCollection services)
    {
        services.AddScoped<IMaintenanceService, MaintenanceService>();
        services.AddScoped<IPreventiveMaintenanceScheduler, PreventiveMaintenanceScheduler>();
        services.AddHostedService<PreventiveMaintenanceBackgroundService>();

        return services;
    }
}
