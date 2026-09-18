using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.Assets.DTOs;

public class UpdateAssetTypeRequest
{
    public required string Code { get; set; }

    public required string Name { get; set; }

    [Required]
    public Guid? AssetCategoryId { get; set; }

    [Required]
    [Range(1, 100)]
    public int? UsefulLifeYears { get; set; }

    public int? DefaultMaintenanceIntervalDays { get; set; }
}
