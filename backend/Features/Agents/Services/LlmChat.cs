using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreGrid.Api.Features.Agents.Services;

// One OpenAI-compatible chat-completions call in JSON mode, shared by the agents
// that use a model. Returns the assistant message content; throws on any HTTP or
// shape failure so the caller can move on to the next provider in
// LlmSettings.Chain and, after the last one, to its deterministic fallback.
internal static class LlmChat
{
    private const int MaxLoggedErrorChars = 300;

    public static async Task<string> CompleteJsonAsync(
        HttpClient client,
        LlmSettings llm,
        string systemPrompt,
        string userMessage,
        double temperature,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object>
        {
            ["model"] = llm.Model,
            ["temperature"] = temperature,
            ["response_format"] = new { type = "json_object" },
            ["messages"] = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage },
            },
        };
        if (llm.MaxTokens is { } maxTokens) body["max_tokens"] = maxTokens;
        if (!string.IsNullOrEmpty(llm.ReasoningEffort)) body["reasoning_effort"] = llm.ReasoningEffort;

        using var request = new HttpRequestMessage(HttpMethod.Post, llm.Endpoint)
        {
            Headers = { { "Authorization", $"Bearer {llm.ApiKey}" } },
            Content = new StringContent(JsonSerializer.Serialize(body, jsonOptions), Encoding.UTF8, "application/json"),
        };

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            if (error.Length > MaxLoggedErrorChars) error = error[..MaxLoggedErrorChars] + "…";
            throw new HttpRequestException($"{llm.Model} returned {(int)response.StatusCode}: {error}", null, response.StatusCode);
        }

        var chat = await response.Content.ReadFromJsonAsync<ChatResponse>(jsonOptions, cancellationToken);
        return chat?.Choices?.FirstOrDefault()?.Message?.Content
            ?? throw new InvalidOperationException($"{llm.Model} returned no message content.");
    }

    private sealed class ChatResponse
    {
        [JsonPropertyName("choices")] public List<Choice>? Choices { get; set; }
    }

    private sealed class Choice
    {
        [JsonPropertyName("message")] public Message? Message { get; set; }
    }

    private sealed class Message
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }
}
