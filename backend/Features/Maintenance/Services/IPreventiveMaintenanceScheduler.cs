namespace CoreGrid.Api.Features.Maintenance.Services;

// Schedules preventive maintenance for assets with due maintenance intervals.
public interface IPreventiveMaintenanceScheduler
{
    // Returns the number of new PREVENTIVE records created.
    Task<int> ScheduleDueMaintenanceAsync(DateOnly asOfDate, CancellationToken cancellationToken);
}
