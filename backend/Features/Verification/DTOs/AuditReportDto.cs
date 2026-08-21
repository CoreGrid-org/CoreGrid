namespace CoreGrid.Api.Features.Verification.DTOs;

// FR-084/FR-085: the "Audit Campaign Report" tab on the shared Reports page
// — an aggregate across every campaign/discrepancy in the caller's
// organisation for the given filters, distinct from CampaignReportDto's
// single-campaign completion report (FR-065).
public class AuditReportDto
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }

    public int CampaignsInPeriod { get; set; }
    public int AssetsVerified { get; set; }
    public int AssetsInScope { get; set; }
    public int OpenDiscrepancies { get; set; }

    public List<AuditReportClassificationRow> ByClassification { get; set; } = [];

    public DateTimeOffset GeneratedAt { get; set; }
}

public class AuditReportClassificationRow
{
    public required string Classification { get; set; }
    public int Raised { get; set; }
    public int Resolved { get; set; }
}

public class AuditReportFilter
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? AssetCategoryId { get; set; }
    public string? Status { get; set; } // "Open" | "Resolved" | null (all)
}
