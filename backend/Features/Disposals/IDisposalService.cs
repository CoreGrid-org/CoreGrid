using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Disposals.DTOs;

namespace CoreGrid.Api.Features.Disposals;

public interface IDisposalService
{
    Task<CondemnAssetResponse> CondemnAssetAsync(Guid organizationId, Guid assetId, CondemnAssetRequest request, Guid condemnedByUserId, CancellationToken cancellationToken = default);

    Task<DisposalResponse> SubmitDisposalRequestAsync(Guid organizationId, SubmitDisposalRequest request, Guid initiatedByUserId, CancellationToken cancellationToken = default);

    Task<DisposalApprovalResult> ApproveDisposalAsync(Guid organizationId, Guid disposalRequestId, Guid approvingUserId, CancellationToken cancellationToken = default);

    Task<List<DisposalResponse>> GetDisposalRequestsAsync(Guid organizationId, DisposalQueryParameters parameters, CancellationToken cancellationToken = default);

    Task<DisposalResponse?> GetDisposalRequestByIdAsync(Guid organizationId, Guid disposalRequestId, Guid viewingUserId, CancellationToken cancellationToken = default);
}

public class DisposalApprovalResult
{
    public bool Success { get; set; }
    public bool IsForbidden { get; set; }
    public string? ForbiddenReason { get; set; }
    public bool IsInvalidState { get; set; }
    public string? InvalidStateReason { get; set; }
    public DisposalPreconditionResult? PreconditionResult { get; set; }
    public DisposalResponse? DisposalResponse { get; set; }
}
