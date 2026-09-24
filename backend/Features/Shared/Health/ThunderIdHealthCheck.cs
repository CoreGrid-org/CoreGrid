using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreGrid.Api.Features.Shared.Health;

// Checks the availability of the ThunderID identity service.
public class ThunderIdHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var issuer = configuration["ThunderID:Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            return HealthCheckResult.Unhealthy("ThunderID:Issuer is not configured.");
        }

        try
        {
            var client = httpClientFactory.CreateClient(nameof(ThunderIdHealthCheck));
            using var response = await client.GetAsync(
                new Uri(new Uri(issuer), "/.well-known/openid-configuration"), cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("ThunderID reachable.")
                : HealthCheckResult.Degraded($"ThunderID returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ThunderID not reachable.", ex);
        }
    }
}
