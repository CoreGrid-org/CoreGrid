using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.AgentTools.Services;

internal static class AssetSelectionExtensions
{
    public static IQueryable<Asset> Matching(this IQueryable<Asset> assets, AssetSelection selection) =>
        assets.Where(a => a.OrganizationId == selection.OrganizationId
            && (selection.AssetTypeId == null || a.AssetTypeId == selection.AssetTypeId)
            && (selection.AssetId == null || a.Id == selection.AssetId)
            && (selection.IncludeDisposed || a.Status != AssetStatuses.Disposed));
}
