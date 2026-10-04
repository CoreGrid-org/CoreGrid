namespace CoreGrid.Api.Features.Agents.Services.Llm;

public interface ILlmClient
{
    Task<T?> CompleteAsync<T>(
        string agent,
        string systemPrompt,
        object facts,
        Func<string, T> parseReply,
        CancellationToken cancellationToken) where T : class;
}
