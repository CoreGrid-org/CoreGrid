using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Shared;

namespace CoreGrid.Api.Features.Assets.Services;

public interface IAssetService
{
    Task<PagedResult<AssetDto>> GetAssetsAsync(
        Guid organizationId,
        AssetQueryParameters parameters);

    Task<AssetDetailDto?> GetAssetByIdAsync(
        Guid organizationId,
        Guid assetId);

    Task<AssetDetailDto> CreateAssetAsync(
        Guid organizationId,
        Guid? userId,
        CreateAssetRequest request);

    Task<AssetDetailDto?> UpdateAssetAsync(
        Guid organizationId,
        Guid assetId,
        Guid? userId,
        UpdateAssetRequest request);

    Task<bool> UpdateConditionAsync(
        Guid organizationId,
        Guid assetId,
        Guid? userId,
        UpdateAssetConditionRequest request);

    Task<AssetDetailDto?> GetAssetByQrCodeAsync(
        Guid organizationId,
        string code);
}