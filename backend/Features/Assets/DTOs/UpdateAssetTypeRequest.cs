using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.Assets.DTOs;

public class UpdateAssetTypeRequest
{
    [Required, MaxLength(20)]
    public required string Code { get; set; }

    [Required, MaxLength(200)]
    public required string Name { get; set; }

    [Required]
    public Guid? AssetCategoryId { get; set; }

    [Required]
    [Range(1, 100)]
    public int? UsefulLifeYears { get; set; }

    [Range(1, int.MaxValue)]
    public int? DefaultMaintenanceIntervalDays { get; set; }
}
