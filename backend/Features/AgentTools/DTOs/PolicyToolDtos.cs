namespace CoreGrid.Api.Features.AgentTools.DTOs;

public class OrganizationPolicyFactsDto
{
    public Guid? AssetTypeId { get; set; }
    public decimal RepairToReplaceCostThreshold { get; set; }
    public decimal MinimumServiceLifeYears { get; set; }
    public decimal MaxAcceptableFailureFrequency { get; set; }
    public int ValuationValidityWindowDays { get; set; }
    public decimal ConfidenceFloor { get; set; }
}

public class AssetComplianceStateDto
{
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public string CurrentCondition { get; set; } = string.Empty;
    public bool IsCondemned { get; set; }
    public bool HasValuation { get; set; }
    public DateOnly? ValuationDate { get; set; }
    public int OpenMaintenanceCount { get; set; }
    public int OpenTransferCount { get; set; }
    public decimal ElapsedServiceLifeYears { get; set; }
}
