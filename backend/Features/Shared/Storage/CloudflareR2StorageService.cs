using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using CoreGrid.Api.Features.Shared.Exceptions;
using Microsoft.Extensions.Configuration;

namespace CoreGrid.Api.Features.Shared.Storage;

// Cloudflare R2 is S3-compatible, so this uses the standard AWS SDK for .NET
// pointed at R2's endpoint instead of a separate Cloudflare-specific SDK.
//
// Configuration (CloudflareR2 section):
//   AccountId, AccessKeyId, SecretAccessKey, BucketName  required
//   PublicBaseUrl                                        only for UploadAsync (public objects)
// Keep the credentials out of appsettings: `dotnet user-secrets set
// "CloudflareR2:AccessKeyId" "..."` in dev, CloudflareR2__AccessKeyId etc. as
// environment variables in production. Until they're set, uploads fail with a
// 503 "photo storage isn't configured" rather than a generic 500.
//
// Registered as a singleton: the S3 client is thread-safe and meant to be
// reused, so it's built once on first use.
public sealed class CloudflareR2StorageService(IConfiguration configuration, ILogger<CloudflareR2StorageService> logger)
    : IFileStorageService, IDisposable
{
    private readonly Lazy<AmazonS3Client> _client = new(() => BuildClient(configuration));

    public bool IsConfigured =>
        new[] { "AccountId", "AccessKeyId", "SecretAccessKey", "BucketName" }
            .All(k => !string.IsNullOrWhiteSpace(configuration[$"CloudflareR2:{k}"]));

    public async Task<string> UploadAsync(
        string folder,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var publicBaseUrl = RequireConfig(configuration, "CloudflareR2:PublicBaseUrl");
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
        var request = new GetPreSignedUrlRequest
        {
            BucketName = RequireConfig(configuration, "CloudflareR2:BucketName"),
            Key = key,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET,
        };

        return await _client.Value.GetPreSignedURLAsync(request);
    }

    private async Task<string> PutObjectAsync(
        string folder,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var bucketName = RequireConfig(configuration, "CloudflareR2:BucketName");
        var key = $"{folder.Trim('/')}/{Guid.NewGuid():N}-{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            DisablePayloadSigning = true, // R2 doesn't support streaming (chunked) payload signing
        };

        try
        {
            await _client.Value.PutObjectAsync(request, cancellationToken);
        }
        catch (AmazonServiceException ex)
        {
            logger.LogError(ex, "Cloudflare R2 upload to {Bucket}/{Key} failed ({Status} {ErrorCode}).",
                bucketName, key, ex.StatusCode, ex.ErrorCode);
            throw new ServiceUnavailableException("The photo couldn't be stored right now. Please try again.", "storage_upload_failed", ex);
        }
        catch (AmazonClientException ex)
        {
            logger.LogError(ex, "Could not reach Cloudflare R2 for {Bucket}/{Key}.", bucketName, key);
            throw new ServiceUnavailableException("Photo storage can't be reached right now. Please try again.", "storage_unreachable", ex);
        }

        return key;
    }

    private static AmazonS3Client BuildClient(IConfiguration configuration)
    {
        var accountId = RequireConfig(configuration, "CloudflareR2:AccountId");
        var accessKeyId = RequireConfig(configuration, "CloudflareR2:AccessKeyId");
        var secretAccessKey = RequireConfig(configuration, "CloudflareR2:SecretAccessKey");

        var s3Config = new AmazonS3Config
        {
            ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
            AuthenticationRegion = "auto",
            ForcePathStyle = true,
            // AWS SDK v4 adds CRC checksums to every request by default, which
            // R2 rejects; Cloudflare's guidance for this SDK is WHEN_REQUIRED.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };

        return new AmazonS3Client(new BasicAWSCredentials(accessKeyId, secretAccessKey), s3Config);
    }

    private static string RequireConfig(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value)
            ? throw new ServiceUnavailableException(
                "Photo storage isn't configured on this server yet, so photos can't be uploaded. Ask an administrator to set up Cloudflare R2.",
                "storage_not_configured")
            : value;
    }

    public void Dispose()
    {
        if (_client.IsValueCreated) _client.Value.Dispose();
    }
}
