using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.AgentTools.Services;

public sealed class FailureStatisticsEngine : IFailureStatisticsEngine
{
    private const decimal TrendSensitivity = 0.1m;
    private const int ProjectionWindowDays = 365;

    public FailureStatisticsResult Compute(IReadOnlyList<CompletedRepair> completedCorrectiveRepairs, DateOnly asOfDate)
    {
        var repairs = completedCorrectiveRepairs.OrderBy(r => r.CompletionDate).ToList();

        return new FailureStatisticsResult(
            repairs.Count,
            MeanTimeBetweenFailures(repairs),
            CostTrend(repairs),
            ProjectedNextTwelveMonthsCost(repairs, asOfDate));
    }

    private static decimal? MeanTimeBetweenFailures(List<CompletedRepair> repairs)
    {
        if (repairs.Count < 2) return null;

        var totalDays = repairs[^1].CompletionDate.DayNumber - repairs[0].CompletionDate.DayNumber;
        return Math.Round((decimal)totalDays / (repairs.Count - 1), 1);
    }

    private static string CostTrend(List<CompletedRepair> repairs)
    {
        if (repairs.Count < 2) return CostTrends.InsufficientData;

        var midpoint = repairs.Count / 2;
        var earlier = repairs.Take(midpoint).Average(r => r.ActualCost);
        var later = repairs.Skip(midpoint).Average(r => r.ActualCost);

        if (earlier == 0) return later > 0 ? CostTrends.Increasing : CostTrends.Stable;

        return ((later - earlier) / earlier) switch
        {
            > TrendSensitivity => CostTrends.Increasing,
            < -TrendSensitivity => CostTrends.Decreasing,
            _ => CostTrends.Stable
        };
    }

    private static decimal ProjectedNextTwelveMonthsCost(List<CompletedRepair> repairs, DateOnly asOfDate)
    {
        var windowStart = asOfDate.AddDays(-ProjectionWindowDays);
        return repairs
            .Where(r => r.CompletionDate >= windowStart && r.CompletionDate <= asOfDate)
            .Sum(r => r.ActualCost);
    }
}
