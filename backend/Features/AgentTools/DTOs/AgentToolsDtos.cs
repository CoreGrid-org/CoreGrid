using System;
using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.AgentTools.DTOs;

public class AssetSummaryDto
{
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateOnly AcquisitionDate { get; set; }
    public decimal AcquisitionCost { get; set; }
}

public class AssetFinancialsDto
{
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public decimal AcquisitionCost { get; set; }
    public DateOnly AcquisitionDate { get; set; }
    public int UsefulLifeYears { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal ResidualBookValue { get; set; }
    public decimal CumulativeMaintenanceCost { get; set; }
    
   // Stores the estimated replacement cost when available.
    public decimal? ReplacementEstimate { get; set; }
    public string? ReplacementEstimateNote { get; set; }
}

public class DepartmentBudgetSummaryDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    
    // Stores the available budget information.
    public decimal? AllocatedMaintenanceBudget { get; set; }
    public decimal? CommittedAmount { get; set; }
    public decimal? SpentAmount { get; set; }
    public decimal? RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

// Represents the organization policies used for evaluation.
public class OrganizationPolicyFactsDto
{
    public Guid? AssetTypeId { get; set; }
    public decimal RepairToReplaceCostThreshold { get; set; }
    public decimal MinimumServiceLifeYears { get; set; }
    public decimal MaxAcceptableFailureFrequency { get; set; }
    public int ValuationValidityWindowDays { get; set; }
    public decimal ConfidenceFloor { get; set; }
}
// Represents the compliance state of an asset.
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

public class ComputeDepreciationRequest
{
    [Required]
    [Range(0, 1_000_000_000_000)]
    public decimal? AcquisitionCost { get; set; }

    [Required]
    public DateOnly? AcquisitionDate { get; set; }

    [Required]
    [Range(1, 100)]
    public int? UsefulLifeYears { get; set; }

    public DateOnly? AsOfDate { get; set; }
}

public class ComputeDepreciationResponse
{
    public decimal AcquisitionCost { get; set; }
    public DateOnly AcquisitionDate { get; set; }
    public int UsefulLifeYears { get; set; }
    public DateOnly AsOfDate { get; set; }
    public decimal AnnualDepreciation { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal CurrentValue { get; set; }
    public string DepreciationMethod { get; set; } = "straight-line";
}
