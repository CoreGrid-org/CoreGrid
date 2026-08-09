namespace CoreGrid.Api.Domain;

// This deployment's own department record (SRS §4.2, §8.2). CoreGrid is
// self-hosted once per department, so a given deployment has exactly one
// row here — Setup creates it once and refuses to create a second. There is
// no ThunderID-side identifier to mirror: this deployment's ThunderID
// instance is single-tenant too, so it has nothing to mirror.
public class Organization
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
