using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services.Budget;

public static class BudgetAssessmentValidator
{
    private static readonly HashSet<string> AllowedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        LifecycleActions.Repair, LifecycleActions.Replace, LifecycleActions.Transfer, LifecycleActions.Dispose, LifecycleActions.Retain
    };

    private static readonly string[] CoreActions =
    [
        LifecycleActions.Repair, LifecycleActions.Replace, LifecycleActions.Transfer, LifecycleActions.Dispose
    ];

    public static FinancialAssessmentResultDto Validate(FinancialAssessmentResultDto result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.ResidualValue < 0)
            throw Invalid("Residual value must be greater than or equal to 0.");

        if (result.RankedOptions is null || result.RankedOptions.Count is < 4 or > 5)
            throw Invalid("Assessment must contain 4 or 5 ranked lifecycle options (RETAIN is optional).");

        var actionsPresent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in result.RankedOptions)
        {
            ValidateOption(option, actionsPresent);
        }

        if (!CoreActions.All(actionsPresent.Contains))
            throw Invalid("Assessment must cover all 4 actions: REPAIR, REPLACE, TRANSFER, and DISPOSE.");

        ValidateProposal(result);
        return result;
    }

    private static void ValidateOption(RankedOptionDto option, HashSet<string> actionsPresent)
    {
        if (string.IsNullOrWhiteSpace(option.Action))
            throw Invalid("Every ranked option must declare an action.");

        var action = option.Action.Trim().ToUpperInvariant();
        if (!AllowedActions.Contains(action))
            throw Invalid($"Action '{option.Action}' is not an allowed lifecycle action (REPAIR, REPLACE, TRANSFER, DISPOSE, RETAIN).");

        if (!actionsPresent.Add(action))
            throw Invalid($"Action '{action}' is duplicated in ranked options. Each action must appear exactly once.");

        if (option.Score is < 0.0m or > 1.0m)
            throw Invalid($"Option '{action}' score {option.Score} must be within the normalized range [0.0, 1.0].");

        if (string.IsNullOrWhiteSpace(option.Rationale))
            throw Invalid($"Option '{action}' must include an evidence-based rationale.");
    }

    private static void ValidateProposal(FinancialAssessmentResultDto result)
    {
        if (string.IsNullOrWhiteSpace(result.ProposedRecommendation))
            throw Invalid("A proposed recommendation is required.");

        var proposed = result.ProposedRecommendation.Trim().ToUpperInvariant();
        if (!AllowedActions.Contains(proposed))
            throw Invalid($"Proposed recommendation '{result.ProposedRecommendation}' is not an allowed lifecycle action.");

        var highestScore = result.RankedOptions.Max(o => o.Score);
        var isTopScored = result.RankedOptions
            .Where(o => o.Score == highestScore)
            .Any(o => o.Action.Trim().ToUpperInvariant() == proposed);

        if (!isTopScored)
            throw Invalid($"Proposed recommendation '{proposed}' does not match the highest-scoring ranked option (score {highestScore}).");
    }

    private static InvalidOperationException Invalid(string message) => new(message);
}
