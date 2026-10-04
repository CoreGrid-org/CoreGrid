namespace CoreGrid.Api.Features.AgentTools.DTOs;

public sealed record AssetSelection(Guid OrganizationId, Guid? AssetTypeId, Guid? AssetId, bool IncludeDisposed)
{
    public static AssetSelection Fleet(Guid organizationId, Guid assetTypeId, Guid? assetId = null) =>
        new(organizationId, assetTypeId, assetId, IncludeDisposed: false);

    public static AssetSelection Single(Guid organizationId, Guid assetId) =>
        new(organizationId, AssetTypeId: null, assetId, IncludeDisposed: true);
}
