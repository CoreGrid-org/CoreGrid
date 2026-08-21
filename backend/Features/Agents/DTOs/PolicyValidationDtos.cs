namespace CoreGrid.Api.Features.Agents.DTOs;

// SRS §7.3: the Policy Compliance Agent's contract. FinancialAssessment
// normally arrives from the Budget Analysis Agent (node 3) — until that
// agent exists, a caller supplies these facts directly; the shape and the
// rule evaluation behind it are unchanged either way.
public class PolicyValidationRequest
{
    public Guid AssetId { get; set; }
    public required string ProposedRecommendation { get; set; } // REPAIR | REPLACE | TRANSFER | DISPOSE | RETAIN
    public FinancialAssessmentFacts? FinancialAssessment { get; set; }
}

public class FinancialAssessmentFacts
{
    public decimal? RepairToReplaceRatio { get; set; }
    public decimal? ProjectedRepairCost { get; set; }
    public decimal? BudgetHeadroom { get; set; }
    public decimal? Confidence { get; set; } // 0-1
}

// SRS §7.3 output contract.
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

// The facts the rule engine actually evaluates — assembled from
// get_asset_compliance_state + get_organization_policies (§7.4) plus
// whatever FinancialAssessmentFacts the caller supplied.
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
