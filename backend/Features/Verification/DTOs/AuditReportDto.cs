namespace CoreGrid.Api.Features.Verification.DTOs;

// Represents the audit report summary and discrepancy details.
public class AuditReportDto
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }

    public int CampaignsInPeriod { get; set; }
    public int AssetsVerified { get; set; }
    public int AssetsInScope { get; set; }
    public int OpenDiscrepancies { get; set; }

    public List<AuditReportClassificationRow> ByClassification { get; set; } = [];

    // Defines the filters for audit report queries.
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

       // Controls pagination of discrepancy results.
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}
