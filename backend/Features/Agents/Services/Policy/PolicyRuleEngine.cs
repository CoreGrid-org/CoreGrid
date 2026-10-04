using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services.Policy;

public sealed class PolicyRuleEngine : IPolicyRuleEngine
{
    private const string NotTriggered = "recommendation does not trigger this rule";

    public PolicyValidation Evaluate(PolicyEvaluationFacts facts)
    {
        RuleCheck[] checks =
        [
            DisposalConditionJustified(facts),
            DisposalServiceLifeReached(facts),
            DisposalValuationCurrent(facts),
            ReplacementRatioClearsThreshold(facts),
            RepairWithinBudget(facts),
            AssetNotTerminal(facts),
            NoOpenRecords(facts),
            ConfidenceAboveFloor(facts),
            DisposalRequiresApproval(facts)
        ];

        var results = checks.Select(c => c.Result).ToList();
        return new PolicyValidation
        {
            Verdict = results.Any(r => r.Outcome == PolicyVerdicts.Fail) ? PolicyVerdicts.Fail
                : results.Any(r => r.Outcome == PolicyVerdicts.NeedsRevision) ? PolicyVerdicts.NeedsRevision
                : PolicyVerdicts.Pass,
            RuleResults = results,
            BlockingReasons = checks.Select(c => c.BlockingReason).OfType<string>().ToList(),
            IsHighImpact = IsDisposal(facts) || IsBelowConfidenceFloor(facts)
        };
    }

    private sealed record RuleCheck(PolicyRuleResult Result, string? BlockingReason = null);

    private static RuleCheck DisposalConditionJustified(PolicyEvaluationFacts f) => IsDisposal(f)
        ? Check("PR-01", "Condition is Poor or Unserviceable", f.AssetCondition,
            f.AssetCondition is AssetConditions.Poor or AssetConditions.Unserviceable,
            PolicyVerdicts.Fail, "PR-01: asset condition does not justify disposal.")
        : NotApplicable("PR-01", "Condition is Poor or Unserviceable (DISPOSE only)");

    private static RuleCheck DisposalServiceLifeReached(PolicyEvaluationFacts f) => IsDisposal(f)
        ? Check("PR-02", $"Elapsed service life >= {f.MinimumServiceLifeYears} years", $"{f.ElapsedServiceLifeYears} years",
            f.ElapsedServiceLifeYears >= f.MinimumServiceLifeYears,
            PolicyVerdicts.Fail, "PR-02: elapsed service life is below the configured minimum.")
        : NotApplicable("PR-02", "Elapsed service life >= configured minimum (DISPOSE only)");

    private static RuleCheck DisposalValuationCurrent(PolicyEvaluationFacts f)
    {
        if (!IsDisposal(f)) return NotApplicable("PR-03", "Valuation within configured validity window (DISPOSE only)");

        var valuationDate = f.HasValuation ? f.ValuationDate : null;
        var isCurrent = valuationDate.HasValue
            && f.EvaluatedAsOf.DayNumber - valuationDate.Value.DayNumber <= f.ValuationValidityWindowDays;

        return Check("PR-03", $"Valuation recorded within {f.ValuationValidityWindowDays} days",
            valuationDate?.ToString("yyyy-MM-dd") ?? "none recorded",
            isCurrent, PolicyVerdicts.NeedsRevision, "PR-03: a current valuation is required before disposal.");
    }

    private static RuleCheck ReplacementRatioClearsThreshold(PolicyEvaluationFacts f) => f.ProposedRecommendation == LifecycleActions.Replace
        ? Check("PR-04", $"Repair-to-replace ratio >= {f.RepairToReplaceCostThreshold}",
            f.RepairToReplaceRatio?.ToString("0.00") ?? "not provided",
            f.RepairToReplaceRatio >= f.RepairToReplaceCostThreshold,
            PolicyVerdicts.NeedsRevision, "PR-04: repair-to-replace ratio does not clear the organisation threshold.")
        : NotApplicable("PR-04", "Repair-to-replace ratio >= organisation threshold (REPLACE only)");

    private static RuleCheck RepairWithinBudget(PolicyEvaluationFacts f)
    {
        const string expected = "Projected repair cost <= available budget headroom";
        if (f.ProposedRecommendation != LifecycleActions.Repair) return NotApplicable("PR-05", "Projected repair cost <= budget headroom (REPAIR only)");

        var cost = f.ProjectedRepairCost?.ToString("0.00") ?? "n/a";
        if (!f.BudgetHeadroom.HasValue)
        {
            return new RuleCheck(Rule("PR-05", expected, $"cost {cost}; department budget not tracked", PolicyVerdicts.NotApplicable));
        }

        return Check("PR-05", expected, $"cost {cost} vs. headroom {f.BudgetHeadroom.Value:0.00}",
            f.ProjectedRepairCost <= f.BudgetHeadroom,
            PolicyVerdicts.NeedsRevision, "PR-05: projected repair cost exceeds available departmental budget.");
    }

    private static RuleCheck AssetNotTerminal(PolicyEvaluationFacts f) =>
        Check("PR-06", "Asset is not in a terminal state", f.AssetStatus,
            f.AssetStatus != AssetStatuses.Disposed,
            PolicyVerdicts.Fail, "PR-06: asset is already disposed — no further recommendation is valid.");

    private static RuleCheck NoOpenRecords(PolicyEvaluationFacts f) =>
        Check("PR-07", "No open maintenance or transfer record",
            $"{f.OpenMaintenanceCount} open maintenance, {f.OpenTransferCount} open transfer",
            f.OpenMaintenanceCount == 0 && f.OpenTransferCount == 0,
            PolicyVerdicts.NeedsRevision, "PR-07: an open maintenance or transfer record must be resolved first.");

    private static RuleCheck ConfidenceAboveFloor(PolicyEvaluationFacts f) =>
        Check("PR-08", $"Confidence >= {f.ConfidenceFloor}", f.Confidence?.ToString("0.00") ?? "not provided",
            !IsBelowConfidenceFloor(f), PolicyVerdicts.NeedsRevision, blockingReason: null);

    private static RuleCheck DisposalRequiresApproval(PolicyEvaluationFacts f) => IsDisposal(f)
        ? new RuleCheck(Rule("PR-09", "DISPOSE requires approval", LifecycleActions.Dispose, PolicyVerdicts.Pass))
        : NotApplicable("PR-09", "DISPOSE always requires approval (DISPOSE only)");

    private static bool IsDisposal(PolicyEvaluationFacts f) => f.ProposedRecommendation == LifecycleActions.Dispose;

    private static bool IsBelowConfidenceFloor(PolicyEvaluationFacts f) => f.Confidence < f.ConfidenceFloor;

    private static RuleCheck Check(string id, string expected, string actual, bool passed, string failedOutcome, string? blockingReason) =>
        new(Rule(id, expected, actual, passed ? PolicyVerdicts.Pass : failedOutcome), passed ? null : blockingReason);

    private static RuleCheck NotApplicable(string id, string expected) =>
        new(Rule(id, expected, NotTriggered, PolicyVerdicts.NotApplicable));

    private static PolicyRuleResult Rule(string id, string expected, string actual, string outcome) =>
        new() { RuleId = id, Expected = expected, Actual = actual, Outcome = outcome };
}
