namespace CoreGrid.Api.Features.AgentTools.DTOs;

public class AssetSummaryDto
{
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateOnly AcquisitionDate { get; set; }
    public decimal AcquisitionCost { get; set; }
}

public class AssetTypeSummaryDto
{
    public Guid AssetTypeId { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int UsefulLifeYears { get; set; }
    public int ActiveAssetCount { get; set; }
    public Dictionary<string, int> ConditionCounts { get; set; } = [];
}
