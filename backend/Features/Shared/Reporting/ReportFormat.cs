namespace CoreGrid.Api.Features.Shared.Reporting;

public enum ReportFormat
{
    Csv,
    Pdf,
}

// Resolves the `?format=` query parameter shared by every export endpoint
// (AuditReportController, CampaignReportController and friends), replacing
// each one's own inline `switch` over the raw string.
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
