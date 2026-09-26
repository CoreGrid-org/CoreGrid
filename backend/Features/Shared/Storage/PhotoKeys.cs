using CoreGrid.Api.Features.Shared.Exceptions;

namespace CoreGrid.Api.Features.Shared.Storage;

// Where uploaded photos live in storage, the check that a key a client sends
// back on a save really is one of its own organisation's uploads, and turning
// a stored key into something a browser can show. The upload endpoints return
// an object key; the create/report/raise request echoes it. Without the check
// a client could submit any key (another organisation's photo included) and
// then be handed a signed URL to it on every read.
public static class PhotoKeys
{
    // How long a signed photo link stays valid. Reads mint a fresh one each time.
    public static readonly TimeSpan LinkLifetime = TimeSpan.FromMinutes(15);

    public const string Maintenance = "maintenance";
    public const string Verification = "verification";

    public static string Folder(string kind, Guid organizationId) => $"{kind}/{organizationId:N}";
    public static string MaintenanceFolder(Guid organizationId) => Folder(Maintenance, organizationId);

    // Content-type-derived, so a user's original file name (spaces, unicode,
    // path characters, personal details) never ends up in an object key.
    public static string FileNameFor(string contentType) => contentType switch
    {
        "image/png" => "photo.png",
        "image/webp" => "photo.webp",
        _ => "photo.jpg",
    };

    /// <summary>Returns the key if it's one of this organisation's uploads of that kind; throws otherwise; null stays null.</summary>
    public static string? RequireOwn(string kind, string? key, Guid organizationId, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        var trimmed = key.Trim();
        if (!trimmed.StartsWith(Folder(kind, organizationId) + "/", StringComparison.Ordinal) || trimmed.Contains(".."))
        {
            throw new ValidationException(fieldName, "The photo reference isn't valid. Upload the photo again.");
        }

        return trimmed;
    }

    public static string? RequireOwnMaintenancePhoto(string? key, Guid organizationId, string fieldName) =>
        RequireOwn(Maintenance, key, organizationId, fieldName);

    /// <summary>
    /// A stored photo reference as a URL a browser can load: a fresh signed
    /// link for a private object key, or unchanged for an older full URL.
    /// Null when there's no photo or storage can't sign one right now, so a
    /// raw key is never handed out as if it were a URL.
    /// </summary>
    public static async Task<string?> ToDisplayUrlAsync(IFileStorageService storage, string? stored, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stored)) return null;
        if (stored.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || stored.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return stored;
        }
        if (!storage.IsConfigured) return null;

        try
        {
            return await storage.GetPresignedUrlAsync(stored, LinkLifetime, cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
