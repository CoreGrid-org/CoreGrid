using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Verification.DTOs;

// FR-065: campaign completion report — assets in scope, verified,
// outstanding, and discrepancies broken down by classification and by
// resolution status. FR-084/FR-085 export this same shape as PDF or CSV.
public class CampaignReportDto
{
    public Guid CampaignId { get; set; }
    public required string CampaignName { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public required string Scope { get; set; }
    public CampaignStatus Status { get; set; }

    public int AssetsInScope { get; set; }
    public int Verified { get; set; }
    public int Outstanding { get; set; }

    public List<CampaignReportCount> DiscrepanciesByClassification { get; set; } = [];
    public List<CampaignReportCount> DiscrepanciesByResolutionStatus { get; set; } = [];

    public List<CampaignReportTaskRow> Tasks { get; set; } = [];
    public List<CampaignReportDiscrepancyRow> Discrepancies { get; set; } = [];

    public DateTimeOffset GeneratedAt { get; set; }
}

public class CampaignReportCount
{
    public required string Label { get; set; }
    public int Count { get; set; }
}

public class CampaignReportTaskRow
{
    public required string AssetCode { get; set; }
    public required string AssetName { get; set; }
    public VerificationTaskStatus Status { get; set; }
    public string? AssignedToEmail { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class CampaignReportDiscrepancyRow
{
    public required string AssetCode { get; set; }
    public DiscrepancyType Type { get; set; }
    public DiscrepancyStatus Status { get; set; }
    public bool IsAutomatic { get; set; }
    public string? RaisedByEmail { get; set; }
    public required string Description { get; set; }
    public string? ResolutionType { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}
