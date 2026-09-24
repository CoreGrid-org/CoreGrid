using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.Assets.DTOs;

public class CreateAssetRequest
{
    [Required]
    public Guid? AssetTypeId { get; set; }

    [Required]
    public Guid? DepartmentId { get; set; }

    [Required]
    public Guid? LocationId { get; set; }

    [Required, MaxLength(200)]
    public required string Name { get; set; }

    [Required]
    public DateOnly? AcquisitionDate { get; set; }

    [Required]
    [Range(0, 1_000_000_000_000)]
    public decimal? AcquisitionCost { get; set; }

    [MaxLength(20)]
    public string Condition { get; set; } = "NEW";

    public List<AssetAttributeValueRequest> Attributes { get; set; } = [];
}