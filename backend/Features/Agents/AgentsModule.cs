using CoreGrid.Api.Features.Agents.Services;

namespace CoreGrid.Api.Features.Agents;

public static class AgentsModule
{
    // Shared by every agent that calls the configured LLM (see LlmSettings).
    public const string LlmHttpClient = "Llm";

    public static IServiceCollection AddAgentsFeature(this IServiceCollection services)
    {
        services.AddScoped<IPolicyRuleEngine, PolicyRuleEngine>();
        services.AddScoped<IAgentWorkflowService, AgentWorkflowService>();
        services.AddScoped<IPlannerAgentClient, PlannerAgentService>();
        services.AddScoped<IAssetActionRecommendationEngine, AssetActionRecommendationEngine>();
        services.AddScoped<IPolicyComplianceAgentService, PolicyComplianceAgentService>();
        services.AddScoped<IMaintenanceAnalysisAgentService, MaintenanceAnalysisAgentService>();
        services.AddScoped<IBudgetAgentClient, BudgetAgentService>();

        // Gemini's "thinking" models can take longer than 30s on a full
        // plan/assessment; the agents fall back deterministically on timeout.
        services.AddHttpClient(LlmHttpClient, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }
}
