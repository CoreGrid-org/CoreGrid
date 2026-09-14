using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreGrid.Api.Features.Transfers.DTOs;

namespace CoreGrid.Api.Features.Transfers.Services;

public interface ITransferService
{
    Task<TransferResponse> InitiateTransferAsync(Guid organizationId, InitiateTransferRequest request, Guid initiatedByUserId, CancellationToken cancellationToken = default);

    Task<TransferResponse> ApproveTransferAsync(Guid organizationId, Guid transferId, Guid approvedByUserId, CancellationToken cancellationToken = default);

    Task<TransferResponse> ConfirmReceiptAsync(Guid organizationId, Guid transferId, Guid confirmedByUserId, CancellationToken cancellationToken = default);

    Task<List<TransferResponse>> GetTransfersAsync(Guid organizationId, TransferQueryParameters parameters, CancellationToken cancellationToken = default);

    Task<TransferResponse?> GetTransferByIdAsync(Guid organizationId, Guid transferId, CancellationToken cancellationToken = default);
}
