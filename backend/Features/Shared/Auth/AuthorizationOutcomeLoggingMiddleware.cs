using CoreGrid.Api.Features.Shared.CurrentUser;

namespace CoreGrid.Api.Features.Shared.Auth;

// SEC-ID-09: "All authentication and authorisation outcomes — success,
// failure and denial — shall be recorded with subject, organisation,
// endpoint and timestamp." Runs after UseAuthorization, so it observes the
// 401/403 that middleware already decided rather than re-deciding
// anything itself. Never logs the token itself (SEC-ID-05) — only claims
// already present on the (possibly unauthenticated) principal. The one
// outcome this can't see is RoleEnrichmentMiddleware's own early 401 for a
// missing/deactivated user — that middleware runs earlier in the pipeline
// and short-circuits without calling next(), so it logs that case itself.
public class AuthorizationOutcomeLoggingMiddleware(RequestDelegate next, ILogger<AuthorizationOutcomeLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, CurrentUserContext currentUserContext)
    {
        await next(context);

        if (context.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            var subject = context.User.FindFirst("sub")?.Value ?? "anonymous";
            var organizationId = currentUserContext.IsResolved && !currentUserContext.IsServicePrincipal
                ? currentUserContext.OrganizationId.ToString()
                : "unknown";

            logger.LogWarning(
                "Authorization outcome {StatusCode} for {Subject} (org {OrganizationId}) on {Method} {Path} at {Timestamp:o}",
                context.Response.StatusCode, subject, organizationId, context.Request.Method, context.Request.Path, DateTimeOffset.UtcNow);
        }
    }
}
