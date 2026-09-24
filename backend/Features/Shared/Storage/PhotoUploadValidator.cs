namespace CoreGrid.Api.Features.Shared.Storage;

public record PhotoValidationResult(bool IsValid, string? Error);

// One implementation of "is this really a photo" (NFR-13), replacing the
// two copies that previously lived directly in MaintenanceController and
// DiscrepanciesController. Checks size and declared Content-Type first
// (cheap, rejects the common bad case immediately) then sniffs the first
// bytes against each allowed format's magic number — a client can lie
// about Content-Type, but it can't make a non-image file start with a
// JPEG/PNG/WebP signature without also being that format as far as any
// downstream image decoder is concerned.
public static class PhotoUploadValidator
{
    public const long MaxSizeBytes = 5 * 1024 * 1024; // 5MB, matches ReportFaultPage's stated limit
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public static async Task<PhotoValidationResult> ValidateAsync(IFormFile photo, CancellationToken cancellationToken)
    {
        if (photo.Length == 0)
        {
            return new PhotoValidationResult(false, "No file was uploaded.");
        }

        if (photo.Length > MaxSizeBytes)
        {
            return new PhotoValidationResult(false, "Photo must be 5MB or smaller.");
        }

        if (!AllowedContentTypes.Contains(photo.ContentType))
        {
            return new PhotoValidationResult(false, "Only JPEG, PNG or WebP photos are accepted.");
        }

        var header = new byte[12];
        await using (var stream = photo.OpenReadStream())
        {
            var read = await stream.ReadAsync(header.AsMemory(0, (int)Math.Min(header.Length, photo.Length)), cancellationToken);
            if (read < header.Length && photo.Length >= header.Length)
            {
                return new PhotoValidationResult(false, "Could not read the uploaded file.");
            }
        }

        if (!MatchesAnySignature(header))
        {
            return new PhotoValidationResult(false, "The file's contents do not match a JPEG, PNG or WebP image.");
        }

        return new PhotoValidationResult(true, null);
    }

    private static bool MatchesAnySignature(byte[] header) =>
        IsJpeg(header) || IsPng(header) || IsWebp(header);

    // JPEG: FF D8 FF
    private static bool IsJpeg(byte[] h) =>
        h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF;

    // PNG: 89 50 4E 47 0D 0A 1A 0A
    private static bool IsPng(byte[] h) =>
        h.Length >= 8 &&
        h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47 &&
        h[4] == 0x0D && h[5] == 0x0A && h[6] == 0x1A && h[7] == 0x0A;

    // WebP: "RIFF" .... "WEBP"
    private static bool IsWebp(byte[] h) =>
        h.Length >= 12 &&
        h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F' &&
        h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P';
}
