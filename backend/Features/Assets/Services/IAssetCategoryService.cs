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
}