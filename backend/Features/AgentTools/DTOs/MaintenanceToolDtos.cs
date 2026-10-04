namespace CoreGrid.Api.Features.AgentTools.DTOs;

public class MaintenanceHistoryDto
{
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public List<MaintenanceHistoryEntryDto> Records { get; set; } = [];
}

public class MaintenanceHistoryEntryDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly? CompletionDate { get; set; }
    public decimal? ActualCost { get; set; }
    public string? ResultingCondition { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class FailureStatisticsDto
{
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public int AssetCount { get; set; } = 1;
    public int AssetsWithRepairs { get; set; }
    public int RepairCount { get; set; }
    public decimal? MeanTimeBetweenFailuresDays { get; set; }
    public string CostTrend { get; set; } = string.Empty;
    public decimal ProjectedNextTwelveMonthsCost { get; set; }
    public DateOnly EvaluatedAsOf { get; set; }
}

public static class CostTrends
{
    public const string Increasing = "INCREASING";
    public const string Decreasing = "DECREASING";
    public const string Stable = "STABLE";
    public const string InsufficientData = "INSUFFICIENT_DATA";
}
