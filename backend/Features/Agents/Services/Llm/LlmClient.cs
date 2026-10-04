using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreGrid.Api.Features.Agents.Services.Llm;

public sealed class LlmClient(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<LlmClient> logger) : ILlmClient
{
    public const string HttpClientName = "Llm";

    private const string LegacyApiKey = "Planner:OpenAiApiKey";
    private const int MaxLoggedErrorChars = 300;

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<T?> CompleteAsync<T>(
        string agent,
        string systemPrompt,
        object facts,
        Func<string, T> parseReply,
        CancellationToken cancellationToken) where T : class
    {
        var providers = LlmSettings.Chain(configuration, agent, LegacyApiKey);
        if (providers.Count == 0) return null;

        var userMessage = JsonSerializer.Serialize(facts, JsonOptions);
        using var client = httpClientFactory.CreateClient(HttpClientName);

        foreach (var provider in providers)
        {
            try
            {
                var reply = parseReply(await SendAsync(client, provider, systemPrompt, userMessage, cancellationToken));
                logger.LogInformation("{Agent}: reply from {Model} accepted.", agent, provider.Model);
                return reply;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    "{Agent}: {Model} failed ({Type}: {Message}); trying the next provider.",
                    agent, provider.Model, ex.GetType().Name, ex.Message);
            }
        }

        logger.LogWarning("{Agent}: every configured LLM provider failed; using the deterministic result.", agent);
        return null;
    }

    private static async Task<string> SendAsync(
        HttpClient client,
        LlmSettings provider,
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object>
        {
            ["model"] = provider.Model,
            ["temperature"] = 0,
            ["response_format"] = new { type = "json_object" },
            ["messages"] = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            }
        };
        if (provider.MaxTokens is { } maxTokens) body["max_tokens"] = maxTokens;
        if (!string.IsNullOrEmpty(provider.ReasoningEffort)) body["reasoning_effort"] = provider.ReasoningEffort;

        using var request = new HttpRequestMessage(HttpMethod.Post, provider.Endpoint)
        {
            Headers = { { "Authorization", $"Bearer {provider.ApiKey}" } },
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            if (error.Length > MaxLoggedErrorChars) error = error[..MaxLoggedErrorChars] + "…";
            throw new HttpRequestException($"{provider.Model} returned {(int)response.StatusCode}: {error}", null, response.StatusCode);
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletion>(JsonOptions, cancellationToken);
        return completion?.Choices?.FirstOrDefault()?.Message?.Content
            ?? throw new InvalidOperationException($"{provider.Model} returned no message content.");
    }

    private sealed record ChatCompletion([property: JsonPropertyName("choices")] List<ChatChoice>? Choices);

    private sealed record ChatChoice([property: JsonPropertyName("message")] ChatMessage? Message);

    private sealed record ChatMessage([property: JsonPropertyName("content")] string? Content);
}
