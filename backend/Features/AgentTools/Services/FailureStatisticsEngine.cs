namespace CoreGrid.Api.Features.AgentTools.Services;

public class FailureStatisticsEngine : IFailureStatisticsEngine
{
    private const string Increasing = "INCREASING";
    private const string Decreasing = "DECREASING";
    private const string Stable = "STABLE";
    private const string InsufficientData = "INSUFFICIENT_DATA";

    public FailureStatisticsResult Compute(IReadOnlyList<CompletedRepair> completedCorrectiveRepairs, DateOnly asOfDate)
    {
        var repairs = completedCorrectiveRepairs.OrderBy(r => r.CompletionDate).ToList();
        var repairCount = repairs.Count;

        var mtbf = ComputeMeanTimeBetweenFailures(repairs);
        var costTrend = ComputeCostTrend(repairs);
        var projection = ComputeProjectedNextTwelveMonthsCost(repairs, asOfDate);

        return new FailureStatisticsResult(repairCount, mtbf, costTrend, projection);
    }

    // Average number of days between consecutive completed repairs — needs
    // at least two to have a gap to measure at all.
    private static decimal? ComputeMeanTimeBetweenFailures(List<CompletedRepair> repairs)
    {
        if (repairs.Count < 2)
        {
            return null;
        }

        var totalDays = 0;
        for (var i = 1; i < repairs.Count; i++)
        {
            totalDays += repairs[i].CompletionDate.DayNumber - repairs[i - 1].CompletionDate.DayNumber;
        }

        return Math.Round((decimal)totalDays / (repairs.Count - 1), 1);
    }

    // Splits repairs chronologically in half and compares the average cost
   
    private static string ComputeCostTrend(List<CompletedRepair> repairs)
    {
        if (repairs.Count < 2)
        {
            return InsufficientData;
        }

        var midpoint = repairs.Count / 2;
        var firstHalf = repairs.Take(midpoint).Average(r => r.ActualCost);
        var secondHalf = repairs.Skip(midpoint).Average(r => r.ActualCost);

        if (firstHalf == 0)
        {
            return secondHalf > 0 ? Increasing : Stable;
        }

        var changeRatio = (secondHalf - firstHalf) / firstHalf;
        return changeRatio switch
        {
            > 0.1m => Increasing,
            < -0.1m => Decreasing,
            _ => Stable,
        };
    }

    // Naive trailing-twelve-months projection: sums actual cost from repairs
    // completed in the last 365 days and uses that total as the projection
    // for the next twelve months — "the next year probably looks like the
    // last one," not a seasonally-adjusted forecast.
    private static decimal ComputeProjectedNextTwelveMonthsCost(List<CompletedRepair> repairs, DateOnly asOfDate)
    {
        var windowStart = asOfDate.AddDays(-365);
        return repairs
            .Where(r => r.CompletionDate >= windowStart && r.CompletionDate <= asOfDate)
            .Sum(r => r.ActualCost);
    }
}
