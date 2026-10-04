using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.AgentTools.Services;

public interface IPlannerTools
{
    Task<AssetSummaryDto?> GetAssetSummaryAsync(Guid organizationId, Guid assetId, CancellationToken cancellationToken = default);

    Task<AssetTypeSummaryDto?> GetAssetTypeSummaryAsync(Guid organizationId, Guid assetTypeId, CancellationToken cancellationToken = default);
}
