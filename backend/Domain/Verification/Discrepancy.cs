namespace CoreGrid.Api.Domain;

// Represents an asset verification discrepancy.
public class Discrepancy
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

// Optional verification campaign associated with the discrepancy.
    public Guid? CampaignId { get; set; }
    public VerificationCampaign? Campaign { get; set; }

    public Guid? VerificationTaskId { get; set; }
    public VerificationTask? VerificationTask { get; set; }

    public Guid AssetId { get; set; }
    public Asset? Asset { get; set; }

    public DiscrepancyType Type { get; set; }

    public bool IsAutomatic { get; set; }

    public Guid? RaisedByUserId { get; set; }
    public User? RaisedByUser { get; set; }

    public required string Description { get; set; }

   // Stores the URL of the discrepancy photo.
    public string? PhotoUrl { get; set; }

    public DiscrepancyStatus Status { get; set; }


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
