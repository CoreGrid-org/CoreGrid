using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.OrgConfig.DTOs;

// Used for both create and update — an organisation policy is a full
// replace of its parameter set, never a partial patch (SRS FR-015). Every
// numeric field is [Required] for exactly that reason: a field the client
// omits must fail validation, not silently zero out a threshold that then
// passes every policy check against it.
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
