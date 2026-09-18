namespace CoreGrid.Api.Features.AgentTools.Services;

public record CompletedRepair(DateOnly CompletionDate, decimal ActualCost);

public record FailureStatisticsResult(
    int RepairCount,
    decimal? MeanTimeBetweenFailuresDays,
    string CostTrend,
    decimal ProjectedNextTwelveMonthsCost);

// The Maintenance Analysis Agent's statistics step (SRS §7.3, graph node 2).
// Deliberately a pure function over completed CORRECTIVE repairs — same
// reasoning as IPolicyRuleEngine/IAssetActionRecommendationEngine: no DB
// access, no LLM call, so it's unit-testable and its numbers are provably
// reproducible from the same inputs.
public interface IFailureStatisticsEngine
{
    FailureStatisticsResult Compute(IReadOnlyList<CompletedRepair> completedCorrectiveRepairs, DateOnly asOfDate);
}
