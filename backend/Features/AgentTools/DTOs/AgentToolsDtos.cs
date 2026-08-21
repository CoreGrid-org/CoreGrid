using System;

namespace CoreGrid.Api.Features.AgentTools.DTOs;

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
    
    // Nullable/explicit indicator if replacement estimate is not available from market/catalog sources
    public decimal? ReplacementEstimate { get; set; }
    public string? ReplacementEstimateNote { get; set; }
}

public class DepartmentBudgetSummaryDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    
    // Explicitly markers for budget system gap
    public decimal? AllocatedMaintenanceBudget { get; set; }
    public decimal? CommittedAmount { get; set; }
    public decimal? SpentAmount { get; set; }
    public decimal? RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public class ComputeDepreciationRequest
{
    public decimal AcquisitionCost { get; set; }
    public DateOnly AcquisitionDate { get; set; }
    public int UsefulLifeYears { get; set; }
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
