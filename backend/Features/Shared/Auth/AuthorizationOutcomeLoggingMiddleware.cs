using CoreGrid.Api.Features.Shared.CurrentUser;

namespace CoreGrid.Api.Features.Shared.Auth;

// Logs authorization failures with the request and user context.
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
