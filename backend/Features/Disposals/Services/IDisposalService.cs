using CoreGrid.Api.Features.Disposals.DTOs;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Scoping;

namespace CoreGrid.Api.Features.Disposals.Services;

public interface IDisposalService
{
    Task<CondemnAssetResponse> CondemnAssetAsync(Guid organizationId, Guid assetId, CondemnAssetRequest request, Guid condemnedByUserId, CancellationToken cancellationToken);

    Task<DisposalResponse> SubmitDisposalRequestAsync(Guid organizationId, SubmitDisposalRequest request, Guid initiatedByUserId, CancellationToken cancellationToken);

    // §5.6: DisposalApprovalResult (a hand-rolled result union) is gone —
    // separation-of-duties throws ForbiddenException (with the precondition
    // snapshot as its payload), an invalid state throws ConflictException,
    // and failed preconditions throw BusinessRuleException (payload too).
    Task<DisposalResponse> ApproveDisposalAsync(Guid organizationId, Guid disposalRequestId, Guid approvingUserId, CancellationToken cancellationToken);

    Task<DisposalResponse> RequestDisposalRevisionAsync(Guid organizationId, Guid disposalRequestId, Guid requestedByUserId, string comments, CancellationToken cancellationToken);

    // SRS §9.4: terminal — reverts the asset to CONDEMNED (the condemnation
    // itself isn't in question, only this particular disposal request), so
    // a new disposal request can be raised against it later.
    Task<DisposalResponse> RejectDisposalAsync(Guid organizationId, Guid disposalRequestId, Guid rejectedByUserId, RejectDisposalRequest request, CancellationToken cancellationToken);

    Task<PagedResult<DisposalResponse>> GetDisposalRequestsAsync(Guid organizationId, DepartmentScope scope, DisposalQueryParameters parameters, CancellationToken cancellationToken);

    Task<DisposalResponse?> GetDisposalRequestByIdAsync(Guid organizationId, DepartmentScope scope, Guid disposalRequestId, Guid viewingUserId, CancellationToken cancellationToken);
}
