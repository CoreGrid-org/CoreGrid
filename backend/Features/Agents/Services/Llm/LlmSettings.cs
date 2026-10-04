namespace CoreGrid.Api.Features.Agents.Services.Llm;

public sealed record LlmSettings(string Endpoint, string Model, string? ApiKey)
{
    public const string DefaultEndpoint = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
    public const string DefaultModel = "gemini-3.5-flash";
    public const string DefaultFallbackEndpoint = "https://api.groq.com/openai/v1/chat/completions";
    public const string DefaultFallbackModel = "openai/gpt-oss-120b";

    private const string SharedSection = "Llm";
    private const string FallbackSection = "LlmFallback";

    public string? ReasoningEffort { get; init; }
    public int? MaxTokens { get; init; }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    public static LlmSettings For(IConfiguration configuration, string agent, params string[] legacyApiKeys)
    {
        var legacyApiKey = legacyApiKeys.Select(key => Value(configuration, key)).FirstOrDefault(key => key is not null);
        return Build(
            key => Value(configuration, $"{agent}:{key}") ?? Value(configuration, $"{SharedSection}:{key}"),
            DefaultEndpoint,
            DefaultModel,
            legacyApiKey);
    }

    public static LlmSettings Fallback(IConfiguration configuration) =>
        Build(key => Value(configuration, $"{FallbackSection}:{key}"), DefaultFallbackEndpoint, DefaultFallbackModel, legacyApiKey: null);

    public static IReadOnlyList<LlmSettings> Chain(IConfiguration configuration, string agent, params string[] legacyApiKeys) =>
        new[] { For(configuration, agent, legacyApiKeys), Fallback(configuration) }
            .Where(settings => settings.HasApiKey)
            .ToList();

    private static LlmSettings Build(Func<string, string?> read, string defaultEndpoint, string defaultModel, string? legacyApiKey) =>
        new(read("Endpoint") ?? defaultEndpoint, read("Model") ?? defaultModel, read("ApiKey") ?? legacyApiKey)
        {
            ReasoningEffort = read("ReasoningEffort"),
            MaxTokens = int.TryParse(read("MaxTokens"), out var maxTokens) && maxTokens > 0 ? maxTokens : null
        };

    private static string? Value(IConfiguration configuration, string key) =>
        string.IsNullOrWhiteSpace(configuration[key]) ? null : configuration[key]!.Trim();
}
