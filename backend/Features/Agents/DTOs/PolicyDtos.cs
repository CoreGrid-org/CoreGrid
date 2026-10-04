namespace CoreGrid.Api.Features.Agents.DTOs;

public class FinancialAssessmentFacts
{
    public decimal? RepairToReplaceRatio { get; set; }
    public decimal? ProjectedRepairCost { get; set; }
    public decimal? BudgetHeadroom { get; set; }
    public decimal? Confidence { get; set; }
}

public class PolicyValidation
{
    public required string Verdict { get; set; }
    public List<PolicyRuleResult> RuleResults { get; set; } = [];
    public List<string> BlockingReasons { get; set; } = [];
    public bool IsHighImpact { get; set; }
}

public class PolicyRuleResult
{
    public required string RuleId { get; set; }
    public required string Expected { get; set; }
    public required string Actual { get; set; }
    public required string Outcome { get; set; }
}

public class PolicyEvaluationFacts
{
    public required string ProposedRecommendation { get; set; }
    public required string AssetCondition { get; set; }
    public required string AssetStatus { get; set; }
    public decimal ElapsedServiceLifeYears { get; set; }
    public bool HasValuation { get; set; }
    public DateOnly? ValuationDate { get; set; }
    public int OpenMaintenanceCount { get; set; }
    public int OpenTransferCount { get; set; }
    public decimal? RepairToReplaceRatio { get; set; }
    public decimal? ProjectedRepairCost { get; set; }
    public decimal? BudgetHeadroom { get; set; }
    public decimal? Confidence { get; set; }
    public decimal MinimumServiceLifeYears { get; set; }
    public int ValuationValidityWindowDays { get; set; }
    public decimal RepairToReplaceCostThreshold { get; set; }
    public decimal ConfidenceFloor { get; set; }
    public DateOnly EvaluatedAsOf { get; set; }
}

public sealed record PolicyEvaluationOptions(
    string? ForcedAction = null,
    string? ExcludedAction = null,
    FinancialAssessmentFacts? ManualFacts = null);

public sealed record PolicyOutcome(string Recommendation, PolicyValidation Validation, FleetEvaluationDto Fleet, string Summary);

public class FleetEvaluationDto
{
    public int AssetCount { get; set; }
    public Dictionary<string, int> ActionCounts { get; set; } = [];
    public int PassCount { get; set; }
    public int DeferredCount { get; set; }
    public int BlockedCount { get; set; }
    public List<FleetAssetResultDto> Assets { get; set; } = [];
}

public class FleetAssetResultDto
{
    public Guid AssetId { get; set; }
    public required string AssetCode { get; set; }
    public required string Condition { get; set; }
    public required string Action { get; set; }
    public required string Verdict { get; set; }
    public bool IsHighImpact { get; set; }
    public decimal? Ratio { get; set; }
    public decimal ProjectedCost { get; set; }
    public required string Reason { get; set; }
}
