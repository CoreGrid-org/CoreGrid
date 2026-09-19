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

        // Planner Agent's only external dependency. The named client keeps
        // OpenAI transport settings out of workflow code and prevents an
        // unavailable model from blocking a request indefinitely;
        // PlannerAgentService safely falls back to the deterministic plan
        // when this request fails.
        services.AddHttpClient("OpenAI", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Budget Analysis Agent's outbound HTTP client. Configurable
        // endpoint supports either OpenAI or Gemini's OpenAI-compatible
        // endpoint with a 30-second timeout.
        services.AddHttpClient("Budget", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
