namespace CoreGrid.Api.Domain;

// Tenant record mirroring a ThunderID sub-organisation (SRS §4.2, §8.2).
// CoreGrid never stores credentials here — this is a local mirror only.
public class Organization
{
    public Guid Id { get; set; }

    // The ThunderID sub-organisation identifier this record mirrors.
    public required string ExternalOrgId { get; set; }

    public required string Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
