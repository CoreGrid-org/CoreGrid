namespace CoreGrid.Api.Features.Agents;

// Which chat-completions endpoint, model and key an in-process agent calls.
// Every agent shares the "Llm" section (Gemini's OpenAI-compatible endpoint by
// default); an agent's own section ("Planner", "Budget") can override any
// single value. Blank values count as unset, so an empty placeholder in
// appsettings never hides a key set in user-secrets or the environment.
//
//   Llm:Endpoint / Llm:Model / Llm:ApiKey          shared defaults
//   <Agent>:Endpoint / <Agent>:Model / <Agent>:ApiKey  per-agent overrides
public sealed record LlmSettings(string Endpoint, string Model, string? ApiKey)
{
    public const string DefaultEndpoint = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
    public const string DefaultModel = "gemini-3.5-flash";

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    /// <param name="legacyApiKeys">Older key names still honoured, lowest priority.</param>
    public static LlmSettings For(IConfiguration configuration, string agent, params string[] legacyApiKeys)
    {
        string? Read(string key) => string.IsNullOrWhiteSpace(configuration[key]) ? null : configuration[key]!.Trim();

        return new LlmSettings(
            Read($"{agent}:Endpoint") ?? Read("Llm:Endpoint") ?? DefaultEndpoint,
            Read($"{agent}:Model") ?? Read("Llm:Model") ?? DefaultModel,
            Read($"{agent}:ApiKey") ?? Read("Llm:ApiKey") ?? legacyApiKeys.Select(Read).FirstOrDefault(k => k is not null));
    }
}
