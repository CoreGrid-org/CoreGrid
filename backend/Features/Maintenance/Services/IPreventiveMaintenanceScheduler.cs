namespace CoreGrid.Api.Features.Maintenance.Services;

// FR-041: schedule a preventive maintenance record when the maintenance
// interval configured on an asset type has elapsed since the last
// completed maintenance. Pulled out of PreventiveMaintenanceBackgroundService
// as its own DB-scoped, unit-testable service — same split as
// MaintenanceAnalysisToolsService/IFailureStatisticsEngine (a thin
// BackgroundService wrapper around a service that can be constructed and
// exercised directly in a test, without a 24-hour Task.Delay in the way).
public interface IPreventiveMaintenanceScheduler
{
    // Returns the number of new PREVENTIVE records created.
    Task<int> ScheduleDueMaintenanceAsync(DateOnly asOfDate, CancellationToken cancellationToken);
}
