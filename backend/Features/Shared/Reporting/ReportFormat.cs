namespace CoreGrid.Api.Features.Shared.Reporting;

public enum ReportFormat
{
    Csv,
    Pdf,
}

// Resolves the requested report export format.
public static class ReportFormatResolver
{
    public static bool TryResolve(string? format, out ReportFormat resolved)
    {
        switch (format?.Trim().ToLowerInvariant())
        {
            case "pdf":
                resolved = ReportFormat.Pdf;
                return true;
            case "csv":
                resolved = ReportFormat.Csv;
                return true;
            default:
                resolved = default;
                return false;
        }
    }

    public const string UnsupportedFormatMessage = "Unsupported export format. Use 'pdf' or 'csv'.";
}
