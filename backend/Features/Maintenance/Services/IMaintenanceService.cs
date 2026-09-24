using CoreGrid.Api.Features.Maintenance.DTOs;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Scoping;

namespace CoreGrid.Api.Features.Maintenance.Services;

public interface IMaintenanceService
{
    Task<MaintenanceRecordDto?> GetMaintenanceRecordByIdAsync(Guid organizationId, DepartmentScope scope, Guid id, CancellationToken cancellationToken);

    Task<MaintenanceRecordDto?> ReportFaultAsync(Guid organizationId, Guid currentUserId, ReportFaultRequest request, CancellationToken cancellationToken);

    /// Officer creates a maintenance record directly, specifying type and priority.
    Task<MaintenanceRecordDto?> CreateMaintenanceAsync(Guid organizationId, Guid currentUserId, CreateMaintenanceRequest request, CancellationToken cancellationToken);

    /// amend classification, priority and description on a
    /// record that hasn't reached a terminal status yet.
    Task<MaintenanceRecordDto?> AmendMaintenanceAsync(Guid organizationId, Guid currentUserId, Guid maintenanceId, AmendMaintenanceRequest request, CancellationToken cancellationToken);

    ///Officer/Administrator approves a REQUESTED record, assigns it and records an estimated cost.
    /// Transitions status: REQUESTED → APPROVED.
    Task<MaintenanceRecordDto?> ApproveMaintenanceAsync(Guid organizationId, Guid currentUserId, Guid maintenanceId, ApproveMaintenanceRequest request, CancellationToken cancellationToken);

    ///Assigned officer starts an APPROVED record.
    /// Transitions status: APPROVED → IN_PROGRESS.
    Task<MaintenanceRecordDto?> StartMaintenanceAsync(Guid organizationId, Guid currentUserId, Guid maintenanceId, CancellationToken cancellationToken);

    /// Officer completes an IN_PROGRESS record.
    /// Transitions status: IN_PROGRESS → COMPLETED.
 
    Task<MaintenanceRecordDto?> CompleteMaintenanceAsync(Guid organizationId, Guid currentUserId, Guid maintenanceId, CompleteMaintenanceRequest request, CancellationToken cancellationToken);

    Task<MaintenanceRecordDto?> CancelMaintenanceAsync(Guid organizationId, Guid currentUserId, Guid maintenanceId, CancelMaintenanceRequest request, CancellationToken cancellationToken);

    /// filter by status, priority, department, asset, assignee and
    /// date range, with server-side sorting and pagination.
    Task<PagedResult<MaintenanceRecordDto>> ListMaintenanceRecordsAsync(Guid organizationId, DepartmentScope scope, MaintenanceRecordFilter filter, CancellationToken cancellationToken);
}
