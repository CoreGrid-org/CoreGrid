using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Paging;

namespace CoreGrid.Api.Features.Assets.Services;

public interface IAssetTypeService
{
    Task<PagedResult<AssetTypeDto>> GetAssetTypesAsync(
        Guid organizationId,
        PagedQuery query,
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<AssetTypeDto?> GetAssetTypeByIdAsync(
        Guid organizationId,
        Guid assetTypeId,
        CancellationToken cancellationToken);

    // Deliberately unpaginated — a bounded sub-resource the dynamic asset
    // form needs in full (plan §12.3), unlike every other list here.
    Task<List<AssetAttributeDefinitionDto>> GetAttributeDefinitionsAsync(
        Guid organizationId,
        Guid assetTypeId,
        CancellationToken cancellationToken);

    Task<AssetTypeDto> CreateAssetTypeAsync(
        Guid organizationId,
        Guid? userId,
        CreateAssetTypeRequest request,
        CancellationToken cancellationToken);

    Task<AssetTypeDto?> UpdateAssetTypeAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId,
        UpdateAssetTypeRequest request,
        CancellationToken cancellationToken);

    Task<AssetAttributeDefinitionDto?> CreateAttributeDefinitionAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId,
        CreateAssetAttributeDefinitionRequest request,
        CancellationToken cancellationToken);

    Task<AssetAttributeDefinitionDto?> UpdateAttributeDefinitionAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid attributeId,
        Guid? userId,
        UpdateAssetAttributeDefinitionRequest request,
        CancellationToken cancellationToken);

    Task<(bool Found, bool HardDeleted, AssetTypeDto? AssetType)> DeleteAssetTypeAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId,
        CancellationToken cancellationToken);

    Task<AssetTypeDto?> SetAssetTypeActiveAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId,
        bool isActive,
        CancellationToken cancellationToken);

    Task<(bool Found, bool HardDeleted, AssetAttributeDefinitionDto? Definition)> DeleteAttributeDefinitionAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid attributeId,
        Guid? userId,
        CancellationToken cancellationToken);

    Task<AssetAttributeDefinitionDto?> SetAttributeDefinitionActiveAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid attributeId,
        Guid? userId,
        bool isActive,
        CancellationToken cancellationToken);
}
