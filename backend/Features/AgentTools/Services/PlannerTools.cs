using CoreGrid.Api.Data;
using CoreGrid.Api.Features.AgentTools.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.AgentTools.Services;

public sealed class PlannerTools(CoreGridDbContext db) : IPlannerTools
{
    public Task<AssetSummaryDto?> GetAssetSummaryAsync(Guid organizationId, Guid assetId, CancellationToken cancellationToken = default) =>
        db.Assets.AsNoTracking()
            .Matching(AssetSelection.Single(organizationId, assetId))
            .Select(a => new AssetSummaryDto
            {
                AssetId = a.Id,
                AssetCode = a.AssetCode,
                Name = a.Name,
                AssetType = a.AssetType != null ? a.AssetType.Name : string.Empty,
                Category = a.AssetType != null && a.AssetType.AssetCategory != null ? a.AssetType.AssetCategory.Name : string.Empty,
                Status = a.Status,
                Condition = a.Condition,
                Department = a.Department != null ? a.Department.Name : string.Empty,
                Location = a.Location != null ? a.Location.Name : string.Empty,
                AcquisitionDate = a.AcquisitionDate,
                AcquisitionCost = a.AcquisitionCost
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<AssetTypeSummaryDto?> GetAssetTypeSummaryAsync(Guid organizationId, Guid assetTypeId, CancellationToken cancellationToken = default)
    {
        var type = await db.AssetTypes.AsNoTracking()
            .Where(t => t.Id == assetTypeId && t.OrganizationId == organizationId)
            .Select(t => new { t.Id, t.Name, Category = t.AssetCategory!.Name, t.UsefulLifeYears })
            .FirstOrDefaultAsync(cancellationToken);
        if (type is null) return null;

        var conditionCounts = await db.Assets.AsNoTracking()
            .Matching(AssetSelection.Fleet(organizationId, assetTypeId))
            .GroupBy(a => a.Condition)
            .Select(g => new { Condition = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Condition, g => g.Count, cancellationToken);

        return new AssetTypeSummaryDto
        {
            AssetTypeId = type.Id,
            AssetType = type.Name,
            Category = type.Category,
            UsefulLifeYears = type.UsefulLifeYears,
            ActiveAssetCount = conditionCounts.Values.Sum(),
            ConditionCounts = conditionCounts
        };
    }
}
