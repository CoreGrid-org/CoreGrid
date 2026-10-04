namespace CoreGrid.Api.Features.Agents;

// Which chat-completions endpoint, model and key an in-process agent calls.
// Every agent shares the "Llm" section (Gemini's OpenAI-compatible endpoint by
// default); an agent's own section ("Planner", "Budget") can override any
// single value. Blank values count as unset, so an empty placeholder in
// appsettings never hides a key set in user-secrets or the environment.
//
//   Llm:Endpoint / Llm:Model / Llm:ApiKey          shared defaults
//   LlmFallback:Endpoint / :Model / :ApiKey        optional second provider (Groq by default)
//   <Agent>:Endpoint / <Agent>:Model / <Agent>:ApiKey  per-agent overrides
//   <Agent>|Llm :ReasoningEffort / :MaxTokens       optional latency/token caps
//     (sent only when set — not every provider accepts reasoning_effort)
public sealed record LlmSettings(string Endpoint, string Model, string? ApiKey)
{
    public string? ReasoningEffort { get; init; }
    public int? MaxTokens { get; init; }

    public const string DefaultEndpoint = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
    public const string DefaultModel = "gemini-3.5-flash";

    // Optional second provider, tried when the primary fails (error, 429, timeout or
    // unusable output). Groq's OpenAI-compatible endpoint by default; configured only
    // by setting LlmFallback:ApiKey.
    public const string DefaultFallbackEndpoint = "https://api.groq.com/openai/v1/chat/completions";
    public const string DefaultFallbackModel = "openai/gpt-oss-120b";

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    /// <param name="legacyApiKeys">Older key names still honoured, lowest priority.</param>
    public static LlmSettings For(IConfiguration configuration, string agent, params string[] legacyApiKeys)
    {
        string? Read(string key) => string.IsNullOrWhiteSpace(configuration[key]) ? null : configuration[key]!.Trim();

        return new LlmSettings(
            Read($"{agent}:Endpoint") ?? Read("Llm:Endpoint") ?? DefaultEndpoint,
            Read($"{agent}:Model") ?? Read("Llm:Model") ?? DefaultModel,
            Read($"{agent}:ApiKey") ?? Read("Llm:ApiKey") ?? legacyApiKeys.Select(Read).FirstOrDefault(k => k is not null))
        {
            ReasoningEffort = Read($"{agent}:ReasoningEffort") ?? Read("Llm:ReasoningEffort"),
            MaxTokens = int.TryParse(Read($"{agent}:MaxTokens") ?? Read("Llm:MaxTokens"), out var max) && max > 0 ? max : null
        };
    }

    /// <summary>The optional fallback provider (LlmFallback:Endpoint / Model / ApiKey).</summary>
    public static LlmSettings Fallback(IConfiguration configuration)
    {
        string? Read(string key) => string.IsNullOrWhiteSpace(configuration[key]) ? null : configuration[key]!.Trim();

        return new LlmSettings(
            Read("LlmFallback:Endpoint") ?? DefaultFallbackEndpoint,
            Read("LlmFallback:Model") ?? DefaultFallbackModel,
            Read("LlmFallback:ApiKey"))
        {
            ReasoningEffort = Read("LlmFallback:ReasoningEffort"),
            MaxTokens = int.TryParse(Read("LlmFallback:MaxTokens"), out var max) && max > 0 ? max : null
        };
    }

    /// <summary>
    /// The providers an agent should try, in order: its primary (For) and then the
    /// shared fallback, each only if it has an API key. Empty means "use the
    /// deterministic fallback straight away".
    /// </summary>
    public static IReadOnlyList<LlmSettings> Chain(IConfiguration configuration, string agent, params string[] legacyApiKeys) =>
        new[] { For(configuration, agent, legacyApiKeys), Fallback(configuration) }
            .Where(s => s.HasApiKey)
            .ToList();
}
