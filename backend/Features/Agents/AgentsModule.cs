using CoreGrid.Api.Features.Agents.Services;

namespace CoreGrid.Api.Features.Agents;

public static class AgentsModule
{
    public static IServiceCollection AddAgentsFeature(this IServiceCollection services)
    {
        services.AddScoped<IPolicyRuleEngine, PolicyRuleEngine>();
        services.AddScoped<IAgentWorkflowService, AgentWorkflowService>();
        services.AddScoped<IPlannerAgentClient, PlannerAgentService>();
        services.AddScoped<IAssetActionRecommendationEngine, AssetActionRecommendationEngine>();
        services.AddScoped<IPolicyComplianceAgentService, PolicyComplianceAgentService>();
        services.AddScoped<IMaintenanceAnalysisAgentService, MaintenanceAnalysisAgentService>();
        services.AddScoped<IBudgetAgentClient, BudgetAgentService>();

        // Configures the HTTP client used by the Planner Agent.
        services.AddHttpClient("OpenAI", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Configures the HTTP client used by the Budget Agent.
        services.AddHttpClient("Budget", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
