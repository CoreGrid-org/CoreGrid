using System.Text.Json.Serialization;
using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.Agents.DTOs;

/// <summary>
/// Output contract for Budget Analysis Agent evaluation.
/// Corresponds to Python contracts.py FinancialAssessment model.
/// </summary>
public class FinancialAssessmentResultDto
{
    [JsonPropertyName("residual_value")]
    public decimal ResidualValue { get; set; }

    [JsonPropertyName("replacement_estimate")]
    public decimal? ReplacementEstimate { get; set; }

    [JsonPropertyName("repair_to_replace_ratio")]
    public decimal? RepairToReplaceRatio { get; set; }

    [JsonPropertyName("budget_headroom")]
    public decimal? BudgetHeadroom { get; set; }

    [JsonPropertyName("ranked_options")]
    public List<RankedOptionDto> RankedOptions { get; set; } = [];

    [JsonPropertyName("proposed_recommendation")]
    public string ProposedRecommendation { get; set; } = string.Empty;

    // Fleet scope: totals across AssetCount assets (single asset: 1).
    [JsonPropertyName("asset_count")]
    public int AssetCount { get; set; } = 1;

    [JsonPropertyName("projected_repair_cost")]
    public decimal ProjectedRepairCost { get; set; }

    // MODEL when a model ranked the options; DETERMINISTIC otherwise.
    [JsonPropertyName("source")]
    public string Source { get; set; } = "DETERMINISTIC";

    // Per-asset financial triage, persisted for the Policy Compliance node;
    // stripped from API responses (the fleet summary carries the outcome).
    [JsonPropertyName("assets")]
    public List<AssetFinancialTriageDto>? Assets { get; set; }
}

// One asset's deterministic financial triage within an evaluation.
public class AssetFinancialTriageDto
{
    [JsonPropertyName("asset_id")]
    public Guid AssetId { get; set; }

    [JsonPropertyName("asset_code")]
    public string AssetCode { get; set; } = string.Empty;

    [JsonPropertyName("residual_value")]
    public decimal ResidualValue { get; set; }

    [JsonPropertyName("projected_cost")]
    public decimal ProjectedCost { get; set; }

    [JsonPropertyName("ratio")]
    public decimal Ratio { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;
}

/// <summary>
/// Single candidate lifecycle action evaluated and ranked by the Budget Analysis Agent.
/// </summary>
public class RankedOptionDto
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty; // REPAIR | REPLACE | TRANSFER | DISPOSE | RETAIN

    [JsonPropertyName("score")]
    public decimal Score { get; set; } // 0.0 to 1.0

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = string.Empty;
}

/// <summary>
/// Input request for Budget Analysis Agent evaluation.
/// </summary>
public class FinancialAssessmentRequestDto
{
    public Guid OrganizationId { get; set; }
    public Guid AssetId { get; set; }
    public Guid? DepartmentId { get; set; }
    public int? FiscalYear { get; set; }
    public FailureStatisticsDto MaintenanceAnalysis { get; set; } = null!;
}
