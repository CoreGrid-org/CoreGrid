namespace CoreGrid.Api.Features.Assets.DTOs;

public class AssetTypeDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid AssetCategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string CategoryCode { get; set; } = string.Empty;

    public int UsefulLifeYears { get; set; }

    public int? DefaultMaintenanceIntervalDays { get; set; }

    public int AttributeCount { get; set; }

    public bool IsActive { get; set; }
}