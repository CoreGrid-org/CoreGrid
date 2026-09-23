namespace CoreGrid.Api.Domain;

// Defines the supported asset statuses.
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
