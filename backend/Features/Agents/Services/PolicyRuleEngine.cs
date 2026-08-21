using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

// SRS §7.6, rules PR-01 to PR-09. Deliberately a pure function over
// PolicyEvaluationFacts — no DB access, no model call — so that "the same
// inputs always produce the same verdict" (§7.3's answer to the viva
// question about trusting an LLM with a compliance decision) is something
// this class can actually prove, and so it's unit-testable without any of
// the surrounding infrastructure.
public class PolicyRuleEngine : IPolicyRuleEngine
{
    private const string Dispose = "DISPOSE";
    private const string Replace = "REPLACE";
    private const string Repair = "REPAIR";
    private const string TerminalAssetStatus = "DISPOSED";

    public PolicyValidation Evaluate(PolicyEvaluationFacts facts)
    {
        var results = new List<PolicyRuleResult>();
        var blockingReasons = new List<string>();
        var isHighImpact = false;

        // PR-01: DISPOSE requires condition Poor or Unserviceable.
        if (facts.ProposedRecommendation == Dispose)
        {
            var conditionOk = facts.AssetCondition is "POOR" or "UNSERVICEABLE";
            results.Add(Rule("PR-01", "Condition is Poor or Unserviceable", facts.AssetCondition, conditionOk ? "PASS" : "FAIL"));
            if (!conditionOk) blockingReasons.Add("PR-01: asset condition does not justify disposal.");
        }
        else
        {
            results.Add(NotApplicable("PR-01", "Condition is Poor or Unserviceable (DISPOSE only)"));
        }

        // PR-02: DISPOSE requires elapsed service life >= configured minimum.
        if (facts.ProposedRecommendation == Dispose)
        {
            var lifeOk = facts.ElapsedServiceLifeYears >= facts.MinimumServiceLifeYears;
            results.Add(Rule(
                "PR-02",
                $"Elapsed service life >= {facts.MinimumServiceLifeYears} years",
                $"{facts.ElapsedServiceLifeYears} years",
                lifeOk ? "PASS" : "FAIL"));
            if (!lifeOk) blockingReasons.Add("PR-02: elapsed service life is below the configured minimum.");
        }
        else
        {
            results.Add(NotApplicable("PR-02", "Elapsed service life >= configured minimum (DISPOSE only)"));
        }

        // PR-03: DISPOSE requires a valuation within the configured validity window.
        if (facts.ProposedRecommendation == Dispose)
        {
            var valuationOk = facts.HasValuation
                && facts.ValuationDate.HasValue
                && facts.EvaluatedAsOf.DayNumber - facts.ValuationDate.Value.DayNumber <= facts.ValuationValidityWindowDays;
            results.Add(Rule(
                "PR-03",
                $"Valuation recorded within {facts.ValuationValidityWindowDays} days",
                facts.HasValuation && facts.ValuationDate.HasValue ? facts.ValuationDate.Value.ToString("yyyy-MM-dd") : "none recorded",
                valuationOk ? "PASS" : "NEEDS_REVISION"));
            if (!valuationOk) blockingReasons.Add("PR-03: a current valuation is required before disposal.");
        }
        else
        {
            results.Add(NotApplicable("PR-03", "Valuation within configured validity window (DISPOSE only)"));
        }

        // PR-04: REPLACE requires repair-to-replace ratio >= org threshold.
        if (facts.ProposedRecommendation == Replace)
        {
            var ratioOk = facts.RepairToReplaceRatio.HasValue && facts.RepairToReplaceRatio.Value >= facts.RepairToReplaceCostThreshold;
            results.Add(Rule(
                "PR-04",
                $"Repair-to-replace ratio >= {facts.RepairToReplaceCostThreshold}",
                facts.RepairToReplaceRatio?.ToString("0.00") ?? "not provided",
                ratioOk ? "PASS" : "NEEDS_REVISION"));
            if (!ratioOk) blockingReasons.Add("PR-04: repair-to-replace ratio does not clear the organisation threshold.");
        }
        else
        {
            results.Add(NotApplicable("PR-04", "Repair-to-replace ratio >= organisation threshold (REPLACE only)"));
        }

        // PR-05: REPAIR requires projected repair cost <= budget headroom.
        if (facts.ProposedRecommendation == Repair)
        {
            var costOk = facts.ProjectedRepairCost.HasValue && facts.BudgetHeadroom.HasValue
                && facts.ProjectedRepairCost.Value <= facts.BudgetHeadroom.Value;
            results.Add(Rule(
                "PR-05",
                "Projected repair cost <= available budget headroom",
                $"cost {facts.ProjectedRepairCost?.ToString("0.00") ?? "n/a"} vs. headroom {facts.BudgetHeadroom?.ToString("0.00") ?? "n/a"}",
                costOk ? "PASS" : "NEEDS_REVISION"));
            if (!costOk) blockingReasons.Add("PR-05: projected repair cost exceeds available departmental budget.");
        }
        else
        {
            results.Add(NotApplicable("PR-05", "Projected repair cost <= budget headroom (REPAIR only)"));
        }

        // PR-06: no recommendation for an asset in a terminal state — fatal.
        var isTerminal = facts.AssetStatus == TerminalAssetStatus;
        results.Add(Rule("PR-06", "Asset is not in a terminal state", facts.AssetStatus, isTerminal ? "FAIL" : "PASS"));
        if (isTerminal) blockingReasons.Add("PR-06: asset is already disposed — no further recommendation is valid.");

        // PR-07: no recommendation where an open maintenance or transfer record exists.
        var hasOpenRecord = facts.OpenMaintenanceCount > 0 || facts.OpenTransferCount > 0;
        results.Add(Rule(
            "PR-07",
            "No open maintenance or transfer record",
            $"{facts.OpenMaintenanceCount} open maintenance, {facts.OpenTransferCount} open transfer",
            hasOpenRecord ? "NEEDS_REVISION" : "PASS"));
        if (hasOpenRecord) blockingReasons.Add("PR-07: an open maintenance or transfer record must be resolved first.");

        // PR-08: confidence below the floor forces human review regardless of action.
        var belowConfidenceFloor = facts.Confidence.HasValue && facts.Confidence.Value < facts.ConfidenceFloor;
        results.Add(Rule(
            "PR-08",
            $"Confidence >= {facts.ConfidenceFloor}",
            facts.Confidence?.ToString("0.00") ?? "not provided",
            belowConfidenceFloor ? "NEEDS_REVISION" : "PASS"));
        if (belowConfidenceFloor) isHighImpact = true;

        // PR-09: DISPOSE is always high-impact and always requires approval.
        if (facts.ProposedRecommendation == Dispose)
        {
            results.Add(Rule("PR-09", "DISPOSE requires approval", "DISPOSE", "PASS"));
            isHighImpact = true;
        }
        else
        {
            results.Add(NotApplicable("PR-09", "DISPOSE always requires approval (DISPOSE only)"));
        }

        var hasFatalFail = results.Any(r => r.RuleId is "PR-06" && r.Outcome == "FAIL")
            || results.Any(r => r.RuleId is "PR-01" or "PR-02" && r.Outcome == "FAIL");
        var hasNeedsRevision = results.Any(r => r.Outcome == "NEEDS_REVISION");

        var verdict = hasFatalFail ? "FAIL" : hasNeedsRevision ? "NEEDS_REVISION" : "PASS";

        return new PolicyValidation
        {
            Verdict = verdict,
            RuleResults = results,
            BlockingReasons = blockingReasons,
            IsHighImpact = isHighImpact
        };
    }

    private static PolicyRuleResult Rule(string id, string expected, string actual, string outcome) =>
        new() { RuleId = id, Expected = expected, Actual = actual, Outcome = outcome };

    private static PolicyRuleResult NotApplicable(string id, string expected) =>
        new() { RuleId = id, Expected = expected, Actual = "recommendation does not trigger this rule", Outcome = "N/A" };
}
