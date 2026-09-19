namespace CoreGrid.Api.Domain;

// Asset.Condition values (constrained by CK_Assets_Condition). Split out of
// the former AssetStatusConstants (Phase 4, §6.1). Order (NEW to
// UNSERVICEABLE) matches the dashboard's condition breakdown and every
// duplicated ValidConditions/ConditionOrder array the codebase already used.
public static class AssetConditions
{
    public const string New = "NEW";
    public const string Good = "GOOD";
    public const string Fair = "FAIR";
    public const string Poor = "POOR";
    public const string Unserviceable = "UNSERVICEABLE";

    public static readonly string[] All = [New, Good, Fair, Poor, Unserviceable];
}
