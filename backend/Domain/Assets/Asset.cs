using System;
using System.Collections.Generic;

namespace CoreGrid.Api.Domain;

public class Asset
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid AssetTypeId { get; set; }
    public AssetType? AssetType { get; set; }

    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid LocationId { get; set; }
    public Location? Location { get; set; }

    public required string AssetCode { get; set; }
    public required string Name { get; set; }
    public required string Status { get; set; } // ACTIVE | UNDER_MAINTENANCE | ...
    public required string Condition { get; set; } // NEW | GOOD | ...
    public DateOnly AcquisitionDate { get; set; }
    public decimal AcquisitionCost { get; set; }
    public decimal ResidualValue { get; set; }
    public decimal CumulativeMaintenanceCost { get; set; } = 0;
    public int RepairCount { get; set; } = 0;
    public DateOnly? LastRepairDate { get; set; }
    public required string QrPayload { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<AssetAttributeValue> AssetAttributeValues { get; set; } = new List<AssetAttributeValue>();
    public ICollection<AssetHistory> AssetHistoryEntries { get; set; } = new List<AssetHistory>();
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
}

