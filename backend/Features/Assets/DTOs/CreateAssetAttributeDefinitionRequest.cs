using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.Assets.DTOs;

public class CreateAssetAttributeDefinitionRequest
{
    [Required, MaxLength(100)]
    public required string Name { get; set; }

    [Required, MaxLength(20)]
    public required string DataType { get; set; } // TEXT | NUMBER | DATE | BOOLEAN | SELECT

    [Required]
    public bool? IsRequired { get; set; }

    public string? ValidationRule { get; set; }

    public List<string>? SelectOptions { get; set; }

    public int? DisplayOrder { get; set; }
}
