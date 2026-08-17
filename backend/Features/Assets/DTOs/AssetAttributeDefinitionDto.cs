namespace CoreGrid.Api.Features.Assets.DTOs;

public class AssetAttributeDefinitionDto
{
    public Guid Id { get; set; }

    public Guid AssetTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public string? ValidationRule { get; set; }

    public List<string>? SelectOptions { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }
}