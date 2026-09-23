namespace CoreGrid.Api.Domain;

// Represents an organization in the system.
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
