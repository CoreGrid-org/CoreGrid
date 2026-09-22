namespace CoreGrid.Api.Features.Shared.Http;
// Adds standard security headers to responses.
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-Frame-Options"] = "DENY";
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            return Task.CompletedTask;
        });

        await next(context);
    }
}
