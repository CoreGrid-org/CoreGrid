namespace CoreGrid.Api.Features.Assets.DTOs;

public class AssetAttributeValueDto
{
    public Guid AttributeDefinitionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public string? ValueText { get; set; }

    public decimal? ValueNumber { get; set; }

    public DateOnly? ValueDate { get; set; }

    public bool? ValueBoolean { get; set; }
}