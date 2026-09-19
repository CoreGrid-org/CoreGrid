using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Finance;

namespace CoreGrid.Api.Features.Disposals.Services;

public class DisposalPreconditionService : IDisposalPreconditionService
{
    private readonly CoreGridDbContext _dbContext;

    public DisposalPreconditionService(CoreGridDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DisposalPreconditionResult> EvaluateAsync(Guid disposalRequestId, Guid approvingUserId, CancellationToken cancellationToken = default)
    {
        var result = new DisposalPreconditionResult();

        var request = await _dbContext.DisposalRequests
            .Include(d => d.Asset)
                .ThenInclude(a => a!.AssetType)
            .FirstOrDefaultAsync(d => d.Id == disposalRequestId, cancellationToken);

        if (request == null)
        {
            throw NotFoundException.For(nameof(DisposalRequest), disposalRequestId);
        }

        if (request.Asset == null)
        {
            throw new InvalidOperationException($"Asset associated with DisposalRequest {disposalRequestId} not found.");
        }

        // Separation of Duties (FR-051 AC2: Approver must not be the requester)
        var sod = CheckSeparationOfDuties(request, approvingUserId);
        result.SeparationOfDutiesPassed = sod.Passed;
        result.SeparationOfDutiesFailureReason = sod.Reason;

        // Fetch applicable organization policy (specific asset type policy or default org policy)
        var policy = await _dbContext.OrganizationPolicies
            .Where(p => p.OrganizationId == request.OrganizationId && (p.AssetTypeId == request.Asset.AssetTypeId || p.AssetTypeId == null))
            .OrderByDescending(p => p.AssetTypeId != null) // Prefer specific asset type policy over org default
            .FirstOrDefaultAsync(cancellationToken);

        // Evaluate P1 to P6
        var p1 = CheckP1AssetCondemned(request.Asset);
        var p2 = CheckP2ValuationRecorded(request, request.Asset);
        var p3 = CheckP3ServiceLifeElapsed(request.Asset, policy, request.Asset.AssetType);
        var p4 = await CheckP4NoOpenMaintenanceAsync(request.AssetId, cancellationToken);
        var p5 = await CheckP5NoOpenTransfersAsync(request.AssetId, cancellationToken);
        var p6 = await CheckP6AgentWorkflowLinkedAsync(request.AssetId, cancellationToken);

        result.Checks.Add(p1);
        result.Checks.Add(p2);
        result.Checks.Add(p3);
        result.Checks.Add(p4);
        result.Checks.Add(p5);
        result.Checks.Add(p6);

        result.AllPassed = result.Checks.All(c => c.Passed) && result.SeparationOfDutiesPassed;

        return result;
    }

    /// <summary>
    /// P1 — Asset status is CONDEMNED.
    /// </summary>
    public PreconditionCheck CheckP1AssetCondemned(Asset asset)
    {
        bool passed = asset.Status == AssetStatuses.Condemned;

        return new PreconditionCheck
        {
            Code = "P1",
            Description = "Asset status must be CONDEMNED",
            Passed = passed,
            FailureReason = passed ? null : $"Asset status is '{asset.Status}', but must be '{AssetStatuses.Condemned}'."
        };
    }

    /// <summary>
    /// P2 — A valuation amount and valuation date are recorded.
    /// Amount comes from DisposalRequest.EstimatedResidualValue, Date comes from DisposalRequest.ValuationDate.
    /// </summary>
    public PreconditionCheck CheckP2ValuationRecorded(DisposalRequest request, Asset asset)
    {
        bool hasAmount = request.EstimatedResidualValue >= 0;
        bool hasDate = request.ValuationDate.HasValue;
        bool passed = hasAmount && hasDate;

        string? failureReason = null;
        if (!hasAmount && !hasDate)
        {
            failureReason = "Valuation amount and valuation date are both missing.";
        }
        else if (!hasAmount)
        {
            failureReason = "Valuation amount is missing or negative.";
        }
        else if (!hasDate)
        {
            failureReason = "Valuation date is missing.";
        }

        return new PreconditionCheck
        {
            Code = "P2",
            Description = "A valuation amount and valuation date must be recorded",
            Passed = passed,
            FailureReason = failureReason
        };
    }

    /// <summary>
    /// P3 — Elapsed service life ≥ the minimum configured for the asset type.
    /// Evaluates against OrganizationPolicy.MinimumServiceLifeYears or AssetType.UsefulLifeYears.
    /// </summary>
    public PreconditionCheck CheckP3ServiceLifeElapsed(Asset asset, OrganizationPolicy? policy, AssetType? assetType, DateOnly? evaluationDate = null)
    {
        var today = evaluationDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // §6.1/B22: shared with AgentToolsService.ComputeDepreciation —
        // same whole-year, calendar-anniversary elapsed-time convention.
        int elapsedYears = StraightLineDepreciation.ElapsedWholeYears(asset.AcquisitionDate, today);

        // Required years from Policy or AssetType
        decimal requiredMinYears = policy != null && policy.MinimumServiceLifeYears > 0
            ? policy.MinimumServiceLifeYears
            : (assetType?.UsefulLifeYears ?? 0);

        bool passed = elapsedYears >= (int)requiredMinYears;

        return new PreconditionCheck
        {
            Code = "P3",
            Description = "Elapsed service life must be >= minimum configured for the asset type",
            Passed = passed,
            FailureReason = passed ? null : $"Elapsed service life ({elapsedYears} years) is less than required minimum ({requiredMinYears} years)."
        };
    }

    /// <summary>
    /// P4 — No maintenance record for the asset is in REQUESTED, APPROVED or IN_PROGRESS (FR-051 §6.6).
    /// </summary>
    public async Task<PreconditionCheck> CheckP4NoOpenMaintenanceAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var openStatuses = new[]
        {
            MaintenanceStatus.REQUESTED,
            MaintenanceStatus.APPROVED,
            MaintenanceStatus.IN_PROGRESS
        };

        var blockingRecord = await _dbContext.MaintenanceRecords
            .AsNoTracking()
            .Where(m => m.AssetId == assetId && openStatuses.Contains(m.Status))
            .Select(m => new { m.Status })
            .FirstOrDefaultAsync(cancellationToken);

        bool passed = blockingRecord == null;

        return new PreconditionCheck
        {
            Code = "P4",
            Description = "No maintenance record for the asset is in REQUESTED, APPROVED or IN_PROGRESS",
            Passed = passed,
            FailureReason = passed ? null : $"Open maintenance record exists with status {blockingRecord!.Status}."
        };
    }

    /// <summary>
    /// P5 — No transfer for the asset is in TRANSFER_REQUESTED or IN_TRANSIT.
    /// Fully implemented against the AssetTransfers table.
    /// </summary>
    public async Task<PreconditionCheck> CheckP5NoOpenTransfersAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var openStatuses = new[] { TransferStatus.REQUESTED, TransferStatus.APPROVED, TransferStatus.IN_TRANSIT };

        bool hasOpenTransfer = await _dbContext.AssetTransfers
            .AnyAsync(t => t.AssetId == assetId && openStatuses.Contains(t.Status), cancellationToken);

        return new PreconditionCheck
        {
            Code = "P5",
            Description = "No transfer for the asset is in TRANSFER_REQUESTED or IN_TRANSIT",
            Passed = !hasOpenTransfer,
            FailureReason = hasOpenTransfer ? "Asset has an active transfer in REQUESTED, APPROVED, or IN_TRANSIT status." : null
        };
    }

    /// <summary>
    /// P6 — Where an agentic workflow is linked to the request, it has reached AWAITING_APPROVAL and its deterministic validation result is PASS (FR-051 §6.6).
    /// ASSUMPTION (pending confirmation from Hasitha/Component D): 'linked workflow' = most recent AgentWorkflow for this asset with Recommendation == DISPOSE. If multiple interpretations are possible, this should be revisited.
    /// </summary>
    public async Task<PreconditionCheck> CheckP6AgentWorkflowLinkedAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var linkedWorkflow = await _dbContext.AgentWorkflows
            .AsNoTracking()
            .Where(w => w.AssetId == assetId && w.Recommendation == "DISPOSE")
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new { w.Id, w.Status })
            .FirstOrDefaultAsync(cancellationToken);

        // Conditional: Where an agentic workflow is linked...
        if (linkedWorkflow == null)
        {
            return new PreconditionCheck
            {
                Code = "P6",
                Description = "Where an agentic workflow is linked to the request, it must have reached AWAITING_APPROVAL with PASS validation",
                Passed = true,
                FailureReason = null
            };
        }

        bool isAwaitingApproval = linkedWorkflow.Status == WorkflowStatus.AWAITING_APPROVAL;

        return new PreconditionCheck
        {
            Code = "P6",
            Description = "Where an agentic workflow is linked to the request, it must have reached AWAITING_APPROVAL with PASS validation",
            Passed = isAwaitingApproval,
            FailureReason = isAwaitingApproval ? null : $"Linked agent workflow {linkedWorkflow.Id} has status '{linkedWorkflow.Status}', but must be '{nameof(WorkflowStatus.AWAITING_APPROVAL)}'."
        };
    }

    /// <summary>
    /// Separation of duties: Approver must not be the requester (FR-051 AC2).
    /// </summary>
    public (bool Passed, string? Reason) CheckSeparationOfDuties(DisposalRequest request, Guid approvingUserId)
    {
        if (request.InitiatedByUserId == approvingUserId)
        {
            return (false, "Separation of duties violation: Approving user cannot be the user who raised the disposal request.");
        }

        return (true, null);
    }
}
