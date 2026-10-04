using System.Text.Json.Serialization;

namespace CoreGrid.Api.Features.Agents.DTOs;

public static class AssessmentSources
{
    public const string Model = "MODEL";
    public const string Deterministic = "DETERMINISTIC";
}

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

    [JsonPropertyName("asset_count")]
    public int AssetCount { get; set; } = 1;

    [JsonPropertyName("projected_repair_cost")]
    public decimal ProjectedRepairCost { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = AssessmentSources.Deterministic;

    [JsonPropertyName("assets")]
    public List<AssetFinancialTriageDto>? Assets { get; set; }
}

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

public class RankedOptionDto
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public decimal Score { get; set; }

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = string.Empty;
}
