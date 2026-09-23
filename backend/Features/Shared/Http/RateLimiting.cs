using System.Text.Json;
using System.Threading.RateLimiting;
using CoreGrid.Api.Features.Shared.Api;
using CoreGrid.Api.Features.Shared.CurrentUser;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreGrid.Api.Features.Shared.Http;

// Defines the rate-limiting policies used by the application.
public static class RateLimitPolicies
{
    public const string AgentWorkflowInitiate = nameof(AgentWorkflowInitiate);
    public const string ReportExport = nameof(ReportExport);
    public const string PhotoUpload = nameof(PhotoUpload);
    public const string SetupComplete = nameof(SetupComplete);
}

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCoreGridRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                var correlationId = context.HttpContext.Items.TryGetValue("CorrelationId", out var id)
                    ? id as string
                    : context.HttpContext.TraceIdentifier;

                context.HttpContext.Response.ContentType = "application/json";
                var envelope = new ErrorEnvelope
                {
                    Message = "Too many requests. Try again shortly.",
                    Code = "rate_limited",
                    CorrelationId = correlationId
                };
                await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(envelope), cancellationToken);
            };

            options.AddPolicy(RateLimitPolicies.AgentWorkflowInitiate, PerUserOrgPartition(permitLimit: 10, window: TimeSpan.FromMinutes(1)));
            options.AddPolicy(RateLimitPolicies.ReportExport, PerUserOrgPartition(permitLimit: 20, window: TimeSpan.FromMinutes(1)));
            options.AddPolicy(RateLimitPolicies.PhotoUpload, PerUserOrgPartition(permitLimit: 30, window: TimeSpan.FromMinutes(1)));

            // Limits setup requests by client IP.
            options.AddPolicy(RateLimitPolicies.SetupComplete, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });

        return services;
    }

    private static Func<HttpContext, RateLimitPartition<string>> PerUserOrgPartition(int permitLimit, TimeSpan window) =>
        httpContext =>
        {
            var subject = httpContext.User.FindFirst("sub")?.Value;
            var currentUserContext = httpContext.RequestServices.GetRequiredService<CurrentUserContext>();
            var organizationId = currentUserContext.IsResolved && !currentUserContext.IsServicePrincipal
                ? currentUserContext.OrganizationId.ToString()
                : null;
            var key = subject is not null ? $"{organizationId}:{subject}" : "anonymous";

            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window
            });
        };
}
