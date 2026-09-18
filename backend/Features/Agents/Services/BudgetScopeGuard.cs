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
        "REPAIR", "REPLACE", "TRANSFER", "DISPOSE"
    };

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

        if (result.RankedOptions == null || result.RankedOptions.Count != 4)
        {
            throw new InvalidOperationException("Assessment must contain exactly 4 ranked lifecycle options.");
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
                    $"Action '{option.Action}' is not an allowed lifecycle action (REPAIR, REPLACE, TRANSFER, DISPOSE).");
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

        if (actionsPresent.Count != 4)
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
    /// Deterministic rule-based assessment used when the LLM call fails, times out, or when no API key is configured.
    /// Uses real organization policy RepairToReplaceCostThreshold if provided; otherwise defaults to 0.70 (70%).
    /// </summary>
    public static FinancialAssessmentResultDto FallbackAssessment(
        AssetFinancialsDto financials,
        FailureStatisticsDto maintenanceAnalysis,
        decimal? policyRepairToReplaceThreshold = null)
    {
        ArgumentNullException.ThrowIfNull(financials);
        ArgumentNullException.ThrowIfNull(maintenanceAnalysis);

        var threshold = policyRepairToReplaceThreshold is > 0
            ? policyRepairToReplaceThreshold.Value
            : DefaultRepairToReplaceThreshold;

        var residual = Math.Max(0m, financials.ResidualBookValue);
        var projectedRepair = Math.Max(0m, maintenanceAnalysis.ProjectedNextTwelveMonthsCost);
        var denominator = Math.Max(residual, 1m);
        var ratio = Math.Round(projectedRepair / denominator, 2);

        string proposed;
        List<RankedOptionDto> rankedOptions;

        if (ratio >= threshold)
        {
            if (residual <= 0m)
            {
                proposed = "DISPOSE";
                rankedOptions =
                [
                    new()
                    {
                        Action = "DISPOSE",
                        Score = 0.85m,
                        Rationale = $"Asset is fully depreciated (residual value $0) and projected 12-month repair cost is ${projectedRepair:N2}, exceeding the economic threshold ({threshold:P0}). Decommissioning is optimal."
                    },
                    new()
                    {
                        Action = "REPLACE",
                        Score = 0.70m,
                        Rationale = $"Procuring a replacement is financially justified as maintenance costs exceed the asset's residual value, but capital expenditure is required."
                    },
                    new()
                    {
                        Action = "TRANSFER",
                        Score = 0.30m,
                        Rationale = $"Transferring an asset with escalating repair needs (${projectedRepair:N2}/yr) merely shifts an economic burden to another department."
                    },
                    new()
                    {
                        Action = "REPAIR",
                        Score = 0.15m,
                        Rationale = $"Projected repair cost of ${projectedRepair:N2} yields a repair-to-replace ratio of {ratio:F2}, exceeding policy threshold {threshold:P0}. Continued repair is uneconomical."
                    }
                ];
            }
            else
            {
                proposed = "REPLACE";
                rankedOptions =
                [
                    new()
                    {
                        Action = "REPLACE",
                        Score = 0.80m,
                        Rationale = $"Projected 12-month repair cost of ${projectedRepair:N2} represents {ratio:P0} of the residual book value (${residual:N2}), exceeding the policy threshold ({threshold:P0}). Replacement is recommended."
                    },
                    new()
                    {
                        Action = "DISPOSE",
                        Score = 0.65m,
                        Rationale = $"Disposal without replacement is feasible if service requirements have subsided, avoiding ongoing maintenance of ${projectedRepair:N2}."
                    },
                    new()
                    {
                        Action = "TRANSFER",
                        Score = 0.35m,
                        Rationale = $"Transfer is only recommended if receiving department has specialized maintenance capability to absorb ${projectedRepair:N2} projected annual costs."
                    },
                    new()
                    {
                        Action = "REPAIR",
                        Score = 0.20m,
                        Rationale = $"Repair-to-replace ratio of {ratio:F2} exceeds acceptable threshold {threshold:P0}. Further repair investment will yield negative returns."
                    }
                ];
            }
        }
        else
        {
            proposed = "REPAIR";
            rankedOptions =
            [
                new()
                {
                    Action = "REPAIR",
                    Score = 0.85m,
                    Rationale = $"Projected 12-month repair cost of ${projectedRepair:N2} represents {ratio:P0} of residual book value (${residual:N2}), safely within the policy threshold ({threshold:P0}). Continuing maintenance is economical."
                },
                new()
                {
                    Action = "TRANSFER",
                    Score = 0.50m,
                    Rationale = $"Asset remains economically viable (ratio {ratio:F2}); transfer to another operational unit is feasible if department requirements change."
                },
                new()
                {
                    Action = "REPLACE",
                    Score = 0.35m,
                    Rationale = $"Premature replacement is unwarranted while the asset retains ${residual:N2} in book value and projected repairs (${projectedRepair:N2}) remain economical."
                },
                new()
                {
                    Action = "DISPOSE",
                    Score = 0.15m,
                    Rationale = $"Asset is in usable condition with substantial remaining value (${residual:N2}); disposal would result in unnecessary capital loss."
                }
            ];
        }

        return new FinancialAssessmentResultDto
        {
            ResidualValue = residual,
            ReplacementEstimate = financials.ReplacementEstimate,
            RepairToReplaceRatio = ratio,
            BudgetHeadroom = null, // Department budget tracking not configured in database schema
            RankedOptions = rankedOptions,
            ProposedRecommendation = proposed
        };
    }
}
