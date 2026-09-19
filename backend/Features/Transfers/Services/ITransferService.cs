using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Paging;
using CoreGrid.Api.Features.Shared.Scoping;
using CoreGrid.Api.Features.Transfers.DTOs;

namespace CoreGrid.Api.Features.Transfers.Services;

public interface ITransferService
{
    Task<TransferResponse> InitiateTransferAsync(Guid organizationId, InitiateTransferRequest request, Guid initiatedByUserId, CancellationToken cancellationToken);

    Task<TransferResponse> ApproveTransferAsync(Guid organizationId, Guid transferId, Guid approvedByUserId, CancellationToken cancellationToken);

    // SRS §9.4 / FR-045: reverts the asset to ACTIVE — a rejected transfer
    // never left the requesting department, so there is nothing to undo
    // beyond releasing the TRANSFER_REQUESTED hold.
    Task<TransferResponse> RejectTransferAsync(Guid organizationId, Guid transferId, Guid rejectedByUserId, RejectTransferRequest request, CancellationToken cancellationToken);

    // Appendix B: an InventoryOfficer confirming receipt must belong to
    // the transfer's destination department; Administrator is exempt
    // (plan §4.4's documented deviation).
    Task<TransferResponse> ConfirmReceiptAsync(Guid organizationId, Guid transferId, Guid confirmedByUserId, CoreGridRole callerRole, Guid? callerDepartmentId, CancellationToken cancellationToken);

    Task<PagedResult<TransferResponse>> GetTransfersAsync(Guid organizationId, DepartmentScope scope, TransferQueryParameters parameters, CancellationToken cancellationToken);

    Task<TransferResponse?> GetTransferByIdAsync(Guid organizationId, DepartmentScope scope, Guid transferId, CancellationToken cancellationToken);

    Task<PagedResult<TransferResponse>> GetTransferHistoryForAssetAsync(Guid organizationId, DepartmentScope scope, Guid assetId, PagedQuery query, CancellationToken cancellationToken);
}
