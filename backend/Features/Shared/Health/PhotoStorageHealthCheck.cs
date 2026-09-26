using CoreGrid.Api.Features.Shared.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreGrid.Api.Features.Shared.Health;

// Reports whether photo storage (Cloudflare R2) has its credentials. Only a
// configuration check, no network call: missing storage degrades photo
// uploads, it doesn't take the rest of CoreGrid down.
public class PhotoStorageHealthCheck(IFileStorageService storage) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(storage.IsConfigured
            ? HealthCheckResult.Healthy("Cloudflare R2 configured.")
            : HealthCheckResult.Degraded("Cloudflare R2 credentials not set (CloudflareR2:AccountId/AccessKeyId/SecretAccessKey/BucketName); photo uploads are unavailable."));
}
