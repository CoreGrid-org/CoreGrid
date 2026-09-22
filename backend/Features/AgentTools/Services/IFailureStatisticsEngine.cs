namespace CoreGrid.Api.Features.AgentTools.Services;

public record CompletedRepair(DateOnly CompletionDate, decimal ActualCost);

public record FailureStatisticsResult(
    int RepairCount,
    decimal? MeanTimeBetweenFailuresDays,
    string CostTrend,
    decimal ProjectedNextTwelveMonthsCost);

// Calculates failure statistics from completed corrective repairs.
public interface IFailureStatisticsEngine
{
    FailureStatisticsResult Compute(IReadOnlyList<CompletedRepair> completedCorrectiveRepairs, DateOnly asOfDate);
}
