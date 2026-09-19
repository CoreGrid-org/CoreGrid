namespace CoreGrid.Api.Features.Shared.Finance;

public record DepreciationResult(
    decimal AnnualDepreciation,
    decimal AccumulatedDepreciation,
    decimal CurrentValue);

// One straight-line depreciation implementation, replacing the three that
// previously computed the same thing slightly differently:
// AssetService.CalculateResidualValue (fractional elapsed years, 365.25-day
// based), AgentToolsService.ComputeDepreciation (whole elapsed years,
// calendar-anniversary based) and DisposalPreconditionService.CheckP3
// (also whole elapsed years, same algorithm as AgentToolsService's). Both
// elapsed-time conventions are kept as named methods — whole years for
// depreciation schedules and P3's minimum-service-life check, fractional
// years for the residual-value/compliance-state figure — rather than
// picked as "the one true way", since collapsing them would silently
// change every existing caller's numbers.
public static class StraightLineDepreciation
{
    // Whole calendar years elapsed from acquisitionDate to asOf, counting
    // an anniversary only once it has actually passed (not yet reached ->
    // one fewer full year). Never negative.
    public static int ElapsedWholeYears(DateOnly acquisitionDate, DateOnly asOf)
    {
        var years = asOf.Year - acquisitionDate.Year;
        if (asOf < acquisitionDate.AddYears(years))
        {
            years--;
        }

        return Math.Max(0, years);
    }

    // Elapsed years as a continuous fraction, using the 365.25-day-year
    // convention AssetService's residual-value figure has always used.
    public static double ElapsedFractionalYears(DateOnly acquisitionDate, DateTimeOffset asOf)
    {
        var acquisitionDateTime = acquisitionDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var elapsedDays = (asOf - acquisitionDateTime).TotalDays;
        return elapsedDays / 365.25;
    }

    // Whole-year depreciation schedule: annual charge, cost-capped
    // accumulated depreciation, and current book value. usefulLifeYears <= 0
    // or acquisitionCost <= 0 is treated as "no depreciation" (the asset's
    // full acquisition cost, floored at zero) rather than a division error
    // — matching AgentToolsService.ComputeDepreciation's existing behaviour
    // for an asset with no AssetType.
    public static DepreciationResult ComputeSchedule(decimal acquisitionCost, DateOnly acquisitionDate, int usefulLifeYears, DateOnly asOf)
    {
        if (usefulLifeYears <= 0 || acquisitionCost <= 0)
        {
            return new DepreciationResult(0, 0, Math.Max(0, acquisitionCost));
        }

        var yearsElapsed = ElapsedWholeYears(acquisitionDate, asOf);

        var annualDepreciation = acquisitionCost / usefulLifeYears;
        var accumulated = Math.Min(annualDepreciation * yearsElapsed, acquisitionCost);
        var currentValue = Math.Max(0, acquisitionCost - accumulated);

        return new DepreciationResult(
            Math.Round(annualDepreciation, 2),
            Math.Round(accumulated, 2),
            Math.Round(currentValue, 2));
    }

    // Fractional-year residual value, matching AssetService's existing
    // figure used for the asset register's displayed residual value.
    public static decimal CalculateResidualValue(decimal acquisitionCost, DateOnly acquisitionDate, int usefulLifeYears, DateTimeOffset? asOf = null)
    {
        if (usefulLifeYears <= 0)
        {
            return Math.Round(acquisitionCost, 2, MidpointRounding.AwayFromZero);
        }

        var elapsedYears = ElapsedFractionalYears(acquisitionDate, asOf ?? DateTimeOffset.UtcNow);
        if (elapsedYears <= 0)
        {
            return Math.Round(acquisitionCost, 2, MidpointRounding.AwayFromZero);
        }

        var depreciation = (acquisitionCost / usefulLifeYears) * (decimal)elapsedYears;
        var residualValue = acquisitionCost - depreciation;

        return Math.Max(0m, Math.Round(residualValue, 2, MidpointRounding.AwayFromZero));
    }
}
