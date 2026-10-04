using CoreGrid.Api.Features.Agents.Services.Budget;
using CoreGrid.Api.Features.Agents.Services.Llm;
using CoreGrid.Api.Features.Agents.Services.Orchestration;
using CoreGrid.Api.Features.Agents.Services.Planner;
using CoreGrid.Api.Features.Agents.Services.Policy;

namespace CoreGrid.Api.Features.Agents;

public static class AgentsModule
{
    private static readonly TimeSpan LlmTimeout = TimeSpan.FromSeconds(30);

    public static IServiceCollection AddAgentsFeature(this IServiceCollection services)
    {
        services.AddScoped<IAgentWorkflowService, AgentWorkflowService>();
        services.AddScoped<WorkflowPipeline>();

        services.AddScoped<IPlannerAgent, PlannerAgent>();
        services.AddScoped<IBudgetAgent, BudgetAgent>();
        services.AddScoped<IPolicyComplianceEvaluator, PolicyComplianceEvaluator>();
        services.AddScoped<IPolicyRuleEngine, PolicyRuleEngine>();
        services.AddScoped<IAssetActionRecommendationEngine, AssetActionRecommendationEngine>();

        services.AddScoped<ILlmClient, LlmClient>();
        services.AddHttpClient(LlmClient.HttpClientName, client => client.Timeout = LlmTimeout);

        return services;
    }
}
