namespace CoreGrid.Api.Features.Assets.DTOs;

public class AssetAttributeValueRequest
{
    public Guid AssetAttributeDefinitionId { get; set; }

    public string? ValueText { get; set; }
    public decimal? ValueNumber { get; set; }
    public DateOnly? ValueDate { get; set; }
    public bool? ValueBoolean { get; set; }
}