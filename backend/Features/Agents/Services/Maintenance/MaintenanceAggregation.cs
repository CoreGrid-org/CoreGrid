using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services.Maintenance;

internal static class MaintenanceAggregation
{
    private static readonly string[] TrendPriority = [CostTrends.Increasing, CostTrends.Stable, CostTrends.Decreasing];

    public static FailureStatisticsDto Aggregate(EvaluationScope scope, IReadOnlyList<FailureStatisticsDto> perAsset)
    {
        if (scope.IsSingleAsset && perAsset.Count == 1)
        {
            return perAsset[0];
        }

        var mtbfs = perAsset
            .Where(s => s.MeanTimeBetweenFailuresDays.HasValue)
            .Select(s => s.MeanTimeBetweenFailuresDays!.Value)
            .ToList();

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

    public static string DominantTrend(IReadOnlyList<FailureStatisticsDto> perAsset)
    {
        var trends = perAsset.Select(s => s.CostTrend).Where(TrendPriority.Contains).ToList();
        if (trends.Count == 0) return CostTrends.InsufficientData;

        return trends
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => Array.IndexOf(TrendPriority, g.Key))
            .First().Key;
    }
}
