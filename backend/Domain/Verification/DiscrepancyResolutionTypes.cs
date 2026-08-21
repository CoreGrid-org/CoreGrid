namespace CoreGrid.Api.Domain;

// FR-062's five resolution types. Discrepancy.ResolutionType stays a plain
// string column (matches ResolveDiscrepancyRequest already being free-text
// from the API's point of view), but DiscrepancyService validates every
// incoming value against this set.
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

    // FR-062's acceptance criteria: NO_ACTION requires a justification of at
    // least 20 characters (longer than the general "explanation required"
    // floor every resolution type shares).
    public const int NoActionMinimumJustificationLength = 20;
}
