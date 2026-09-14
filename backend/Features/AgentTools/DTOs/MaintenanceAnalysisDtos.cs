namespace CoreGrid.Api.Features.AgentTools.DTOs;

// get_maintenance_history (§7.4) — Maintenance Analysis Agent tool (node 2).
public class MaintenanceHistoryDto
{
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public List<MaintenanceHistoryEntryDto> Records { get; set; } = [];
}

public class MaintenanceHistoryEntryDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty; // CORRECTIVE | PREVENTIVE
    public string Status { get; set; } = string.Empty;
    public DateOnly? CompletionDate { get; set; }
    public decimal? ActualCost { get; set; }
    public string? ResultingCondition { get; set; }
    public string Description { get; set; } = string.Empty;
}

// compute_failure_statistics (§7.4) — Maintenance Analysis Agent tool
// (node 2's job per SRS §7.3: "repair count, MTBF, cost trend, 12-month
// projection"). Node 2 doesn't produce a recommendation like node 4 does —
// it assembles facts for nodes 3/4 to use, so this is exactly that: numbers,
// not a REPAIR/REPLACE-style verdict.
public class FailureStatisticsDto
{
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public int RepairCount { get; set; }
    public decimal? MeanTimeBetweenFailuresDays { get; set; }
    public string CostTrend { get; set; } = string.Empty; // INCREASING | DECREASING | STABLE | INSUFFICIENT_DATA
    public decimal ProjectedNextTwelveMonthsCost { get; set; }
    public DateOnly EvaluatedAsOf { get; set; }
}
