using System;
using System.Collections.Generic;

namespace CoreGrid.Api.Domain;

public class AssetAttributeDefinition
{
    public Guid Id { get; set; }

    public Guid AssetTypeId { get; set; }
    public AssetType? AssetType { get; set; }

    public required string Name { get; set; }
    public required string DataType { get; set; } // TEXT | NUMBER | DATE | BOOLEAN | SELECT
    public bool IsRequired { get; set; }
    public string? ValidationRule { get; set; }
    public List<string>? SelectOptions { get; set; }
    public int DisplayOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<AssetAttributeValue> AssetAttributeValues { get; set; } = new List<AssetAttributeValue>();
}
