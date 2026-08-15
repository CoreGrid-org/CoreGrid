using System;

namespace CoreGrid.Api.Domain;

public class OrganizationPolicy
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid? AssetTypeId { get; set; }
    public AssetType? AssetType { get; set; }

    public decimal RepairToReplaceCostThreshold { get; set; }
    public decimal MinimumServiceLifeYears { get; set; }
    public decimal MaxAcceptableFailureFrequency { get; set; }
    public int ValuationValidityWindowDays { get; set; }
    public decimal ConfidenceFloor { get; set; }
    public decimal CostVarianceTolerancePercent { get; set; }
    public int OutstandingTransferDays { get; set; }
    public int ApprovalOverduePeriodHours { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
