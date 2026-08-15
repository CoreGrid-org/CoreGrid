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
