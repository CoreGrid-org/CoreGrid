namespace CoreGrid.Api.Domain;

// Represents an asset verification campaign.
public class VerificationCampaign
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public required string Name { get; set; }

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    public Guid? ScopeDepartmentId { get; set; }
    public Department? ScopeDepartment { get; set; }

    public Guid? ScopeLocationId { get; set; }
    public Location? ScopeLocation { get; set; }

    public Guid? ScopeAssetCategoryId { get; set; }
    public AssetCategory? ScopeAssetCategory { get; set; }

    public Guid? ScopeAssetTypeId { get; set; }
    public AssetType? ScopeAssetType { get; set; }

    public CampaignStatus Status { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<VerificationTask> Tasks { get; set; } = new List<VerificationTask>();
}

public enum CampaignStatus
{
    Active,
    Completed,
    Cancelled
}
