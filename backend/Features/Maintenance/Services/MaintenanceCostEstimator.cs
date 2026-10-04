using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Maintenance.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Maintenance.Services;

public interface IMaintenanceCostEstimator
{
    Task<MaintenanceCostSuggestionDto?> SuggestAsync(Guid organizationId, Guid maintenanceId, CancellationToken cancellationToken);
}

// Suggests an estimated cost for a maintenance record from completed work of
// the same classification (CORRECTIVE / PREVENTIVE). History is searched from
// the most specific slice outward — this asset, its asset type, its category,
// then the whole organisation — and the first slice with enough samples wins.
// Within it, same-priority work is preferred when there's enough of it.
// Outliers are trimmed (1.5×IQR), and the suggestion is a recency-weighted
// median (one-year half-life) with the weighted P25–P75 as its range.
public class MaintenanceCostEstimator(CoreGridDbContext db) : IMaintenanceCostEstimator
{
    private const int MinSamples = 3;
    private const int LookbackYears = 5;
    private const double HalfLifeDays = 365;

    public async Task<MaintenanceCostSuggestionDto?> SuggestAsync(Guid organizationId, Guid maintenanceId, CancellationToken cancellationToken)
    {
        var target = await db.MaintenanceRecords.AsNoTracking()
            .Where(m => m.Id == maintenanceId && m.OrganizationId == organizationId)
            .Select(m => new
            {
                m.Id,
                m.AssetId,
                m.Type,
                m.Priority,
                AssetTypeId = m.Asset!.AssetTypeId,
                AssetTypeName = m.Asset.AssetType!.Name,
                CategoryId = m.Asset.AssetType.AssetCategoryId,
                CategoryName = m.Asset.AssetType.AssetCategory!.Name,
                AssetCode = m.Asset.AssetCode
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (target is null) return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var since = today.AddYears(-LookbackYears);

        // One query: every comparable completed record in the organisation's
        // recent history; the tiers below are just in-memory slices of it.
        var history = await db.MaintenanceRecords.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId
                && m.Id != target.Id
                && m.Type == target.Type
                && m.Status == MaintenanceStatus.COMPLETED
                && m.ActualCost != null && m.ActualCost > 0
                && m.CompletionDate != null && m.CompletionDate >= since)
            .Select(m => new Sample(
                m.AssetId,
                m.Asset!.AssetTypeId,
                m.Asset.AssetType!.AssetCategoryId,
                m.Priority,
                m.ActualCost!.Value,
                m.CompletionDate!.Value))
            .ToListAsync(cancellationToken);

        var typeLabel = target.Type.ToString().ToLowerInvariant();
        var tiers = new (string Basis, string Label, Func<Sample, bool> Match)[]
        {
            ("ASSET", $"{typeLabel} work on {target.AssetCode}", s => s.AssetId == target.AssetId),
            ("ASSET_TYPE", $"{typeLabel} work on {target.AssetTypeName} assets", s => s.AssetTypeId == target.AssetTypeId),
            ("CATEGORY", $"{typeLabel} work in the {target.CategoryName} category", s => s.CategoryId == target.CategoryId),
            ("ORGANIZATION", $"{typeLabel} work across the organisation", _ => true),
        };

        // First tier with enough data; failing that, the most specific tier with any.
        var slices = tiers.Select(t => (t.Basis, t.Label, Samples: history.Where(t.Match).ToList())).ToList();
        var (basis, label, samples) = slices.FirstOrDefault(s => s.Samples.Count >= MinSamples);
        if (samples is null)
        {
            (basis, label, samples) = slices.FirstOrDefault(s => s.Samples.Count > 0);
        }

        if (samples is null || samples.Count == 0)
        {
            return new MaintenanceCostSuggestionDto
            {
                Basis = "NONE",
                BasisLabel = $"No completed {typeLabel} work with a recorded cost yet",
                Confidence = "NONE",
                Method = "Not enough history to suggest a value — enter the estimate manually."
            };
        }

        var samePriority = samples.Where(s => s.Priority == target.Priority).ToList();
        var priorityMatched = samePriority.Count >= MinSamples;
        if (priorityMatched) samples = samePriority;

        var weighted = Trim(samples)
            .Select(s => (Cost: s.Cost, Weight: Math.Pow(0.5, Math.Max(0, today.DayNumber - s.CompletedOn.DayNumber) / HalfLifeDays)))
            .OrderBy(x => x.Cost)
            .ToList();

        var confidence = weighted.Count >= 8 && basis is "ASSET" or "ASSET_TYPE"
            ? "HIGH"
            : weighted.Count >= MinSamples ? "MEDIUM" : "LOW";

        return new MaintenanceCostSuggestionDto
        {
            SuggestedCost = RoundCost(WeightedQuantile(weighted, 0.5)),
            LowCost = RoundCost(WeightedQuantile(weighted, 0.25)),
            HighCost = RoundCost(WeightedQuantile(weighted, 0.75)),
            SampleSize = weighted.Count,
            Basis = basis,
            BasisLabel = priorityMatched ? $"{target.Priority.ToString().ToLowerInvariant()}-priority {label}" : label,
            PriorityMatched = priorityMatched,
            Confidence = confidence,
            Method = "Recency-weighted median of comparable completed work (outliers trimmed); range is the 25th–75th percentile."
        };
    }

    // Drops values outside 1.5×IQR once there are enough points for quartiles to mean anything.
    private static List<Sample> Trim(List<Sample> samples)
    {
        if (samples.Count < 4) return samples;

        var costs = samples.Select(s => s.Cost).OrderBy(c => c).ToList();
        var q1 = Quantile(costs, 0.25);
        var q3 = Quantile(costs, 0.75);
        var fence = 1.5m * (q3 - q1);
        var kept = samples.Where(s => s.Cost >= q1 - fence && s.Cost <= q3 + fence).ToList();
        return kept.Count > 0 ? kept : samples;
    }

    private static decimal Quantile(List<decimal> sorted, double q)
    {
        var position = (sorted.Count - 1) * q;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        return sorted[lower] + (sorted[upper] - sorted[lower]) * (decimal)(position - lower);
    }

    private static decimal WeightedQuantile(List<(decimal Cost, double Weight)> sorted, double q)
    {
        var total = sorted.Sum(x => x.Weight);
        var target = total * q;
        var running = 0d;
        foreach (var (cost, weight) in sorted)
        {
            running += weight;
            if (running >= target) return cost;
        }
        return sorted[^1].Cost;
    }

    // Whole currency units below 1,000; nearest 10 above — an estimate, not an invoice.
    private static decimal RoundCost(decimal value) =>
        value < 1000m ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : Math.Round(value / 10m, 0, MidpointRounding.AwayFromZero) * 10m;

    private sealed record Sample(Guid AssetId, Guid AssetTypeId, Guid CategoryId, MaintenancePriority Priority, decimal Cost, DateOnly CompletedOn);
}
