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
}
