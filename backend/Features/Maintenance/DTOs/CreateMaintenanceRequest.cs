using System;
using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Maintenance.DTOs;
public class CreateMaintenanceRequest
{
    public Guid AssetId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ObservedCondition { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public MaintenanceType Type { get; set; }
    public MaintenancePriority Priority { get; set; }
    public decimal? EstimatedCost { get; set; }
    public Guid? AssigneeId { get; set; }
}
