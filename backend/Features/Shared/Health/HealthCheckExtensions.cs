using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreGrid.Api.Features.Shared.Health;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddCoreGridHealthChecks(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddHttpClient(nameof(ThunderIdHealthCheck), client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            if (environment.IsDevelopment())
            {
                // Same self-signed-certificate relaxation as the JWT bearer
                // backchannel and ThunderIdIdentityDirectory (Program.cs) —
                // ThunderID's local quick-start container.
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            return handler;
        });

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("postgresql")
            .AddCheck<ThunderIdHealthCheck>("thunderid")
            .AddCheck<PhotoStorageHealthCheck>("photo-storage");

        return services;
    }

    //JSON, per-dependency — not the framework's plain-text default.
    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration_ms = e.Value.Duration.TotalMilliseconds
            }),
            total_duration_ms = report.TotalDuration.TotalMilliseconds
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
