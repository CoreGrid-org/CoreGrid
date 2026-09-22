namespace CoreGrid.Api.Features.Shared.Finance;

public record DepreciationResult(
    decimal AnnualDepreciation,
    decimal AccumulatedDepreciation,
    decimal CurrentValue);

// Provides straight-line depreciation calculations.
public static class StraightLineDepreciation
{
 // Calculates the number of complete years elapsed.
    public static int ElapsedWholeYears(DateOnly acquisitionDate, DateOnly asOf)
    {
        var years = asOf.Year - acquisitionDate.Year;
        if (asOf < acquisitionDate.AddYears(years))
        {
            years--;
        }

        return Math.Max(0, years);
    }

    // Calculates elapsed years as a fractional value.
    public static double ElapsedFractionalYears(DateOnly acquisitionDate, DateTimeOffset asOf)
    {
        var acquisitionDateTime = acquisitionDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var elapsedDays = (asOf - acquisitionDateTime).TotalDays;
        return elapsedDays / 365.25;
    }

   // Calculates annual depreciation, accumulated depreciation, and current value.
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
