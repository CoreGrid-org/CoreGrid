using System;
using System.Threading.Tasks;
using CoreGrid.Api.Features.Maintenance.DTOs;

namespace CoreGrid.Api.Features.Maintenance.Services;

public interface IMaintenanceService
{
    Task<MaintenanceRecordDto?> GetMaintenanceRecordByIdAsync(Guid organizationId, Guid id);
    Task<MaintenanceRecordDto?> ReportFaultAsync(Guid organizationId, Guid currentUserId, ReportFaultRequest request);
}
