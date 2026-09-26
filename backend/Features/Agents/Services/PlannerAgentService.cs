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

        // ── 3. LLM call ───────────────────────────────────────────────────────
        // Planner:OpenAiApiKey is the pre-Gemini key name, still honoured.
        var llm = LlmSettings.For(_configuration, "Planner", "Planner:OpenAiApiKey");
        if (!llm.HasApiKey)
        {
            _logger.LogWarning(
                "Planner: LLM API key not configured (Llm:ApiKey). Returning deterministic fallback plan.");
            return PlannerScopeGuard.FallbackPlan();
        }

        var model = llm.Model;
        var userMessage =
            $"Objective: {objective}\n" +
            $"Asset summary: {JsonSerializer.Serialize(summary, JsonOptions)}\n" +
            "Produce the typed plan.";

        var requestBody = new
        {
            model,
            temperature = 0,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user",   content = userMessage  }
            }
        };

        try
        {
            using var client = _httpClientFactory.CreateClient(AgentsModule.LlmHttpClient);
            using var request = new HttpRequestMessage(HttpMethod.Post, llm.Endpoint)
            {
                Headers = { { "Authorization", $"Bearer {llm.ApiKey}" } },
                Content = new StringContent(
                    JsonSerializer.Serialize(requestBody, JsonOptions),
                    Encoding.UTF8,
                    "application/json")
            };

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Planner: {Model} returned {Status}: {Body}. Using fallback plan.",
                    model, (int)response.StatusCode, errorBody);
                return PlannerScopeGuard.FallbackPlan();
            }

            var openAiResponse = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(
                JsonOptions, cancellationToken);

            var json = openAiResponse?.Choices?.FirstOrDefault()?.Message?.Content
                ?? throw new InvalidOperationException("OpenAI returned no content.");

            var plan = JsonSerializer.Deserialize<PlannerExecutionPlan>(json, JsonOptions)
                ?? throw new InvalidOperationException("OpenAI returned an empty plan object.");

            var validated = PlannerScopeGuard.ValidatePlan(plan);

            _logger.LogInformation(
                "Planner: plan created. InScope={InScope}, Steps={StepCount}",
                validated.InScope, validated.Steps.Count);

            return validated;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Planner: LLM call failed ({Type}: {Message}). Returning fallback plan.",
                ex.GetType().Name, ex.Message);
            return PlannerScopeGuard.FallbackPlan();
        }
    }

    // ── Minimal OpenAI response deserialization models ─────────────────────

    private sealed class OpenAiChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChoice>? Choices { get; set; }
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiMessage? Message { get; set; }
    }

    private sealed class OpenAiMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
