namespace CoreGrid.Api.Features.OrgConfig.DTOs;

public class OrganizationPolicyDto
{
    public Guid Id { get; set; }

    public Guid? AssetTypeId { get; set; }

    public string? AssetTypeName { get; set; }

    public decimal RepairToReplaceCostThreshold { get; set; }
    public decimal MinimumServiceLifeYears { get; set; }
    public decimal MaxAcceptableFailureFrequency { get; set; }
    public int ValuationValidityWindowDays { get; set; }
    public decimal ConfidenceFloor { get; set; }
    public decimal CostVarianceTolerancePercent { get; set; }
    public int OutstandingTransferDays { get; set; }
    public int ApprovalOverduePeriodHours { get; set; }
}
