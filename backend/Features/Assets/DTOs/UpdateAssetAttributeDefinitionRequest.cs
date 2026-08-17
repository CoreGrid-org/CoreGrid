namespace CoreGrid.Api.Features.Assets.DTOs;

public class UpdateAssetAttributeDefinitionRequest
{
    public required string Name { get; set; }

    public required string DataType { get; set; } // TEXT | NUMBER | DATE | BOOLEAN | SELECT

    public bool IsRequired { get; set; }

    public string? ValidationRule { get; set; }

    public List<string>? SelectOptions { get; set; }

    public int DisplayOrder { get; set; }
}
