using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Agents.Services;

namespace backend.Tests.Features.Agents;

// SRS §7.6, PR-01 to PR-09. Pure unit tests — no database, no HTTP — proving
// exactly what §7.3 promises: the same inputs always produce the same
// verdict, deterministically.
public class PolicyRuleEngineTests
{
    private readonly PolicyRuleEngine _engine = new();

    private static PolicyEvaluationFacts BaseFacts(string recommendation) => new()
    {
        ProposedRecommendation = recommendation,
        AssetCondition = "GOOD",
        AssetStatus = "ACTIVE",
        ElapsedServiceLifeYears = 3,
        HasValuation = false,
        ValuationDate = null,
        OpenMaintenanceCount = 0,
        OpenTransferCount = 0,
        MinimumServiceLifeYears = 5,
        ValuationValidityWindowDays = 90,
        RepairToReplaceCostThreshold = 0.65m,
        ConfidenceFloor = 0.7m,
        EvaluatedAsOf = new DateOnly(2026, 8, 21)
    };

    [Fact]
    public void Dispose_WithGoodCondition_FailsPR01()
    {
        var facts = BaseFacts("DISPOSE");
        facts.AssetCondition = "GOOD";

        var result = _engine.Evaluate(facts);

        Assert.Equal("FAIL", result.Verdict);
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-01" && r.Outcome == "FAIL");
    }

    [Fact]
    public void Dispose_WithUnserviceableConditionAndSufficientLife_PassesPR01AndPR02()
    {
        var facts = BaseFacts("DISPOSE");
        facts.AssetCondition = "UNSERVICEABLE";
        facts.ElapsedServiceLifeYears = 6;
        facts.HasValuation = true;
        facts.ValuationDate = new DateOnly(2026, 8, 1);

        var result = _engine.Evaluate(facts);

        Assert.Equal("PASS", result.Verdict);
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-01" && r.Outcome == "PASS");
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-02" && r.Outcome == "PASS");
    }

    [Fact]
    public void Dispose_WithInsufficientServiceLife_FailsPR02()
    {
        var facts = BaseFacts("DISPOSE");
        facts.AssetCondition = "POOR";
        facts.ElapsedServiceLifeYears = 2;

        var result = _engine.Evaluate(facts);

        Assert.Equal("FAIL", result.Verdict);
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-02" && r.Outcome == "FAIL");
    }

    [Fact]
    public void Dispose_WithoutValuation_NeedsRevisionOnPR03()
    {
        var facts = BaseFacts("DISPOSE");
        facts.AssetCondition = "POOR";
        facts.ElapsedServiceLifeYears = 6;
        facts.HasValuation = false;

        var result = _engine.Evaluate(facts);

        Assert.Equal("NEEDS_REVISION", result.Verdict);
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-03" && r.Outcome == "NEEDS_REVISION");
    }

    [Fact]
    public void Dispose_WithStaleValuation_NeedsRevisionOnPR03()
    {
        var facts = BaseFacts("DISPOSE");
        facts.AssetCondition = "POOR";
        facts.ElapsedServiceLifeYears = 6;
        facts.HasValuation = true;
        facts.ValuationDate = new DateOnly(2026, 1, 1); // > 90 days before EvaluatedAsOf
        facts.ValuationValidityWindowDays = 90;

        var result = _engine.Evaluate(facts);

        Assert.Equal("NEEDS_REVISION", result.Verdict);
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-03" && r.Outcome == "NEEDS_REVISION");
    }

    [Fact]
    public void Dispose_AlwaysSetsHighImpact()
    {
        var facts = BaseFacts("DISPOSE");
        facts.AssetCondition = "POOR";
        facts.ElapsedServiceLifeYears = 6;
        facts.HasValuation = true;
        facts.ValuationDate = facts.EvaluatedAsOf;

        var result = _engine.Evaluate(facts);

        Assert.True(result.IsHighImpact);
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-09" && r.Outcome == "PASS");
    }

    [Fact]
    public void Replace_WithRatioBelowThreshold_NeedsRevisionOnPR04()
    {
        var facts = BaseFacts("REPLACE");
        facts.RepairToReplaceRatio = 0.4m;
        facts.RepairToReplaceCostThreshold = 0.65m;

        var result = _engine.Evaluate(facts);

        Assert.Equal("NEEDS_REVISION", result.Verdict);
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-04" && r.Outcome == "NEEDS_REVISION");
        Assert.False(result.IsHighImpact); // REPLACE alone doesn't trigger PR-09
    }

    [Fact]
    public void Replace_WithRatioAtOrAboveThreshold_PassesPR04()
    {
        var facts = BaseFacts("REPLACE");
        facts.RepairToReplaceRatio = 0.7m;
        facts.RepairToReplaceCostThreshold = 0.65m;

        var result = _engine.Evaluate(facts);

        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-04" && r.Outcome == "PASS");
    }

    [Fact]
    public void Repair_WithCostOverBudget_NeedsRevisionOnPR05()
    {
        var facts = BaseFacts("REPAIR");
        facts.ProjectedRepairCost = 50000m;
        facts.BudgetHeadroom = 10000m;

        var result = _engine.Evaluate(facts);

        Assert.Equal("NEEDS_REVISION", result.Verdict);
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-05" && r.Outcome == "NEEDS_REVISION");
    }

    [Fact]
    public void Repair_WithCostWithinBudget_PassesPR05()
    {
        var facts = BaseFacts("REPAIR");
        facts.ProjectedRepairCost = 5000m;
        facts.BudgetHeadroom = 10000m;

        var result = _engine.Evaluate(facts);

        Assert.Equal("PASS", result.Verdict);
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-05" && r.Outcome == "PASS");
    }

    [Fact]
    public void TerminalAsset_FailsPR06RegardlessOfRecommendation()
    {
        var facts = BaseFacts("RETAIN");
        facts.AssetStatus = "DISPOSED";

        var result = _engine.Evaluate(facts);

        Assert.Equal("FAIL", result.Verdict);
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-06" && r.Outcome == "FAIL");
    }

    [Fact]
    public void OpenMaintenanceRecord_NeedsRevisionOnPR07()
    {
        var facts = BaseFacts("RETAIN");
        facts.OpenMaintenanceCount = 1;

        var result = _engine.Evaluate(facts);

        Assert.Equal("NEEDS_REVISION", result.Verdict);
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-07" && r.Outcome == "NEEDS_REVISION");
    }

    [Fact]
    public void OpenTransferRecord_NeedsRevisionOnPR07()
    {
        var facts = BaseFacts("RETAIN");
        facts.OpenTransferCount = 2;

        var result = _engine.Evaluate(facts);

        Assert.Equal("NEEDS_REVISION", result.Verdict);
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-07" && r.Outcome == "NEEDS_REVISION");
    }

    [Fact]
    public void LowConfidence_ForcesHighImpactEvenForRetain()
    {
        var facts = BaseFacts("RETAIN");
        facts.Confidence = 0.3m;
        facts.ConfidenceFloor = 0.7m;

        var result = _engine.Evaluate(facts);

        Assert.True(result.IsHighImpact);
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-08" && r.Outcome == "NEEDS_REVISION");
    }

    [Fact]
    public void SufficientConfidence_DoesNotForceHighImpact()
    {
        var facts = BaseFacts("RETAIN");
        facts.Confidence = 0.9m;
        facts.ConfidenceFloor = 0.7m;

        var result = _engine.Evaluate(facts);

        Assert.False(result.IsHighImpact);
        Assert.Equal("PASS", result.Verdict);
    }

    [Fact]
    public void RulesThatDoNotApplyToTheRecommendation_AreMarkedNotApplicable()
    {
        var facts = BaseFacts("RETAIN");

        var result = _engine.Evaluate(facts);

        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-01" && r.Outcome == "N/A");
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-04" && r.Outcome == "N/A");
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-05" && r.Outcome == "N/A");
        Assert.Contains(result.RuleResults, r => r.RuleId == "PR-09" && r.Outcome == "N/A");
    }

    [Fact]
    public void SameInputs_AlwaysProduceTheSameVerdict()
    {
        var facts = BaseFacts("DISPOSE");
        facts.AssetCondition = "POOR";
        facts.ElapsedServiceLifeYears = 6;

        var first = _engine.Evaluate(facts);
        var second = _engine.Evaluate(facts);

        Assert.Equal(first.Verdict, second.Verdict);
        Assert.Equal(first.IsHighImpact, second.IsHighImpact);
        Assert.Equal(first.RuleResults.Count, second.RuleResults.Count);
    }
}
