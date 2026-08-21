namespace CoreGrid.Api.Features.Assets.DTOs;

public class UpdateAssetTypeRequest
{
    public required string Code { get; set; }

    public required string Name { get; set; }

    public Guid AssetCategoryId { get; set; }

    public int UsefulLifeYears { get; set; }

    public int? DefaultMaintenanceIntervalDays { get; set; }
}
