namespace CoreGrid.Api.Features.OrgConfig.DTOs;

// Used for both create and update — an organisation policy is a full
// replace of its parameter set, never a partial patch (SRS FR-015).
public class SaveOrganizationPolicyRequest
{
    public Guid? AssetTypeId { get; set; }

    public decimal RepairToReplaceCostThreshold { get; set; }
    public decimal MinimumServiceLifeYears { get; set; }
    public decimal MaxAcceptableFailureFrequency { get; set; }
    public int ValuationValidityWindowDays { get; set; }
    public decimal ConfidenceFloor { get; set; }
    public decimal CostVarianceTolerancePercent { get; set; }
    public int OutstandingTransferDays { get; set; }
    public int ApprovalOverduePeriodHours { get; set; }
}
