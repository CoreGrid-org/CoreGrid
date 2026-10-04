using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

// Rolls per-asset failure statistics up into one scope-level figure for the
// Maintenance Analysis node's output. Pure and deterministic.
internal static class MaintenanceAggregation
{
    private const string InsufficientData = "INSUFFICIENT_DATA";

    // Trend ranking used to break ties — the more cautious reading wins.
    private static readonly string[] TrendPriority = ["INCREASING", "STABLE", "DECREASING"];

    public static FailureStatisticsDto Aggregate(EvaluationScope scope, IReadOnlyList<FailureStatisticsDto> perAsset)
    {
        if (scope.IsSingleAsset && perAsset.Count == 1)
        {
            return perAsset[0];
        }

        var mtbfs = perAsset.Where(s => s.MeanTimeBetweenFailuresDays.HasValue).Select(s => s.MeanTimeBetweenFailuresDays!.Value).ToList();
        return new FailureStatisticsDto
        {
            AssetId = Guid.Empty,
            AssetCode = scope.AssetTypeName,
            AssetCount = perAsset.Count,
            AssetsWithRepairs = perAsset.Count(s => s.RepairCount > 0),
            RepairCount = perAsset.Sum(s => s.RepairCount),
            MeanTimeBetweenFailuresDays = mtbfs.Count > 0 ? Math.Round(mtbfs.Average(), 1) : null,
            CostTrend = DominantTrend(perAsset),
            ProjectedNextTwelveMonthsCost = perAsset.Sum(s => s.ProjectedNextTwelveMonthsCost),
            EvaluatedAsOf = perAsset.Select(s => s.EvaluatedAsOf).DefaultIfEmpty(DateOnly.FromDateTime(DateTime.UtcNow)).Max()
        };
    }

    // The most common trend among assets with enough history to have one.
    public static string DominantTrend(IReadOnlyList<FailureStatisticsDto> perAsset)
    {
        var trends = perAsset.Select(s => s.CostTrend).Where(t => TrendPriority.Contains(t)).ToList();
        if (trends.Count == 0) return InsufficientData;

        return trends
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => Array.IndexOf(TrendPriority, g.Key))
            .First().Key;
    }
}
