using CoreGrid.Api.Features.AgentTools.Services;

namespace CoreGrid.Api.Features.AgentTools;


// Registers services for agent tool operations.
public static class AgentToolsModule
{
    public static IServiceCollection AddAgentToolsFeature(this IServiceCollection services)
    {
        services.AddScoped<IMaintenanceAnalysisToolsService, MaintenanceAnalysisToolsService>();
        services.AddScoped<IFailureStatisticsEngine, FailureStatisticsEngine>();
        services.AddScoped<IAgentToolsService, AgentToolsService>();

        return services;
    }
}
