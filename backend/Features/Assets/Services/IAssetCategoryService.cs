using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Paging;

namespace CoreGrid.Api.Features.Assets.Services;

public interface IAssetCategoryService
{
    Task<PagedResult<AssetCategoryDto>> GetCategoriesAsync(
        Guid organizationId,
        PagedQuery query,
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<AssetCategoryDto?> GetCategoryByIdAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken);

    Task<AssetCategoryDto> CreateCategoryAsync(
        Guid organizationId,
        Guid? userId,
        CreateAssetCategoryRequest request,
        CancellationToken cancellationToken);

    Task<AssetCategoryDto?> UpdateCategoryAsync(
        Guid organizationId,
        Guid categoryId,
        Guid? userId,
        UpdateAssetCategoryRequest request,
        CancellationToken cancellationToken);

    Task<(bool Found, bool HardDeleted, AssetCategoryDto? Category)> DeleteCategoryAsync(
        Guid organizationId,
        Guid categoryId,
        Guid? userId,
        CancellationToken cancellationToken);

    Task<AssetCategoryDto?> SetCategoryActiveAsync(
        Guid organizationId,
        Guid categoryId,
        Guid? userId,
        bool isActive,
        CancellationToken cancellationToken);
}
