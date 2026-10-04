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
        services.AddScoped<IPolicyComplianceEvaluator, PolicyComplianceEvaluator>();
        services.AddScoped<IPolicyComplianceAgentService, PolicyComplianceAgentService>();
        services.AddScoped<IMaintenanceAnalysisAgentService, MaintenanceAnalysisAgentService>();
        services.AddScoped<IBudgetAgentClient, BudgetAgentService>();

        // Prompts and replies are compact now, so a healthy call returns in a
        // few seconds; a stalled provider is cut off sooner and the agent
        // moves to the fallback provider or its deterministic path.
        services.AddHttpClient(LlmHttpClient, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
