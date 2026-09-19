using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Scoping;

namespace CoreGrid.Api.Features.Assets.Services;

public interface IAssetService
{
    Task<PagedResult<AssetDto>> GetAssetsAsync(
        Guid organizationId,
        DepartmentScope scope,
        AssetQueryParameters parameters,
        CancellationToken cancellationToken);

    Task<AssetDetailDto?> GetAssetByIdAsync(
        Guid organizationId,
        DepartmentScope scope,
        Guid assetId,
        CancellationToken cancellationToken);

    Task<AssetDetailDto> CreateAssetAsync(
        Guid organizationId,
        Guid? userId,
        CreateAssetRequest request,
        CancellationToken cancellationToken);

    Task<AssetDetailDto?> UpdateAssetAsync(
        Guid organizationId,
        Guid assetId,
        Guid? userId,
        UpdateAssetRequest request,
        CancellationToken cancellationToken);

    Task<bool> UpdateConditionAsync(
        Guid organizationId,
        Guid assetId,
        Guid? userId,
        UpdateAssetConditionRequest request,
        CancellationToken cancellationToken);

    Task<AssetDetailDto?> GetAssetByQrCodeAsync(
        Guid organizationId,
        DepartmentScope scope,
        string code,
        CancellationToken cancellationToken);

    Task<string> GetOrganizationCodeAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<PagedResult<AssetHistoryDto>?> GetAssetHistoryAsync(
        Guid organizationId,
        DepartmentScope scope,
        Guid assetId,
        AssetHistoryQueryParameters parameters,
        CancellationToken cancellationToken);
}
