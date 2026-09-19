using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreGrid.Api.Features.Shared.Health;

// NFR-20: reports ThunderID reachability as its own dependency line,
// distinct from the database. Uses the OIDC discovery document — the same
// endpoint the JWT bearer handler itself polls for JWKS — as a cheap,
// unauthenticated reachability probe; a non-success response means token
// validation is about to start failing for every caller, which is exactly
// what this check exists to surface before that becomes a wave of 401s.
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
