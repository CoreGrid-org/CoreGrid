namespace CoreGrid.Api.Domain;

// FR-060/FR-061/FR-062.
public class Discrepancy
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid CampaignId { get; set; }
    public VerificationCampaign? Campaign { get; set; }

    public Guid VerificationTaskId { get; set; }
    public VerificationTask? VerificationTask { get; set; }

    public Guid AssetId { get; set; }
    public Asset? Asset { get; set; }

    public DiscrepancyType Type { get; set; }

    public bool IsAutomatic { get; set; }

    public Guid? RaisedByUserId { get; set; }
    public User? RaisedByUser { get; set; }

    public required string Description { get; set; }

    // FR-061 asks for a photograph — this codebase has no file-upload/
    // storage infrastructure yet, so this holds an externally-hosted URL
    // rather than binary content.
    public string? PhotoUrl { get; set; }

    public DiscrepancyStatus Status { get; set; }

    // FR-062 resolution.
    public string? ResolutionType { get; set; }
    public string? ResolutionExplanation { get; set; }
    public string? CorrectiveAction { get; set; }
    public bool RegisterCorrected { get; set; }

    public Guid? ResolvedByUserId { get; set; }
    public User? ResolvedByUser { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public enum DiscrepancyType
{
    Missing,
    Surplus,
    LocationMismatch,
    ConditionMismatch,
    DataMismatch,
    Other
}

public enum DiscrepancyStatus
{
    Open,
    Resolved
}
