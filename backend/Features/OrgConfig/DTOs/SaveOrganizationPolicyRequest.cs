using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.OrgConfig.DTOs;

// Defines the request for creating or updating an organization policy.
public class SaveOrganizationPolicyRequest
{
    public Guid? AssetTypeId { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal? RepairToReplaceCostThreshold { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal? MinimumServiceLifeYears { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal? MaxAcceptableFailureFrequency { get; set; }

    [Required]
    [Range(0, 3650)]
    public int? ValuationValidityWindowDays { get; set; }

    [Required]
    [Range(0, 1)]
    public decimal? ConfidenceFloor { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal? CostVarianceTolerancePercent { get; set; }

    [Required]
    [Range(0, 3650)]
    public int? OutstandingTransferDays { get; set; }

    [Required]
    [Range(0, 8760)]
    public int? ApprovalOverduePeriodHours { get; set; }
}
