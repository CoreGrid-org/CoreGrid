namespace CoreGrid.Api.Features.Assets.Helpers;

public static class AssetCodeGenerator
{
    public static string Generate(
        string organizationCode,
        string assetTypeCode,
        int sequence)
    {
        return $"{organizationCode}-{assetTypeCode}-{sequence:D4}";
    }
}