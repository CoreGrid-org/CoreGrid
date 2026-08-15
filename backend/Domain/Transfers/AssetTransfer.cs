namespace CoreGrid.Api.Domain;

public class AssetTransfer
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    // TODO: add FK once Asset entity exists (Component A)
    public Guid AssetId { get; set; }

    // TODO: add FK once Department exists
    public Guid FromDepartmentId { get; set; }

    // TODO: add FK once Department exists
    public Guid ToDepartmentId { get; set; }

    // TODO: add FK once Location exists
    public Guid FromLocationId { get; set; }

    // TODO: add FK once Location exists
    public Guid ToLocationId { get; set; }

    public Guid InitiatedByUserId { get; set; }
    public User? InitiatedByUser { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }

    public Guid? ConfirmedByUserId { get; set; }
    public User? ConfirmedByUser { get; set; }

    public TransferStatus Status { get; set; }

    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }

    public string? RejectionReason { get; set; }
}

public enum TransferStatus
{
    REQUESTED,
    APPROVED,
    IN_TRANSIT,
    COMPLETED,
    REJECTED,
    CANCELLED
}
