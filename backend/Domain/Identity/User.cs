namespace CoreGrid.Api.Domain;

// Local mirror of a ThunderID identity (SRS §4.7). Holds no credentials and
// is never authoritative for authentication (identity/password stay with
// ThunderID) — but Role is authoritative here: RoleEnrichmentMiddleware
// rehydrates every request's `roles` claim from this column, so CoreGrid's
// own role/department administration takes effect immediately rather than
// waiting on a ThunderID-issued token.
public class User
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    // The ThunderID "sub" claim this record mirrors (SRS §4.2, §4.4).
    public required string ExternalSubjectId { get; set; }

    public required string Email { get; set; }
    public required string GivenName { get; set; }
    public required string FamilyName { get; set; }

    public CoreGridRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<AssetHistory> AssetHistoryEntries { get; set; } = new List<AssetHistory>();
}
