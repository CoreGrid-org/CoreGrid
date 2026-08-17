using CoreGrid.Api.Features.Assets.DTOs;

namespace CoreGrid.Api.Features.Assets.Services;

public interface IAssetCategoryService
{
    Task<List<AssetCategoryDto>> GetCategoriesAsync(Guid organizationId);

    Task<AssetCategoryDto?> GetCategoryByIdAsync(
        Guid organizationId,
        Guid categoryId);

    Task<AssetCategoryDto> CreateCategoryAsync(
        Guid organizationId,
        Guid? userId,
        CreateAssetCategoryRequest request);

    Task<AssetCategoryDto?> UpdateCategoryAsync(
        Guid organizationId,
        Guid categoryId,
        Guid? userId,
        UpdateAssetCategoryRequest request);

    Task<(bool Found, bool HardDeleted, AssetCategoryDto? Category)> DeleteCategoryAsync(
        Guid organizationId,
        Guid categoryId,
        Guid? userId);

    Task<AssetCategoryDto?> SetCategoryActiveAsync(
        Guid organizationId,
        Guid categoryId,
        Guid? userId,
        bool isActive);
}