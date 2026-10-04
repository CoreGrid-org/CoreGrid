using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreGrid.Api.Features.AgentTools.Services;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

// Implements the Planner Agent service.
public sealed class PlannerAgentService : IPlannerAgentClient
{
    private const string SystemPrompt =
        "You are CoreGrid's Planner Agent. Create only an ordered execution plan " +
        "for an asset lifecycle evaluation. " +
        "The available downstream steps are MaintenanceAnalysis, BudgetAnalysis, " +
        "PolicyCompliance, and DeterministicGate. " +
        "Never invent asset facts; use the supplied asset summary. " +
        "Never approve, execute, or mutate a business action. " +
        "Return a JSON object matching this schema exactly: " +
        "{ \"inScope\": boolean, \"rejectionReason\": string|null, " +
        "\"steps\": [ { \"seq\": integer, \"agent\": string, " +
        "\"purpose\": string, \"expectedOutput\": string } ] }. " +
        "Use inScope=false and empty steps[] if the objective is outside scope.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IAgentToolsService _agentTools;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PlannerAgentService> _logger;

    public PlannerAgentService(
        IAgentToolsService agentTools,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<PlannerAgentService> logger)
    {
        _agentTools = agentTools;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<PlannerExecutionPlan> CreatePlanAsync(
        Guid assetId,
        string objective,
        Guid initiatedBy,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        // ── 1. Scope guard (no I/O, deterministic) ────────────────────────────
        var rejectionReason = PlannerScopeGuard.RejectionReason(objective);
        if (rejectionReason is not null)
        {
            _logger.LogInformation(
                "Planner: objective rejected by scope guard. Reason: {Reason}", rejectionReason);
            return PlannerScopeGuard.RejectedPlan(rejectionReason);
        }

        // ── 2. get_asset_summary tool (in-process, no HTTP) ──────────────────
        var summary = await _agentTools.GetAssetSummaryAsync(organizationId, assetId, cancellationToken);
        if (summary is null)
        {
            _logger.LogWarning(
                "Planner: asset {AssetId} not found in organisation {OrgId}.", assetId, organizationId);
            return PlannerScopeGuard.RejectedPlan(
                "The asset was not found in the initiating organisation.");
        }

        // ── 3. LLM call: primary provider, then the optional fallback ─────────
        // Planner:OpenAiApiKey is the pre-Gemini key name, still honoured.
        var providers = LlmSettings.Chain(_configuration, "Planner", "Planner:OpenAiApiKey");
        if (providers.Count == 0)
        {
            _logger.LogWarning(
                "Planner: no LLM API key configured (Llm:ApiKey / LlmFallback:ApiKey). Returning deterministic fallback plan.");
            return PlannerScopeGuard.FallbackPlan();
        }

        var userMessage =
            $"Objective: {objective}\n" +
            $"Asset summary: {JsonSerializer.Serialize(summary, JsonOptions)}\n" +
            "Produce the typed plan.";

        using var client = _httpClientFactory.CreateClient(AgentsModule.LlmHttpClient);
        foreach (var llm in providers)
        {
            try
            {
                var json = await LlmChat.CompleteJsonAsync(
                    client, llm, SystemPrompt, userMessage, temperature: 0, JsonOptions, cancellationToken);

                var plan = JsonSerializer.Deserialize<PlannerExecutionPlan>(json, JsonOptions)
                    ?? throw new InvalidOperationException($"{llm.Model} returned an empty plan object.");

                var validated = PlannerScopeGuard.ValidatePlan(plan);

                _logger.LogInformation(
                    "Planner: plan created by {Model}. InScope={InScope}, Steps={StepCount}",
                    llm.Model, validated.InScope, validated.Steps.Count);

                return validated;
            }
            // A timeout surfaces as a cancellation that the caller didn't ask for.
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Planner: {Model} failed ({Type}: {Message}); trying the next provider.",
                    llm.Model, ex.GetType().Name, ex.Message);
            }
        }

        _logger.LogError("Planner: every configured LLM provider failed. Returning fallback plan.");
        return PlannerScopeGuard.FallbackPlan();
    }
}
