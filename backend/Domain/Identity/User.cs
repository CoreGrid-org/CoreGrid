namespace CoreGrid.Api.Domain;

// Represents a user within an organization.
public class User
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    // The ThunderID "sub" claim this record mirrors
    public required string ExternalSubjectId { get; set; }

    public required string Email { get; set; }
    public required string GivenName { get; set; }
    public required string FamilyName { get; set; }

    public CoreGridRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<AssetHistory> AssetHistoryEntries { get; set; } = new List<AssetHistory>();
}
