namespace CoreGrid.Api.Features.Dashboard;

// Represents the dashboard summary metrics.
public record DashboardSummary(
    int TotalAssets,
    int ActiveAssets,
    int AssetsUnderMaintenance,
    int PendingTransfers,
    int PendingDisposals,
    int OpenDiscrepancies,
    int WorkflowsAwaitingApproval);

// Represents the data used by dashboard charts.
public record ChartDatum(string Label, int Value);

public record MaintenanceCostDatum(string Label, decimal Value);

public record DashboardCharts(
    List<ChartDatum> AssetsByDepartment,
    List<ChartDatum> AssetsByCondition,
    List<MaintenanceCostDatum> MaintenanceCostByMonth);
