namespace CoreGrid.Api.Features.Agents.DTOs;

public class FinancialAssessmentFacts
{
    public decimal? RepairToReplaceRatio { get; set; }
    public decimal? ProjectedRepairCost { get; set; }
    public decimal? BudgetHeadroom { get; set; }
    public decimal? Confidence { get; set; } // 0-1
}

// Represents the result of policy validation.
public class PolicyValidation
{
    public required string Verdict { get; set; } // PASS | FAIL | NEEDS_REVISION
    public List<PolicyRuleResult> RuleResults { get; set; } = [];
    public List<string> BlockingReasons { get; set; } = [];
    public bool IsHighImpact { get; set; }
}

public class PolicyRuleResult
{
    public required string RuleId { get; set; }
    public required string Expected { get; set; }
    public required string Actual { get; set; }
    public required string Outcome { get; set; } // PASS | FAIL | NEEDS_REVISION | N/A
}

// Represents the facts used for policy evaluation.
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

    // Organisation policy thresholds (get_organization_policies).
    public decimal MinimumServiceLifeYears { get; set; }
    public int ValuationValidityWindowDays { get; set; }
    public decimal RepairToReplaceCostThreshold { get; set; }
    public decimal ConfidenceFloor { get; set; }

    public DateOnly EvaluatedAsOf { get; set; }
}
