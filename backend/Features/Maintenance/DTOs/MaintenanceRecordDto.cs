using System;
using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Maintenance.DTOs;

public class MaintenanceRecordDto
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ObservedCondition { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public MaintenanceType Type { get; set; }
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public string? WorkPerformed { get; set; }
    public DateOnly? CompletionDate { get; set; }
    public string? ResultingCondition { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? AssigneeEmail { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
