using System;
using System.Threading.Tasks;
using CoreGrid.Api.Features.Maintenance.DTOs;

namespace CoreGrid.Api.Features.Maintenance.Services;

public interface IMaintenanceService
{
    Task<MaintenanceRecordDto?> GetMaintenanceRecordByIdAsync(Guid organizationId, Guid id);
    Task<MaintenanceRecordDto?> ReportFaultAsync(Guid organizationId, Guid currentUserId, ReportFaultRequest request);

    /// <summary>
    /// FR-035 — Officer creates a maintenance record directly, specifying type and priority.
    /// </summary>
    Task<MaintenanceRecordDto?> CreateMaintenanceAsync(Guid organizationId, Guid currentUserId, CreateMaintenanceRequest request);

    /// <summary>
    /// FR-036 — Officer/Administrator approves a REQUESTED record, assigns it and records an estimated cost.
    /// Transitions status: REQUESTED → APPROVED.
    /// </summary>
    Task<MaintenanceRecordDto?> ApproveMaintenanceAsync(Guid organizationId, Guid currentUserId, Guid maintenanceId, ApproveMaintenanceRequest request);
}
