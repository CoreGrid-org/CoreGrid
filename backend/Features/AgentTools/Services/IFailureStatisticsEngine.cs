namespace CoreGrid.Api.Features.AgentTools.Services;

public sealed record CompletedRepair(DateOnly CompletionDate, decimal ActualCost);

public sealed record FailureStatisticsResult(
    int RepairCount,
    decimal? MeanTimeBetweenFailuresDays,
    string CostTrend,
    decimal ProjectedNextTwelveMonthsCost);

public interface IFailureStatisticsEngine
{
    FailureStatisticsResult Compute(IReadOnlyList<CompletedRepair> completedCorrectiveRepairs, DateOnly asOfDate);
}
