using System;

namespace CoreGrid.Api.Domain;

public class MaintenanceRecord
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid AssetId { get; set; }
    public Asset? Asset { get; set; }

    public string Description { get; set; } = string.Empty;
    public string ObservedCondition { get; set; } = string.Empty; // NEW, GOOD, FAIR, POOR, UNSERVICEABLE
    public string? PhotoUrl { get; set; }

    public MaintenanceType Type { get; set; }
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; }

    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public string? WorkPerformed { get; set; }
    public DateOnly? CompletionDate { get; set; }
    public string? ResultingCondition { get; set; } // NEW, GOOD, FAIR, POOR, UNSERVICEABLE

    public Guid? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public string? CancellationReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}

public enum MaintenanceType
{
    CORRECTIVE,
    PREVENTIVE
}

public enum MaintenancePriority
{
    LOW,
    MEDIUM,
    HIGH,
    CRITICAL
}

public enum MaintenanceStatus
{
    REQUESTED,
    APPROVED,
    IN_PROGRESS,
    COMPLETED,
    CANCELLED
}
