using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.AgentTools.Services;
using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Shared.Exceptions;

namespace CoreGrid.Api.Features.Agents.Services;

public sealed record PolicyEvaluationOptions(
    string? ForcedAction = null,             // a human's manual proposal: evaluate only this
    string? ExcludedAction = null,           // rejected by a reviewer's REVISE: never re-propose it
    FinancialAssessmentFacts? ManualFacts = null);

public sealed record PolicyOutcome(string Recommendation, PolicyValidation Validation, FleetEvaluationDto Fleet, string Summary);

public interface IPolicyComplianceEvaluator
{
    Task<PolicyOutcome> EvaluateAsync(
        EvaluationScope scope,
        IReadOnlyList<FailureStatisticsDto>? maintenanceByAsset,
        FinancialAssessmentResultDto? budget,
        PolicyEvaluationOptions options,
        CancellationToken cancellationToken);
}

// Node 4 (Policy Compliance, SRS §7.3) — deterministic. For each asset in
// scope it builds an ordered list of candidate actions (budget triage,
// condition-based proposal, the scope's ranked options, RETAIN) and keeps the
// first one the rule engine PASSes, so a blocked first choice is revised in
// microseconds instead of a round trip. Scope-level verdict and rule results
// are rolled up from the per-asset outcomes. Policy and compliance state are
// read once for the whole fleet.
public sealed class PolicyComplianceEvaluator(
    IAgentToolsService agentTools,
    IAssetActionRecommendationEngine recommendationEngine,
    IPolicyRuleEngine ruleEngine) : IPolicyComplianceEvaluator
{
    private const int MaxStoredAssets = 200;
    private const string Retain = "RETAIN";

    // Display/headline priority — most consequential first.
    private static readonly string[] ImpactOrder = ["DISPOSE", "REPLACE", "REPAIR", "TRANSFER", Retain];
    private static readonly string[] OutcomeSeverity = ["FAIL", "NEEDS_REVISION", "PASS", "N/A"];

    public async Task<PolicyOutcome> EvaluateAsync(
        EvaluationScope scope,
        IReadOnlyList<FailureStatisticsDto>? maintenanceByAsset,
        FinancialAssessmentResultDto? budget,
        PolicyEvaluationOptions options,
        CancellationToken cancellationToken)
    {
        var states = await agentTools.GetFleetComplianceStateAsync(scope.OrganizationId, scope.AssetTypeId, scope.AssetId, cancellationToken);
        if (states.Count == 0)
        {
            throw new BusinessRuleException("There are no active assets in this evaluation's scope.", "empty_scope");
        }

        var policy = await agentTools.GetOrganizationPoliciesAsync(scope.OrganizationId, scope.AssetTypeId, cancellationToken)
            ?? throw new BusinessRuleException("No organisation policy is configured — cannot evaluate compliance.", "no_policy_configured");

        var triageById = budget?.Assets?.ToDictionary(t => t.AssetId) ?? [];
        var statsById = maintenanceByAsset?.ToDictionary(s => s.AssetId) ?? [];
        var rankedActions = budget?.RankedOptions.OrderByDescending(o => o.Score).Select(o => o.Action).ToList() ?? [];
        var modelPick = scope.IsSingleAsset && budget?.Source == "MODEL" ? budget.ProposedRecommendation : null;
        // Confidence is a model output; deterministic rankings don't claim one (PR-08 then passes as "not provided").
        var confidence = options.ManualFacts?.Confidence
            ?? (modelPick is not null ? budget!.RankedOptions.Max(o => o.Score) : null);
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var results = new List<(FleetAssetResultDto Row, PolicyValidation Validation)>(states.Count);
        foreach (var state in states)
        {
            var triage = triageById.GetValueOrDefault(state.AssetId);
            var conditionProposal = recommendationEngine.Propose(state, policy);
            var candidates = Candidates(options, state, triage, conditionProposal.Recommendation, modelPick, rankedActions);

            var evaluated = candidates
                .Select(action => (Action: action, Validation: ruleEngine.Evaluate(new PolicyEvaluationFacts
                {
                    ProposedRecommendation = action,
                    AssetCondition = state.CurrentCondition,
                    AssetStatus = state.CurrentStatus,
                    ElapsedServiceLifeYears = state.ElapsedServiceLifeYears,
                    HasValuation = state.HasValuation,
                    ValuationDate = state.ValuationDate,
                    OpenMaintenanceCount = state.OpenMaintenanceCount,
                    OpenTransferCount = state.OpenTransferCount,
                    RepairToReplaceRatio = triage?.Ratio ?? options.ManualFacts?.RepairToReplaceRatio,
                    ProjectedRepairCost = triage?.ProjectedCost ?? statsById.GetValueOrDefault(state.AssetId)?.ProjectedNextTwelveMonthsCost ?? options.ManualFacts?.ProjectedRepairCost,
                    BudgetHeadroom = budget?.BudgetHeadroom ?? options.ManualFacts?.BudgetHeadroom,
                    Confidence = confidence,
                    MinimumServiceLifeYears = policy.MinimumServiceLifeYears,
                    ValuationValidityWindowDays = policy.ValuationValidityWindowDays,
                    RepairToReplaceCostThreshold = policy.RepairToReplaceCostThreshold,
                    ConfidenceFloor = policy.ConfidenceFloor,
                    EvaluatedAsOf = asOf
                })))
                .ToList();

            var chosen = evaluated.FirstOrDefault(e => e.Validation.Verdict == "PASS");
            if (chosen.Action is null)
            {
                chosen = evaluated.FirstOrDefault(e => e.Validation.Verdict == "NEEDS_REVISION");
                if (chosen.Action is null) chosen = evaluated[0];
            }

            results.Add((new FleetAssetResultDto
            {
                AssetId = state.AssetId,
                AssetCode = state.AssetCode,
                Condition = state.CurrentCondition,
                Action = chosen.Action,
                Verdict = chosen.Validation.Verdict,
                IsHighImpact = chosen.Validation.IsHighImpact,
                Ratio = triage?.Ratio,
                ProjectedCost = triage?.ProjectedCost ?? 0m,
                Reason = Reason(evaluated, chosen, conditionProposal)
            }, chosen.Validation));
        }

        return scope.IsSingleAsset ? SingleOutcome(results[0]) : FleetOutcome(scope, results);
    }

    private static List<string> Candidates(
        PolicyEvaluationOptions options,
        AssetComplianceStateDto state,
        AssetFinancialTriageDto? triage,
        string conditionAction,
        string? modelPick,
        List<string> rankedActions)
    {
        if (options.ForcedAction is not null) return [options.ForcedAction];

        // Condition is the stronger signal when the asset is condemned or
        // unserviceable, or when finance alone sees nothing to do (RETAIN).
        var severe = state.IsCondemned || state.CurrentCondition == AssetConditions.Unserviceable;
        var conditionFirst = severe || triage is null || triage.Action == Retain;

        var ordered = new List<string?>();
        if (!severe) ordered.Add(modelPick);
        ordered.AddRange(conditionFirst ? new[] { conditionAction, triage?.Action } : new[] { triage!.Action, conditionAction });
        ordered.AddRange(rankedActions);
        ordered.Add(Retain);

        return ordered
            .OfType<string>()
            .Distinct()
            .Where(a => a != options.ExcludedAction)
            .ToList();
    }

    private static string Reason(
        List<(string Action, PolicyValidation Validation)> evaluated,
        (string Action, PolicyValidation Validation) chosen,
        AssetActionRecommendation conditionProposal)
    {
        if (chosen.Validation.Verdict != "PASS")
        {
            return string.Join(" ", chosen.Validation.BlockingReasons.DefaultIfEmpty("No candidate action is permitted by policy."));
        }

        var blocked = evaluated.TakeWhile(e => e.Action != chosen.Action).ToList();
        var basis = chosen.Action == conditionProposal.Recommendation
            ? conditionProposal.Rationale
            : $"Financial triage favours {chosen.Action}.";
        if (blocked.Count == 0) return basis;

        var blockedBy = blocked.Select(b => $"{b.Action} ({string.Join(", ", b.Validation.RuleResults.Where(r => r.Outcome is "FAIL" or "NEEDS_REVISION").Select(r => r.RuleId))})");
        return $"Revised from {string.Join(", ", blockedBy)} — {basis}";
    }

    private static PolicyOutcome SingleOutcome((FleetAssetResultDto Row, PolicyValidation Validation) result)
    {
        var (row, validation) = result;
        var fleet = new FleetEvaluationDto
        {
            AssetCount = 1,
            ActionCounts = row.Verdict == "PASS" ? new() { [row.Action] = 1 } : [],
            PassCount = row.Verdict == "PASS" ? 1 : 0,
            DeferredCount = row.Verdict == "NEEDS_REVISION" ? 1 : 0,
            BlockedCount = row.Verdict == "FAIL" ? 1 : 0,
            Assets = [row]
        };
        var summary = $"{row.Action} · {validation.Verdict} · {(validation.IsHighImpact ? "high" : "low")} impact. {row.Reason}";
        return new PolicyOutcome(row.Action, validation, fleet, summary);
    }

    private static PolicyOutcome FleetOutcome(EvaluationScope scope, List<(FleetAssetResultDto Row, PolicyValidation Validation)> results)
    {
        var passing = results.Where(r => r.Row.Verdict == "PASS").ToList();
        var deferred = results.Where(r => r.Row.Verdict == "NEEDS_REVISION").ToList();
        var blocked = results.Where(r => r.Row.Verdict == "FAIL").ToList();

        var actionCounts = passing.GroupBy(r => r.Row.Action).ToDictionary(g => g.Key, g => g.Count());

        // Headline: the action that applies to the most assets, ignoring RETAIN
        // unless nothing else is warranted; ties go to the more consequential one.
        var headline = actionCounts.Where(kv => kv.Key != Retain)
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => Array.IndexOf(ImpactOrder, kv.Key))
            .Select(kv => kv.Key)
            .FirstOrDefault()
            ?? (passing.Count > 0 ? Retain : results[0].Row.Action);

        var verdict = passing.Count > 0 ? "PASS" : deferred.Count > 0 ? "NEEDS_REVISION" : "FAIL";

        // Rule outcomes are judged over the assets the recommendation applies
        // to (the passing ones); "actual" still counts every asset.
        var judged = passing.Count > 0 ? passing : results;
        var ruleIds = results[0].Validation.RuleResults.Select(r => r.RuleId).ToList();
        var ruleResults = ruleIds.Select(id =>
        {
            var all = results.Select(r => r.Validation.RuleResults.First(x => x.RuleId == id)).ToList();
            var worst = judged.Select(r => r.Validation.RuleResults.First(x => x.RuleId == id).Outcome)
                .OrderBy(o => Array.IndexOf(OutcomeSeverity, o)).First();
            var counts = all.GroupBy(r => r.Outcome)
                .OrderBy(g => Array.IndexOf(OutcomeSeverity, g.Key))
                .Select(g => $"{g.Key} {g.Count()}");
            return new PolicyRuleResult
            {
                RuleId = id,
                Expected = all[0].Expected,
                Actual = string.Join(" · ", counts),
                Outcome = worst
            };
        }).ToList();

        var blockingReasons = deferred.Concat(blocked)
            .SelectMany(r => r.Validation.BlockingReasons)
            .GroupBy(reason => reason)
            .Select(g => $"{g.Count()} asset{(g.Count() == 1 ? "" : "s")}: {g.Key}")
            .ToList();

        var validation = new PolicyValidation
        {
            Verdict = verdict,
            RuleResults = ruleResults,
            BlockingReasons = blockingReasons,
            IsHighImpact = passing.Any(r => r.Validation.IsHighImpact)
        };

        var fleet = new FleetEvaluationDto
        {
            AssetCount = results.Count,
            ActionCounts = actionCounts,
            PassCount = passing.Count,
            DeferredCount = deferred.Count,
            BlockedCount = blocked.Count,
            Assets = results
                .Select(r => r.Row)
                .OrderBy(r => Array.IndexOf(OutcomeSeverity, r.Verdict) switch { 2 => 0, 1 => 1, _ => 2 })
                .ThenBy(r => Array.IndexOf(ImpactOrder, r.Action))
                .ThenByDescending(r => r.ProjectedCost)
                .Take(MaxStoredAssets)
                .ToList()
        };

        var mix = string.Join(", ", actionCounts.OrderBy(kv => Array.IndexOf(ImpactOrder, kv.Key)).Select(kv => $"{kv.Value} {kv.Key.ToLowerInvariant()}"));
        var summary = $"{headline} · {verdict} across {results.Count} {scope.AssetTypeName} assets"
            + (mix.Length > 0 ? $" ({mix})" : "")
            + (deferred.Count > 0 ? $" · {deferred.Count} deferred" : "")
            + (blocked.Count > 0 ? $" · {blocked.Count} blocked" : "") + ".";

        return new PolicyOutcome(headline, validation, fleet, summary);
    }
}
