using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.AgentTools.DTOs;

public class AssetFinancialsDto
{
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public decimal AcquisitionCost { get; set; }
    public DateOnly AcquisitionDate { get; set; }
    public int UsefulLifeYears { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal ResidualBookValue { get; set; }
    public decimal CumulativeMaintenanceCost { get; set; }
    public decimal? ReplacementEstimate { get; set; }
    public string? ReplacementEstimateNote { get; set; }
}

public class DepartmentBudgetSummaryDto
{
    public const string NotConfigured = "NOT_CONFIGURED";

    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public decimal? AllocatedMaintenanceBudget { get; set; }
    public decimal? CommittedAmount { get; set; }
    public decimal? SpentAmount { get; set; }
    public decimal? RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
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
