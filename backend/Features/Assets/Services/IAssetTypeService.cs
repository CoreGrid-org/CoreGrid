using CoreGrid.Api.Features.Assets.DTOs;

namespace CoreGrid.Api.Features.Assets.Services;

public interface IAssetTypeService
{
    Task<List<AssetTypeDto>> GetAssetTypesAsync(Guid organizationId);

    Task<AssetTypeDto?> GetAssetTypeByIdAsync(
        Guid organizationId,
        Guid assetTypeId);

    Task<List<AssetAttributeDefinitionDto>> GetAttributeDefinitionsAsync(
        Guid organizationId,
        Guid assetTypeId);

    Task<AssetTypeDto> CreateAssetTypeAsync(
        Guid organizationId,
        Guid? userId,
        CreateAssetTypeRequest request);

    Task<AssetTypeDto?> UpdateAssetTypeAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId,
        UpdateAssetTypeRequest request);

    Task<AssetAttributeDefinitionDto?> CreateAttributeDefinitionAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId,
        CreateAssetAttributeDefinitionRequest request);

    Task<AssetAttributeDefinitionDto?> UpdateAttributeDefinitionAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid attributeId,
        Guid? userId,
        UpdateAssetAttributeDefinitionRequest request);

    Task<(bool Found, bool HardDeleted, AssetTypeDto? AssetType)> DeleteAssetTypeAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId);

    Task<AssetTypeDto?> SetAssetTypeActiveAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId,
        bool isActive);

    Task<(bool Found, bool HardDeleted, AssetAttributeDefinitionDto? Definition)> DeleteAttributeDefinitionAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid attributeId,
        Guid? userId);

    Task<AssetAttributeDefinitionDto?> SetAttributeDefinitionActiveAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid attributeId,
        Guid? userId,
        bool isActive);
}