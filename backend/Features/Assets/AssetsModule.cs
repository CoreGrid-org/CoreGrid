using CoreGrid.Api.Features.Assets.Services;

namespace CoreGrid.Api.Features.Assets;

public static class AssetsModule
{
    public static IServiceCollection AddAssetsFeature(this IServiceCollection services)
    {
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAssetTypeService, AssetTypeService>();
        services.AddScoped<IAssetCategoryService, AssetCategoryService>();

        return services;
    }
}
