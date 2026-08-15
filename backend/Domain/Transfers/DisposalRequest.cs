namespace CoreGrid.Api.Domain;

public class DisposalRequest
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid AssetId { get; set; }
    public Asset? Asset { get; set; }

    public Guid InitiatedByUserId { get; set; }
    public User? InitiatedByUser { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }

    public DisposalMethod DisposalMethod { get; set; }

    public decimal EstimatedResidualValue { get; set; }

    public DisposalStatus Status { get; set; }

    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? DisposedAt { get; set; }

    public string? Notes { get; set; }
}

public enum DisposalMethod
{
    SCRAP,
    AUCTION,
    DONATION,
    DESTROY
}

public enum DisposalStatus
{
    PENDING,
    APPROVED,
    REJECTED,
    REVISION_REQUESTED,
    DISPOSED
}
