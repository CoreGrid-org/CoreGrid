namespace CoreGrid.Api.Domain;

// Asset.Status values (constrained by CK_Assets_Status). Split out of the
// former AssetStatusConstants (Phase 4, §6.1) so status and condition —
// two different columns with two different constraints — aren't declared
// in the same class.
public static class AssetStatuses
{
    public const string Active = "ACTIVE";
    public const string UnderMaintenance = "UNDER_MAINTENANCE";
    public const string TransferRequested = "TRANSFER_REQUESTED";
    public const string InTransit = "IN_TRANSIT";
    public const string Condemned = "CONDEMNED";
    public const string DisposalRequested = "DISPOSAL_REQUESTED";
    public const string Disposed = "DISPOSED";

    public static readonly string[] All =
        [Active, UnderMaintenance, TransferRequested, InTransit, Condemned, DisposalRequested, Disposed];
}
