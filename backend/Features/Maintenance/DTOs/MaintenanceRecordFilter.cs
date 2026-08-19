using System;
using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Maintenance.DTOs;

public class MaintenanceRecordFilter
{
    public Guid? AssetId { get; set; }
    public MaintenanceStatus? Status { get; set; }
    public MaintenanceType? Type { get; set; }
    public MaintenancePriority? Priority { get; set; }
}
