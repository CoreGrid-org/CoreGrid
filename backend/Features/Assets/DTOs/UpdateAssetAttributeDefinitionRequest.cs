using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.Assets.DTOs;

// Defines the request for updating an asset attribute definition.
public class UpdateAssetAttributeDefinitionRequest
{
    [Required, MaxLength(100)]
    public required string Name { get; set; }

    [Required, MaxLength(20)]
    public required string DataType { get; set; } // TEXT | NUMBER | DATE | BOOLEAN | SELECT

    [Required]
    public bool? IsRequired { get; set; }

    public string? ValidationRule { get; set; }

    public List<string>? SelectOptions { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int? DisplayOrder { get; set; }
}
