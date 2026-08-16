namespace CoreGrid.Api.Features.Assets.Helpers;

public static class AssetCodeGenerator
{
    // e.g. "MOTAHSL" + "RCE" + "PAVE" + 1 -> "MOTAHSL-RCE-PAVE-0001".
    public static string Generate(
        string organizationCode,
        string categoryCode,
        string assetTypeCode,
        int sequence)
    {
        return $"{organizationCode}-{categoryCode}-{assetTypeCode}-{sequence:D4}";
    }
}