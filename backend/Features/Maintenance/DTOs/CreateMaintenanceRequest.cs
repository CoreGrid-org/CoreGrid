using System;
using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Maintenance.DTOs;

/// <summary>
/// Request body for FR-035: an Inventory Officer creates a maintenance record
/// directly, choosing type (CORRECTIVE / PREVENTIVE) and priority explicitly.
/// </summary>
public class CreateMaintenanceRequest
{
    /// <summary>The asset this maintenance record is for.</summary>
    public Guid AssetId { get; set; }

    /// <summary>Free-text description of the work required.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Observed condition of the asset: NEW | GOOD | FAIR | POOR | UNSERVICEABLE.
    /// </summary>
    public string ObservedCondition { get; set; } = string.Empty;

    /// <summary>Optional URL of an attached photograph.</summary>
    public string? PhotoUrl { get; set; }

    /// <summary>CORRECTIVE or PREVENTIVE.</summary>
    public MaintenanceType Type { get; set; }

    /// <summary>LOW | MEDIUM | HIGH | CRITICAL.</summary>
    public MaintenancePriority Priority { get; set; }

    /// <summary>Optional estimated cost for the work.</summary>
    public decimal? EstimatedCost { get; set; }

    /// <summary>
    /// Optional — pre-assign the record to a responsible officer at creation
    /// time. The officer must belong to the same organisation.
    /// </summary>
    public Guid? AssigneeId { get; set; }
}
