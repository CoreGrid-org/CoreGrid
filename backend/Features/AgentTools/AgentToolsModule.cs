using CoreGrid.Api.Features.AgentTools.Services;

namespace CoreGrid.Api.Features.AgentTools;

public static class AgentToolsModule
{
    public static IServiceCollection AddAgentToolsFeature(this IServiceCollection services)
    {
        services.AddScoped<IFailureStatisticsEngine, FailureStatisticsEngine>();
        services.AddScoped<IPlannerTools, PlannerTools>();
        services.AddScoped<IMaintenanceTools, MaintenanceTools>();
        services.AddScoped<IBudgetTools, BudgetTools>();
        services.AddScoped<IPolicyTools, PolicyTools>();

        return services;
    }
}
