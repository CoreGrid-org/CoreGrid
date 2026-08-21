namespace CoreGrid.Api.Features.Dashboard;

// FR-081: role-appropriate indicators. "Assets under maintenance" /
// "pending transfers" / "pending disposals" read real counts against
// Component B/C's schema even though their write-side endpoints don't
// exist yet — those counts are simply 0 until Maintenance/Transfer/
// Disposal requests start getting created.
public record DashboardSummary(
    int TotalAssets,
    int ActiveAssets,
    int AssetsUnderMaintenance,
    int PendingTransfers,
    int PendingDisposals,
    int OpenDiscrepancies,
    int WorkflowsAwaitingApproval);

// FR-082: the three required visualisations. AssetsByCondition is always
// the five conditions in New→Unserviceable order with zero-fill, so the
// frontend can apply its fixed ordinal colour ramp positionally.
// MaintenanceCostByMonth is always the trailing 12 months with zero-fill,
// so a quiet month reads as zero rather than simply not appearing.
public record ChartDatum(string Label, int Value);

public record MaintenanceCostDatum(string Label, decimal Value);

public record DashboardCharts(
    List<ChartDatum> AssetsByDepartment,
    List<ChartDatum> AssetsByCondition,
    List<MaintenanceCostDatum> MaintenanceCostByMonth);
