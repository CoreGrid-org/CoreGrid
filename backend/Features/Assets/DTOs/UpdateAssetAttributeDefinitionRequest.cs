using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.Assets.DTOs;

// Full-replace update (SRS FR-020-adjacent attribute-definition editing) —
// IsRequired and DisplayOrder are [Required] nullable so an omitted field
// fails validation instead of silently resetting to false/0 (Phase 1,
// plan §3): the create counterpart keeps DisplayOrder genuinely optional
// (auto-append), but an update has no "append" concept to fall back to.
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
