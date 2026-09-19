using System.Text.Json;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Disposals.DTOs;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using CoreGrid.Api.Features.Shared.Scoping;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Disposals.Services;

public class DisposalService : IDisposalService
{
    private readonly CoreGridDbContext _dbContext;
    private readonly IDisposalPreconditionService _preconditionService;

    public DisposalService(CoreGridDbContext dbContext, IDisposalPreconditionService preconditionService)
    {
        _dbContext = dbContext;
        _preconditionService = preconditionService;
    }

    public async Task<CondemnAssetResponse> CondemnAssetAsync(
        Guid organizationId,
        Guid assetId,
        CondemnAssetRequest request,
        Guid condemnedByUserId,
        CancellationToken cancellationToken)
    {
        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.OrganizationId == organizationId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Asset), assetId);

        // FR-049: Condemnation requires a recorded condition of POOR or UNSERVICEABLE.
        var isEligibleCondition = string.Equals(asset.Condition, AssetConditions.Poor, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(asset.Condition, AssetConditions.Unserviceable, StringComparison.OrdinalIgnoreCase);

        if (!isEligibleCondition)
        {
            throw new BusinessRuleException(
                $"Asset cannot be condemned because its condition is '{asset.Condition}'. Condemnation requires condition '{AssetConditions.Poor}' or '{AssetConditions.Unserviceable}'.",
                "condition_not_eligible");
        }

        // Prior status guard: Reject if already CONDEMNED, DISPOSAL_REQUESTED, or terminal DISPOSED, or undergoing active transfer
        if (string.Equals(asset.Status, AssetStatuses.Condemned, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Asset is already condemned.", "already_condemned");
        }

        if (string.Equals(asset.Status, AssetStatuses.DisposalRequested, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Asset already has a pending disposal request.", "disposal_already_requested");
        }

        if (string.Equals(asset.Status, AssetStatuses.Disposed, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Asset is already disposed and cannot be modified.", "already_disposed");
        }

        if (string.Equals(asset.Status, AssetStatuses.TransferRequested, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(asset.Status, AssetStatuses.InTransit, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException($"Asset cannot be condemned while in status '{asset.Status}'.", "invalid_status_transition");
        }

        var previousStatus = asset.Status;
        var now = DateTimeOffset.UtcNow;

        asset.Status = AssetStatuses.Condemned;
        asset.UpdatedAt = now;
        asset.UpdatedBy = condemnedByUserId;

        // Record status change in AssetHistory — B13: EvidenceUrl is now
        // recorded in the payload, not silently dropped.
        _dbContext.AssetHistoryEntries.Add(new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = asset.Id,
            ActorUserId = condemnedByUserId,
            EventType = AssetHistoryEventTypes.StatusChange,
            Description = !string.IsNullOrWhiteSpace(request.Reason) ? $"Asset condemned: {request.Reason.Trim()}" : "Asset condemned.",
            PreviousValue = JsonSerializer.Serialize(new { status = previousStatus }),
            NewValue = JsonSerializer.Serialize(new { status = asset.Status, evidenceUrl = request.EvidenceUrl }),
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CondemnAssetResponse
        {
            AssetId = asset.Id,
            AssetCode = asset.AssetCode,
            Name = asset.Name,
            Status = asset.Status,
            Condition = asset.Condition,
            Reason = request.Reason,
            CondemnedAt = now
        };
    }

    public async Task<DisposalResponse> SubmitDisposalRequestAsync(
        Guid organizationId,
        SubmitDisposalRequest request,
        Guid initiatedByUserId,
        CancellationToken cancellationToken)
    {
        // [Required] on the DTO makes a missing value 400 for a model-bound
        // HTTP caller before this method ever runs.
        var requestedAssetId = request.AssetId!.Value;
        var disposalMethod = request.DisposalMethod!.Value;
        var estimatedResidualValue = request.EstimatedResidualValue!.Value;

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(a => a.Id == requestedAssetId && a.OrganizationId == organizationId, cancellationToken)
            ?? throw new ValidationException(nameof(request.AssetId), $"Asset with ID {requestedAssetId} not found.");

        // FR-050: Guard: Asset.Status must be CONDEMNED
        if (!string.Equals(asset.Status, AssetStatuses.Condemned, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException(
                $"Disposal request can only be raised against a condemned asset. Asset status is '{asset.Status}'.",
                "asset_not_condemned");
        }

        var now = DateTimeOffset.UtcNow;

        var disposalRequest = new DisposalRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = asset.Id,
            InitiatedByUserId = initiatedByUserId,
            DisposalMethod = disposalMethod,
            EstimatedResidualValue = estimatedResidualValue,
            ValuationDate = request.ValuationDate,
            Status = DisposalStatus.PENDING,
            RequestedAt = now,
            Notes = request.Notes
        };

        // FR-050: Transition asset to DISPOSAL_REQUESTED
        asset.Status = AssetStatuses.DisposalRequested;
        asset.UpdatedAt = now;
        asset.UpdatedBy = initiatedByUserId;

        _dbContext.DisposalRequests.Add(disposalRequest);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // §5.6: reuse MapToResponse after a reload instead of hand-building.
        return await LoadResponseAsync(organizationId, disposalRequest.Id, evalResult: null, cancellationToken);
    }

    public async Task<DisposalResponse> ApproveDisposalAsync(
        Guid organizationId,
        Guid disposalRequestId,
        Guid approvingUserId,
        CancellationToken cancellationToken)
    {
        var disposalRequest = await _dbContext.DisposalRequests
            .Include(d => d.Asset)
            .FirstOrDefaultAsync(d => d.Id == disposalRequestId && d.OrganizationId == organizationId, cancellationToken)
            ?? throw NotFoundException.For(nameof(DisposalRequest), disposalRequestId);

        if (disposalRequest.Status != DisposalStatus.PENDING)
        {
            throw new ConflictException(
                $"Cannot approve disposal request in status '{disposalRequest.Status}'. Status must be '{DisposalStatus.PENDING}'.",
                "invalid_status_transition");
        }

        // Evaluate preconditions via DisposalPreconditionService
        var evalResult = await _preconditionService.EvaluateAsync(disposalRequestId, approvingUserId, cancellationToken);

        if (!evalResult.SeparationOfDutiesPassed)
        {
            throw new ForbiddenException(
                evalResult.SeparationOfDutiesFailureReason ?? "Separation of duties violation: Approver cannot be the requester.",
                evalResult);
        }

        if (!evalResult.AllPassed)
        {
            throw new BusinessRuleException("One or more disposal preconditions failed.", "preconditions_failed", evalResult);
        }

        if (disposalRequest.Asset == null)
        {
            throw new InvalidOperationException($"Associated Asset with ID {disposalRequest.AssetId} not found.");
        }

        var now = DateTimeOffset.UtcNow;

        // Atomically transition DisposalRequest to APPROVED and Asset to DISPOSED (FR-051 / FR-054 / FR-055)
        disposalRequest.Status = DisposalStatus.APPROVED;
        disposalRequest.ApprovedByUserId = approvingUserId;
        disposalRequest.ApprovedAt = now;
        disposalRequest.DisposedAt = now;

        var previousStatus = disposalRequest.Asset.Status;
        disposalRequest.Asset.Status = AssetStatuses.Disposed;
        disposalRequest.Asset.UpdatedAt = now;
        disposalRequest.Asset.UpdatedBy = approvingUserId;

        // Record DISPOSAL event in AssetHistory
        _dbContext.AssetHistoryEntries.Add(new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = disposalRequest.Asset.Id,
            ActorUserId = approvingUserId,
            EventType = AssetHistoryEventTypes.Disposal,
            Description = $"Asset disposed via {disposalRequest.DisposalMethod}.",
            PreviousValue = JsonSerializer.Serialize(new { status = previousStatus }),
            NewValue = JsonSerializer.Serialize(new { status = disposalRequest.Asset.Status }),
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        // §5.6: reuse MapToResponse after a reload instead of hand-building.
        return await LoadResponseAsync(organizationId, disposalRequest.Id, evalResult, cancellationToken);
    }

    // FR-053: Return a disposal request for revision with recorded comments without rejecting outright
    public async Task<DisposalResponse> RequestDisposalRevisionAsync(
        Guid organizationId,
        Guid disposalRequestId,
        Guid requestedByUserId,
        string comments,
        CancellationToken cancellationToken)
    {
        // Defensive only — every HTTP caller already fails DTO validation
        // ([Required, MinLength(1)] on RequestDisposalRevisionRequest.Comments)
        // before this method runs; this guards a direct in-process call.
        if (string.IsNullOrWhiteSpace(comments))
        {
            throw new ArgumentException("Revision comments must not be empty.", nameof(comments));
        }

        var disposalRequest = await _dbContext.DisposalRequests
            .Include(d => d.Asset)
            .Include(d => d.InitiatedByUser)
            .FirstOrDefaultAsync(d => d.Id == disposalRequestId && d.OrganizationId == organizationId, cancellationToken)
            ?? throw NotFoundException.For(nameof(DisposalRequest), disposalRequestId);

        if (disposalRequest.Status != DisposalStatus.PENDING)
        {
            throw new ConflictException(
                $"Cannot request revision for disposal request in status '{disposalRequest.Status}'. Status must be '{DisposalStatus.PENDING}'.",
                "invalid_status_transition");
        }

        disposalRequest.Status = DisposalStatus.REVISION_REQUESTED;

        // Preserve any prior notes and append revision comments with timestamp/actor
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
        var revisionEntry = $"[Revision Requested - {timestamp}]: {comments.Trim()}";
        disposalRequest.Notes = string.IsNullOrWhiteSpace(disposalRequest.Notes)
            ? revisionEntry
            : $"{disposalRequest.Notes}\n{revisionEntry}";

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(disposalRequest, null);
    }

    public async Task<PagedResult<DisposalResponse>> GetDisposalRequestsAsync(
        Guid organizationId,
        DepartmentScope scope,
        DisposalQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.DisposalRequests
            .AsNoTracking()
            .Include(d => d.Asset)
            .Include(d => d.InitiatedByUser)
            .Include(d => d.ApprovedByUser)
            .Where(d => d.OrganizationId == organizationId)
            .ApplyScope(scope, d => d.Asset != null ? (Guid?)d.Asset.DepartmentId : null);

        if (parameters.Status.HasValue)
        {
            query = query.Where(d => d.Status == parameters.Status.Value);
        }

        if (parameters.Method.HasValue)
        {
            query = query.Where(d => d.DisposalMethod == parameters.Method.Value);
        }

        var ordered = query.OrderByDescending(d => d.RequestedAt);
        var result = await ordered.ToPagedResultAsync(parameters, d => d, cancellationToken);

        return new PagedResult<DisposalResponse>
        {
            Items = result.Items.Select(d => MapToResponse(d, null)).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages
        };
    }

    public async Task<DisposalResponse?> GetDisposalRequestByIdAsync(
        Guid organizationId,
        DepartmentScope scope,
        Guid disposalRequestId,
        Guid viewingUserId,
        CancellationToken cancellationToken)
    {
        var disposalRequest = await _dbContext.DisposalRequests
            .AsNoTracking()
            .Include(d => d.Asset)
            .Include(d => d.InitiatedByUser)
            .Include(d => d.ApprovedByUser)
            .Where(d => d.Id == disposalRequestId && d.OrganizationId == organizationId)
            .ApplyScope(scope, d => d.Asset != null ? (Guid?)d.Asset.DepartmentId : null)
            .FirstOrDefaultAsync(cancellationToken);

        if (disposalRequest == null) return null;

        // Dynamically evaluate live preconditions for checklist UI if pending
        DisposalPreconditionResult? evalResult = null;
        try
        {
            evalResult = await _preconditionService.EvaluateAsync(disposalRequestId, viewingUserId, cancellationToken);
        }
        catch
        {
            // If evaluation cannot run, leave null
        }

        return MapToResponse(disposalRequest, evalResult);
    }

    private async Task<DisposalResponse> LoadResponseAsync(
        Guid organizationId, Guid disposalRequestId, DisposalPreconditionResult? evalResult, CancellationToken cancellationToken)
    {
        var disposalRequest = await _dbContext.DisposalRequests
            .AsNoTracking()
            .Include(d => d.Asset)
            .Include(d => d.InitiatedByUser)
            .Include(d => d.ApprovedByUser)
            .FirstOrDefaultAsync(d => d.Id == disposalRequestId && d.OrganizationId == organizationId, cancellationToken)
            ?? throw new InvalidOperationException("Disposal request was saved but could not be retrieved.");

        return MapToResponse(disposalRequest, evalResult);
    }

    private static DisposalResponse MapToResponse(DisposalRequest d, DisposalPreconditionResult? evalResult)
    {
        return new DisposalResponse
        {
            Id = d.Id,
            OrganizationId = d.OrganizationId,
            AssetId = d.AssetId,
            AssetCode = d.Asset?.AssetCode ?? string.Empty,
            AssetName = d.Asset?.Name ?? string.Empty,
            AssetCondition = d.Asset?.Condition ?? string.Empty,
            AssetStatus = d.Asset?.Status ?? string.Empty,
            InitiatedByUserId = d.InitiatedByUserId,
            InitiatedByUserEmail = d.InitiatedByUser?.Email,
            ApprovedByUserId = d.ApprovedByUserId,
            ApprovedByUserEmail = d.ApprovedByUser?.Email,
            DisposalMethod = d.DisposalMethod,
            EstimatedResidualValue = d.EstimatedResidualValue,
            ValuationDate = d.ValuationDate,
            Status = d.Status,
            RequestedAt = d.RequestedAt,
            ApprovedAt = d.ApprovedAt,
            DisposedAt = d.DisposedAt,
            Notes = d.Notes,
            PreconditionEvaluation = evalResult
        };
    }
}
