namespace CoreGrid.Api.Domain;

// FR-057/FR-059: one task per in-scope asset. "Assigned to the officer
// responsible for the in-scope location" is interpreted, in the absence of
// any location-ownership concept elsewhere in the schema, as the first
// active InventoryOfficer whose Department matches the asset's Department —
// see VerificationCampaignService.
public class VerificationTask
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid CampaignId { get; set; }
    public VerificationCampaign? Campaign { get; set; }

    public Guid AssetId { get; set; }
    public Asset? Asset { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }

    public DateOnly DueDate { get; set; }

    public VerificationTaskStatus Status { get; set; }

    // The officer's assertions on completion (FR-059).
    public bool? AssertedPresent { get; set; }
    public Guid? AssertedLocationId { get; set; }
    public Location? AssertedLocation { get; set; }
    public string? AssertedCondition { get; set; }

    public Guid? CompletedByUserId { get; set; }
    public User? CompletedByUser { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Discrepancy> Discrepancies { get; set; } = new List<Discrepancy>();
}

public enum VerificationTaskStatus
{
    Pending,
    Completed
}
