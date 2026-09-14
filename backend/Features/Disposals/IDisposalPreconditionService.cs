using System;
using System.Threading;
using System.Threading.Tasks;
using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Disposals;

public interface IDisposalPreconditionService
{
    Task<DisposalPreconditionResult> EvaluateAsync(Guid disposalRequestId, Guid approvingUserId, CancellationToken cancellationToken = default);

    PreconditionCheck CheckP1AssetCondemned(Asset asset);
    PreconditionCheck CheckP2ValuationRecorded(DisposalRequest request, Asset asset);
    PreconditionCheck CheckP3ServiceLifeElapsed(Asset asset, OrganizationPolicy? policy, AssetType? assetType, DateOnly? evaluationDate = null);
    Task<PreconditionCheck> CheckP4NoOpenMaintenanceAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<PreconditionCheck> CheckP5NoOpenTransfersAsync(Guid assetId, CancellationToken cancellationToken = default);
    PreconditionCheck CheckP6AgentWorkflowPass(DisposalRequest request);
    (bool Passed, string? Reason) CheckSeparationOfDuties(DisposalRequest request, Guid approvingUserId);
}
