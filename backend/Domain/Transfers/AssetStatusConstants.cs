namespace CoreGrid.Api.Domain;

public static class AssetStatusConstants
{
    // Asset.Status values (constrained by CK_Assets_Status)
    public const string Active = "ACTIVE";
    public const string UnderMaintenance = "UNDER_MAINTENANCE";
    public const string TransferRequested = "TRANSFER_REQUESTED";
    public const string InTransit = "IN_TRANSIT";
    public const string Condemned = "CONDEMNED";
    public const string DisposalRequested = "DISPOSAL_REQUESTED";
    public const string Disposed = "DISPOSED";

    // Asset.Condition values (constrained by CK_Assets_Condition)
    public const string ConditionNew = "NEW";
    public const string ConditionGood = "GOOD";
    public const string ConditionFair = "FAIR";
    public const string ConditionPoor = "POOR";
    public const string ConditionUnserviceable = "UNSERVICEABLE";
}
