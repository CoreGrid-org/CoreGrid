namespace CoreGrid.Api.Domain;

// Defines the supported asset conditions.
public static class AssetConditions
{
    public const string New = "NEW";
    public const string Good = "GOOD";
    public const string Fair = "FAIR";
    public const string Poor = "POOR";
    public const string Unserviceable = "UNSERVICEABLE";

    public static readonly string[] All = [New, Good, Fair, Poor, Unserviceable];
}
