using System;
using System.Threading.Tasks;
using CoreGrid.Api.Features.Maintenance.DTOs;

namespace CoreGrid.Api.Features.Maintenance.Services;

public interface IMaintenanceService
{
    Task<MaintenanceRecordDto?> GetMaintenanceRecordByIdAsync(Guid organizationId, Guid id);
    Task<MaintenanceRecordDto?> ReportFaultAsync(Guid organizationId, Guid currentUserId, ReportFaultRequest request);

    /// FR-035 - Officer creates a maintenance record directly, specifying type and priority.
    Task<MaintenanceRecordDto?> CreateMaintenanceAsync(Guid organizationId, Guid currentUserId, CreateMaintenanceRequest request);

    /// FR-036 - Officer/Administrator approves a REQUESTED record, assigns it and records an estimated cost.
    /// Transitions status: REQUESTED → APPROVED.
    Task<MaintenanceRecordDto?> ApproveMaintenanceAsync(Guid organizationId, Guid currentUserId, Guid maintenanceId, ApproveMaintenanceRequest request);

    /// FR-037 / FR-039 - Assigned officer starts an APPROVED record.
    /// Transitions status: APPROVED → IN_PROGRESS.
    Task<MaintenanceRecordDto?> StartMaintenanceAsync(Guid organizationId, Guid currentUserId, Guid maintenanceId);

    /// FR-038 / FR-040 — Officer completes an IN_PROGRESS record.
    /// Transitions status: IN_PROGRESS → COMPLETED.
    /// Updates asset condition, returns asset to ACTIVE (or CONDEMNED for UNSERVICEABLE — BR2).
    /// Recalculates cumulative cost, repair count and last-repair date (FR-040).
    /// Enforces cost-variance tolerance (BR1). All changes are atomic (BR3).

    Task<MaintenanceRecordDto?> CompleteMaintenanceAsync(Guid organizationId, Guid currentUserId, Guid maintenanceId, CompleteMaintenanceRequest request);
}
