using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace CoreGrid.Api.Features.Shared.Storage;

// Cloudflare R2 is S3-compatible, so this uses the standard AWS SDK for .NET
// pointed at R2's endpoint instead of a separate Cloudflare-specific SDK.
// Config (CloudflareR2:AccountId/AccessKeyId/SecretAccessKey/BucketName/
// PublicBaseUrl) is intentionally left blank in appsettings.json — set the
// three credential values via `dotnet user-secrets set "CloudflareR2:..." "..."`
// in dev (or the CLOUDFLAREr2__... environment variables in prod) once the
// bucket exists; this throws a clear error on first use until then, the same
// pattern as GeminiPolicyComplianceLlmClient's now-removed API key check.
public class CloudflareR2StorageService(IConfiguration configuration) : IFileStorageService
{
    public async Task<string> UploadAsync(
        string folder,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var publicBaseUrl = RequireConfig("CloudflareR2:PublicBaseUrl");
        var key = await PutObjectAsync(folder, fileName, contentType, content, cancellationToken);
        return $"{publicBaseUrl.TrimEnd('/')}/{key}";
    }

    public Task<string> UploadPrivateAsync(
        string folder,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
        => PutObjectAsync(folder, fileName, contentType, content, cancellationToken);

    public async Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken)
    {
        using var client = BuildClient();
        var bucketName = RequireConfig("CloudflareR2:BucketName");

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET,
        };

        return await client.GetPreSignedURLAsync(request);
    }

    private async Task<string> PutObjectAsync(
        string folder,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var bucketName = RequireConfig("CloudflareR2:BucketName");
        var key = $"{folder.Trim('/')}/{Guid.NewGuid():N}-{fileName}";

        using var client = BuildClient();

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            DisablePayloadSigning = true, // R2 doesn't support chunked/streamed payload signing
        };

        try
        {
            await client.PutObjectAsync(request, cancellationToken);
        }
        catch (AmazonS3Exception ex)
        {
            throw new InvalidOperationException($"Cloudflare R2 upload failed: {ex.Message}", ex);
        }

        return key;
    }

    private AmazonS3Client BuildClient()
    {
        var accountId = RequireConfig("CloudflareR2:AccountId");
        var accessKeyId = RequireConfig("CloudflareR2:AccessKeyId");
        var secretAccessKey = RequireConfig("CloudflareR2:SecretAccessKey");

        var s3Config = new AmazonS3Config
        {
            ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
        };

        return new AmazonS3Client(accessKeyId, secretAccessKey, s3Config);
    }

    private string RequireConfig(string key)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException(
                $"Missing required configuration '{key}'. Set the CloudflareR2 credentials " +
                "(AccountId/AccessKeyId/SecretAccessKey/BucketName/PublicBaseUrl) via " +
                "`dotnet user-secrets set` in dev before uploading photos.")
            : value;
    }
}
