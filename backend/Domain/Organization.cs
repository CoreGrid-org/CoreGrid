namespace CoreGrid.Api.Domain;

// This deployment's own customer record (SRS §4.2, §8.2). In M0, CoreGrid
// is self-hosted once per customer organisation, so a given deployment has
// exactly one row here — Setup creates it once and refuses to create a
// second. There is no ThunderID-side identifier to mirror: this
// deployment's ThunderID instance is single-tenant too, so it has nothing
// to mirror. M1 (SRS §17) repurposes this same table to hold many
// customers' rows in one shared deployment instead.
public class Organization
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<Location> Locations { get; set; } = new List<Location>();
    public ICollection<AssetCategory> AssetCategories { get; set; } = new List<AssetCategory>();
    public ICollection<AssetType> AssetTypes { get; set; } = new List<AssetType>();
    public ICollection<OrganizationPolicy> OrganizationPolicies { get; set; } = new List<OrganizationPolicy>();
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    public ICollection<AssetHistory> AssetHistoryEntries { get; set; } = new List<AssetHistory>();
}
