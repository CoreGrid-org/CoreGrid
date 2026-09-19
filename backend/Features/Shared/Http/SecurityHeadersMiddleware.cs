namespace CoreGrid.Api.Features.Shared.Http;

// NFR-09/NFR-13-adjacent baseline security headers, applied to every
// response. A minimal, deliberately non-exhaustive set (§4.7) — HSTS
// itself is enabled separately via app.UseHsts() in Program.cs, which is
// ASP.NET Core's own well-tested implementation rather than a header this
// middleware sets by hand.
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
