namespace CoreGrid.Api.Features.Shared.Storage;

// Generic file storage abstraction — feature controllers depend on this, not
// on Cloudflare R2 directly, so a future feature reusing uploads doesn't
// need to know which object-storage provider is behind it.
public interface IFileStorageService
{
    // Uploads content under a key prefixed by `folder` (e.g. "maintenance")
    // and returns the publicly reachable URL to store on the owning record.
    Task<string> UploadAsync(
        string folder,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken);

    // FR-034: unlike UploadAsync above, the object isn't meant to be
    // reachable by anyone who has the URL — returns the object *key*, not a
    // URL, so the owning record can't accidentally leak a permanent public
    // link. A caller mints a fresh, short-lived URL via GetPresignedUrlAsync
    // only after checking the requester is actually permitted to read the
    // owning record.
    Task<string> UploadPrivateAsync(
        string folder,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken);

    // Mints a time-limited, signed URL for an object previously stored via
    // UploadPrivateAsync. Safe to call on every read — generating one is a
    // local computation (HMAC-signs the request), not a network round trip.
    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken);
}
