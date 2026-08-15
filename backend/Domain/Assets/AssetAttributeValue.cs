using System;

namespace CoreGrid.Api.Domain;

public class AssetAttributeValue
{
    public Guid Id { get; set; }

    public Guid AssetId { get; set; }
    public Asset? Asset { get; set; }

    public Guid AssetAttributeDefinitionId { get; set; }
    public AssetAttributeDefinition? AssetAttributeDefinition { get; set; }

    public string? ValueText { get; set; }
    public decimal? ValueNumber { get; set; }
    public DateOnly? ValueDate { get; set; }
    public bool? ValueBoolean { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
