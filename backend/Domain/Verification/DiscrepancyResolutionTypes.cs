namespace CoreGrid.Api.Domain;

// Defines the supported discrepancy resolution types.
public static class DiscrepancyResolutionTypes
{
    public const string RegisterCorrected = "REGISTER_CORRECTED";
    public const string AssetRelocated = "ASSET_RELOCATED";
    public const string ConditionUpdated = "CONDITION_UPDATED";
    public const string WrittenOff = "WRITTEN_OFF";
    public const string NoAction = "NO_ACTION";

    public static readonly string[] All =
    [
        RegisterCorrected, AssetRelocated, ConditionUpdated, WrittenOff, NoAction
    ];

// Minimum justification length required for the NoAction resolution.
    public const int NoActionMinimumJustificationLength = 20;
}
