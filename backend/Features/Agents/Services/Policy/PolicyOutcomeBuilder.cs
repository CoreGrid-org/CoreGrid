using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services.Policy;

internal sealed record AssetEvaluation(FleetAssetResultDto Row, PolicyValidation Validation);

internal static class PolicyOutcomeBuilder
{
    private const int MaxStoredAssets = 200;

    private static readonly string[] ImpactOrder =
    [
        LifecycleActions.Dispose, LifecycleActions.Replace, LifecycleActions.Repair, LifecycleActions.Transfer, LifecycleActions.Retain
    ];

    private static readonly string[] OutcomeSeverity =
    [
        PolicyVerdicts.Fail, PolicyVerdicts.NeedsRevision, PolicyVerdicts.Pass, PolicyVerdicts.NotApplicable
    ];

    public static PolicyOutcome ForAsset(AssetEvaluation result)
    {
        var (row, validation) = result;
        var passed = row.Verdict == PolicyVerdicts.Pass;
        var fleet = new FleetEvaluationDto
        {
            AssetCount = 1,
            ActionCounts = passed ? new() { [row.Action] = 1 } : [],
            PassCount = passed ? 1 : 0,
            DeferredCount = row.Verdict == PolicyVerdicts.NeedsRevision ? 1 : 0,
            BlockedCount = row.Verdict == PolicyVerdicts.Fail ? 1 : 0,
            Assets = [row]
        };

        var summary = $"{row.Action} · {validation.Verdict} · {(validation.IsHighImpact ? "high" : "low")} impact. {row.Reason}";
        return new PolicyOutcome(row.Action, validation, fleet, summary);
    }

    public static PolicyOutcome ForFleet(EvaluationScope scope, List<AssetEvaluation> results)
    {
        var passing = results.Where(r => r.Row.Verdict == PolicyVerdicts.Pass).ToList();
        var deferred = results.Where(r => r.Row.Verdict == PolicyVerdicts.NeedsRevision).ToList();
        var blocked = results.Where(r => r.Row.Verdict == PolicyVerdicts.Fail).ToList();
        var actionCounts = passing.GroupBy(r => r.Row.Action).ToDictionary(g => g.Key, g => g.Count());

        var headline = actionCounts.Where(kv => kv.Key != LifecycleActions.Retain)
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => Array.IndexOf(ImpactOrder, kv.Key))
            .Select(kv => kv.Key)
            .FirstOrDefault()
            ?? (passing.Count > 0 ? LifecycleActions.Retain : results[0].Row.Action);

        var validation = new PolicyValidation
        {
            Verdict = passing.Count > 0 ? PolicyVerdicts.Pass : deferred.Count > 0 ? PolicyVerdicts.NeedsRevision : PolicyVerdicts.Fail,
            RuleResults = RollUpRules(results, passing.Count > 0 ? passing : results),
            BlockingReasons = deferred.Concat(blocked)
                .SelectMany(r => r.Validation.BlockingReasons)
                .GroupBy(reason => reason)
                .Select(g => $"{g.Count()} asset{(g.Count() == 1 ? "" : "s")}: {g.Key}")
                .ToList(),
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
                .OrderBy(r => r.Verdict switch { PolicyVerdicts.Pass => 0, PolicyVerdicts.NeedsRevision => 1, _ => 2 })
                .ThenBy(r => Array.IndexOf(ImpactOrder, r.Action))
                .ThenByDescending(r => r.ProjectedCost)
                .Take(MaxStoredAssets)
                .ToList()
        };

        return new PolicyOutcome(headline, validation, fleet, FleetSummary(scope, headline, validation.Verdict, fleet));
    }

    private static List<PolicyRuleResult> RollUpRules(List<AssetEvaluation> all, List<AssetEvaluation> judged) =>
        all[0].Validation.RuleResults.Select(rule =>
        {
            var outcomes = all.Select(r => r.Validation.RuleResults.First(x => x.RuleId == rule.RuleId)).ToList();
            var worst = judged
                .Select(r => r.Validation.RuleResults.First(x => x.RuleId == rule.RuleId).Outcome)
                .OrderBy(o => Array.IndexOf(OutcomeSeverity, o))
                .First();
            var counts = outcomes.GroupBy(r => r.Outcome)
                .OrderBy(g => Array.IndexOf(OutcomeSeverity, g.Key))
                .Select(g => $"{g.Key} {g.Count()}");

            return new PolicyRuleResult
            {
                RuleId = rule.RuleId,
                Expected = outcomes[0].Expected,
                Actual = string.Join(" · ", counts),
                Outcome = worst
            };
        }).ToList();

    private static string FleetSummary(EvaluationScope scope, string headline, string verdict, FleetEvaluationDto fleet)
    {
        var mix = string.Join(", ", fleet.ActionCounts
            .OrderBy(kv => Array.IndexOf(ImpactOrder, kv.Key))
            .Select(kv => $"{kv.Value} {kv.Key.ToLowerInvariant()}"));

        return $"{headline} · {verdict} across {fleet.AssetCount} {scope.AssetTypeName} assets"
            + (mix.Length > 0 ? $" ({mix})" : "")
            + (fleet.DeferredCount > 0 ? $" · {fleet.DeferredCount} deferred" : "")
            + (fleet.BlockedCount > 0 ? $" · {fleet.BlockedCount} blocked" : "") + ".";
    }
}
