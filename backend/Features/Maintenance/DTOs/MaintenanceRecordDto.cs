using System;
using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Maintenance.DTOs;

public class MaintenanceRecordDto
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;

    // FR-084: Reports > Maintenance groups by asset type (equipment
    // category, e.g. "Delivery Van"), not the individual asset.
    public string AssetTypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ObservedCondition { get; set; } = string.Empty;

    // FR-034: a real, freshly-minted, short-lived (15 min) signed URL —
    // resolved from MaintenanceRecord.PhotoObjectKey on every read, never
    // persisted as-is. Only ever populated for a caller whose role already
    // passed this endpoint's own read gate.
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
    public Guid? ReportedByUserId { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
