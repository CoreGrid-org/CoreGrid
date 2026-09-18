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

    // The individual discrepancies behind ByClassification's counts. When
    // AuditReportFilter.Page is set (the on-screen fetch), this is just that
    // one page, server-paginated, and DiscrepanciesTotalCount/Page/PageSize/
    // TotalPages describe it. The export endpoint calls GetReportAsync with
    // Page left null instead, so this holds every matching row in full —
    // exports always reflect the complete filtered set, never just one page.
    public List<AuditReportDiscrepancyRow> Discrepancies { get; set; } = [];
    public int DiscrepanciesTotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }

    public DateTimeOffset GeneratedAt { get; set; }
}

public class AuditReportClassificationRow
{
    public required string Classification { get; set; }
    public int Raised { get; set; }
    public int Resolved { get; set; }
}

public class AuditReportDiscrepancyRow
{
    public required string AssetCode { get; set; }
    public required string AssetName { get; set; }
    public required string DepartmentName { get; set; }
    public required string Classification { get; set; }
    public required string Status { get; set; }
    public required DateTimeOffset RaisedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

public class AuditReportFilter
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? AssetCategoryId { get; set; }
    public string? Status { get; set; } // "Open" | "Resolved" | null (all)

    // Null (the export endpoint's call) = return every matching discrepancy
    // row, unpaginated. Set (the on-screen fetch) = server-paginate the
    // Discrepancies list; the aggregate stats above are unaffected either way.
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}
