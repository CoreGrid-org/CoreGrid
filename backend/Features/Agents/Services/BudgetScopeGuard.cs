using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

/// <summary>
/// Pure static verification and deterministic fallback guard for the Budget Analysis Agent (SRS §7.3, Node 3).
/// No I/O, deterministic, and fully unit-testable without any DI.
/// </summary>
public static class BudgetScopeGuard
{
    private static readonly HashSet<string> AllowedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "REPAIR", "REPLACE", "TRANSFER", "DISPOSE", "RETAIN"
    };

    private static readonly string[] CoreActions = ["REPAIR", "REPLACE", "TRANSFER", "DISPOSE"];

    // Tie-break order for equal scores — least disruptive first.
    private static readonly string[] ActionOrder = ["RETAIN", "REPAIR", "REPLACE", "TRANSFER", "DISPOSE"];

    private const decimal DefaultRepairToReplaceThreshold = 0.70m;

    /// <summary>
    /// Validates an assessment produced by the LLM or engine; throws on any structural violation.
    /// </summary>
    public static FinancialAssessmentResultDto ValidateAssessment(FinancialAssessmentResultDto result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.ResidualValue < 0)
        {
            throw new InvalidOperationException("Residual value must be greater than or equal to 0.");
        }

        if (result.RankedOptions == null || result.RankedOptions.Count is < 4 or > 5)
        {
            throw new InvalidOperationException("Assessment must contain 4 or 5 ranked lifecycle options (RETAIN is optional).");
        }

        var actionsPresent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in result.RankedOptions)
        {
            if (string.IsNullOrWhiteSpace(option.Action))
            {
                throw new InvalidOperationException("Every ranked option must declare an action.");
            }

            var upperAction = option.Action.Trim().ToUpperInvariant();
            if (!AllowedActions.Contains(upperAction))
            {
                throw new InvalidOperationException(
                    $"Action '{option.Action}' is not an allowed lifecycle action (REPAIR, REPLACE, TRANSFER, DISPOSE, RETAIN).");
            }

            if (!actionsPresent.Add(upperAction))
            {
                throw new InvalidOperationException(
                    $"Action '{upperAction}' is duplicated in ranked options. Each action must appear exactly once.");
            }

            if (option.Score is < 0.0m or > 1.0m)
            {
                throw new InvalidOperationException(
                    $"Option '{upperAction}' score {option.Score} must be within the normalized range [0.0, 1.0].");
            }

            if (string.IsNullOrWhiteSpace(option.Rationale))
            {
                throw new InvalidOperationException(
                    $"Option '{upperAction}' must include an evidence-based rationale.");
            }
        }

        if (!CoreActions.All(actionsPresent.Contains))
        {
            throw new InvalidOperationException(
                "Assessment must cover all 4 actions: REPAIR, REPLACE, TRANSFER, and DISPOSE.");
        }

        if (string.IsNullOrWhiteSpace(result.ProposedRecommendation))
        {
            throw new InvalidOperationException("A proposed recommendation is required.");
        }

        var proposed = result.ProposedRecommendation.Trim().ToUpperInvariant();
        if (!AllowedActions.Contains(proposed))
        {
            throw new InvalidOperationException(
                $"Proposed recommendation '{result.ProposedRecommendation}' is not an allowed lifecycle action.");
        }

        var highestScore = result.RankedOptions.Max(o => o.Score);
        var highestOptions = result.RankedOptions
            .Where(o => o.Score == highestScore)
            .Select(o => o.Action.Trim().ToUpperInvariant())
            .ToList();

        if (!highestOptions.Contains(proposed))
        {
            throw new InvalidOperationException(
                $"Proposed recommendation '{proposed}' does not match the highest-scoring ranked option (score {highestScore}).");
        }

        return result;
    }

    /// <summary>
    /// One asset's financial triage: projected 12-month repair cost against
    /// residual book value. No projected spend → RETAIN; ratio at or above the
    /// policy threshold → REPLACE (or DISPOSE once fully depreciated); else REPAIR.
    /// </summary>
    public static AssetFinancialTriageDto TriageAsset(
        AssetFinancialsDto financials,
        FailureStatisticsDto? maintenance,
        decimal? policyRepairToReplaceThreshold = null)
    {
        ArgumentNullException.ThrowIfNull(financials);

        var threshold = Threshold(policyRepairToReplaceThreshold);
        var residual = Math.Max(0m, financials.ResidualBookValue);
        var projected = Math.Max(0m, maintenance?.ProjectedNextTwelveMonthsCost ?? 0m);
        var ratio = Math.Round(projected / Math.Max(residual, 1m), 2);

        var action = projected == 0m ? "RETAIN"
            : ratio >= threshold ? (residual <= 0m ? "DISPOSE" : "REPLACE")
            : "REPAIR";

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

    /// <summary>
    /// Deterministic assessment across a scope (one asset or an asset-type
    /// fleet): totals, and every action scored by the share of assets whose
    /// triage lands on it. Used as-is without a model, and as the base the
    /// model's scores are merged onto.
    /// </summary>
    public static FinancialAssessmentResultDto DeterministicAssessment(
        IReadOnlyList<AssetFinancialTriageDto> triage,
        decimal? policyRepairToReplaceThreshold = null,
        decimal? budgetHeadroom = null)
    {
        ArgumentNullException.ThrowIfNull(triage);

        var threshold = Threshold(policyRepairToReplaceThreshold);
        var n = Math.Max(triage.Count, 1);
        var residual = triage.Sum(t => t.ResidualValue);
        var projected = triage.Sum(t => t.ProjectedCost);
        var ratio = Math.Round(projected / Math.Max(residual, 1m), 2);
        var counts = ActionOrder.ToDictionary(a => a, a => triage.Count(t => t.Action == a));

        var options = ActionOrder
            .Select((action, order) => new RankedOptionDto
            {
                Action = action,
                // Share of the fleet, nudged by the tie-break order so ranks are distinct.
                Score = Math.Clamp(Math.Round(0.10m + 0.85m * counts[action] / n - order * 0.01m, 2), 0m, 1m),
                Rationale = Rationale(action, counts[action], triage.Count, threshold, triage.Where(t => t.Action == action).Sum(t => t.ProjectedCost))
            })
            .OrderByDescending(o => o.Score)
            .ToList();

        return new FinancialAssessmentResultDto
        {
            ResidualValue = residual,
            ReplacementEstimate = null,
            RepairToReplaceRatio = ratio,
            BudgetHeadroom = budgetHeadroom,
            RankedOptions = options,
            ProposedRecommendation = options[0].Action,
            AssetCount = triage.Count,
            ProjectedRepairCost = projected,
            Source = "DETERMINISTIC",
            Assets = triage.ToList()
        };
    }

    /// <summary>Single-asset convenience over TriageAsset + DeterministicAssessment.</summary>
    public static FinancialAssessmentResultDto FallbackAssessment(
        AssetFinancialsDto financials,
        FailureStatisticsDto maintenanceAnalysis,
        decimal? policyRepairToReplaceThreshold = null)
    {
        ArgumentNullException.ThrowIfNull(maintenanceAnalysis);
        return DeterministicAssessment([TriageAsset(financials, maintenanceAnalysis, policyRepairToReplaceThreshold)], policyRepairToReplaceThreshold);
    }

    private static decimal Threshold(decimal? policyThreshold) =>
        policyThreshold is > 0 ? policyThreshold.Value : DefaultRepairToReplaceThreshold;

    private static string Rationale(string action, int count, int total, decimal threshold, decimal projected)
    {
        var share = total == 1 ? (count == 1 ? "This asset" : "Not this asset") : $"{count} of {total} assets";
        return action switch
        {
            "RETAIN" => $"{share}: no repair spend projected for the next 12 months — no lifecycle action needed.",
            "REPAIR" => $"{share}: projected repairs (LKR {projected:N0}) stay under {threshold:P0} of residual value — repair is economical.",
            "REPLACE" => $"{share}: projected repairs (LKR {projected:N0}) reach {threshold:P0}+ of residual value while book value remains.",
            "DISPOSE" => $"{share}: fully depreciated with repairs (LKR {projected:N0}) still projected — beyond economic repair.",
            _ => "No financial signal favours transfer; it depends on departmental need rather than cost."
        };
    }
}
