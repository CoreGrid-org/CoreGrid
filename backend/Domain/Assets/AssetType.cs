using System;
using System.Collections.Generic;

namespace CoreGrid.Api.Domain;

public class AssetType
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid AssetCategoryId { get; set; }
    public AssetCategory? AssetCategory { get; set; }

    public required string Code { get; set; }
    public required string Name { get; set; }
    public int UsefulLifeYears { get; set; }
    public int? DefaultMaintenanceIntervalDays { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<AssetAttributeDefinition> AssetAttributeDefinitions { get; set; } = new List<AssetAttributeDefinition>();
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    public ICollection<OrganizationPolicy> OrganizationPolicies { get; set; } = new List<OrganizationPolicy>();
}
