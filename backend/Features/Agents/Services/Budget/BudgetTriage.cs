using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services.Budget;

public static class BudgetTriage
{
    public const decimal DefaultRepairToReplaceThreshold = 0.70m;

    private static readonly string[] ActionOrder =
    [
        LifecycleActions.Retain, LifecycleActions.Repair, LifecycleActions.Replace, LifecycleActions.Transfer, LifecycleActions.Dispose
    ];

    public static AssetFinancialTriageDto TriageAsset(
        AssetFinancialsDto financials,
        FailureStatisticsDto? maintenance,
        decimal? policyRepairToReplaceThreshold = null)
    {
        ArgumentNullException.ThrowIfNull(financials);

        var threshold = Threshold(policyRepairToReplaceThreshold);
        var residual = Math.Max(0m, financials.ResidualBookValue);
        var projected = Math.Max(0m, maintenance?.ProjectedNextTwelveMonthsCost ?? 0m);
        var ratio = Ratio(projected, residual);

        var action = projected == 0m ? LifecycleActions.Retain
            : ratio < threshold ? LifecycleActions.Repair
            : residual <= 0m ? LifecycleActions.Dispose
            : LifecycleActions.Replace;

        return new AssetFinancialTriageDto
        {
            AssetId = financials.AssetId,
            AssetCode = financials.AssetCode,
            ResidualValue = residual,
            ProjectedCost = projected,
            Ratio = ratio,
            Action = action
        };
    }

    public static FinancialAssessmentResultDto DeterministicAssessment(
        IReadOnlyList<AssetFinancialTriageDto> triage,
        decimal? policyRepairToReplaceThreshold = null,
        decimal? budgetHeadroom = null)
    {
        ArgumentNullException.ThrowIfNull(triage);

        var threshold = Threshold(policyRepairToReplaceThreshold);
        var assetCount = Math.Max(triage.Count, 1);
        var residual = triage.Sum(t => t.ResidualValue);
        var projected = triage.Sum(t => t.ProjectedCost);
        var counts = ActionOrder.ToDictionary(a => a, a => triage.Count(t => t.Action == a));

        var options = ActionOrder
            .Select((action, order) => new RankedOptionDto
            {
                Action = action,
                Score = Math.Clamp(Math.Round(0.10m + 0.85m * counts[action] / assetCount - order * 0.01m, 2), 0m, 1m),
                Rationale = Rationale(action, counts[action], triage.Count, threshold, triage.Where(t => t.Action == action).Sum(t => t.ProjectedCost))
            })
            .OrderByDescending(o => o.Score)
            .ToList();

        return new FinancialAssessmentResultDto
        {
            ResidualValue = residual,
            RepairToReplaceRatio = Ratio(projected, residual),
            BudgetHeadroom = budgetHeadroom,
            RankedOptions = options,
            ProposedRecommendation = options[0].Action,
            AssetCount = triage.Count,
            ProjectedRepairCost = projected,
            Source = AssessmentSources.Deterministic,
            Assets = triage.ToList()
        };
    }

    private static decimal Ratio(decimal projected, decimal residual) => Math.Round(projected / Math.Max(residual, 1m), 2);

    private static decimal Threshold(decimal? policyThreshold) =>
        policyThreshold is > 0 ? policyThreshold.Value : DefaultRepairToReplaceThreshold;

    private static string Rationale(string action, int count, int total, decimal threshold, decimal projected)
    {
        var share = total == 1 ? (count == 1 ? "This asset" : "Not this asset") : $"{count} of {total} assets";
        return action switch
        {
            LifecycleActions.Retain => $"{share}: no repair spend projected for the next 12 months — no lifecycle action needed.",
            LifecycleActions.Repair => $"{share}: projected repairs (LKR {projected:N0}) stay under {threshold:P0} of residual value — repair is economical.",
            LifecycleActions.Replace => $"{share}: projected repairs (LKR {projected:N0}) reach {threshold:P0}+ of residual value while book value remains.",
            LifecycleActions.Dispose => $"{share}: fully depreciated with repairs (LKR {projected:N0}) still projected — beyond economic repair.",
            _ => "No financial signal favours transfer; it depends on departmental need rather than cost."
        };
    }
}
