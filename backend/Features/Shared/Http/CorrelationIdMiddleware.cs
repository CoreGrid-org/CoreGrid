namespace CoreGrid.Api.Features.Shared.Http;

// §5.4: every response carries a correlation id, honouring one the caller
// already supplied (useful when a frontend or another service is chaining
// requests) or minting a fresh one. Stashed on HttpContext.Items so
// ApiExceptionFilter, InvalidModelStateResponseFactory and
// AuditSaveChangesInterceptor can all attach the same id to whatever they
// each produce for this request, without re-deriving or re-generating it.
public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
