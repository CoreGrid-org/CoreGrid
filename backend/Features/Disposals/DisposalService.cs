using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Disposals.DTOs;

namespace CoreGrid.Api.Features.Disposals;

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
        CancellationToken cancellationToken = default)
    {
        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.OrganizationId == organizationId, cancellationToken);

        if (asset == null)
        {
            throw new KeyNotFoundException($"Asset with ID {assetId} not found.");
        }

        // FR-049: Condemnation requires a recorded condition of POOR or UNSERVICEABLE.
        var isEligibleCondition = string.Equals(asset.Condition, AssetStatusConstants.ConditionPoor, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(asset.Condition, AssetStatusConstants.ConditionUnserviceable, StringComparison.OrdinalIgnoreCase);

        if (!isEligibleCondition)
        {
            throw new InvalidOperationException($"Asset cannot be condemned because its condition is '{asset.Condition}'. Condemnation requires condition '{AssetStatusConstants.ConditionPoor}' or '{AssetStatusConstants.ConditionUnserviceable}'.");
        }

        // Prior status guard: Reject if already CONDEMNED, DISPOSAL_REQUESTED, or terminal DISPOSED, or undergoing active transfer
        if (string.Equals(asset.Status, AssetStatusConstants.Condemned, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Asset is already condemned.");
        }

        if (string.Equals(asset.Status, AssetStatusConstants.DisposalRequested, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Asset already has a pending disposal request.");
        }

        if (string.Equals(asset.Status, AssetStatusConstants.Disposed, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Asset is already disposed and cannot be modified.");
        }

        if (string.Equals(asset.Status, AssetStatusConstants.TransferRequested, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(asset.Status, AssetStatusConstants.InTransit, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Asset cannot be condemned while in status '{asset.Status}'.");
        }

        var previousStatus = asset.Status;
        var now = DateTimeOffset.UtcNow;

        asset.Status = AssetStatusConstants.Condemned;
        asset.UpdatedAt = now;
        asset.UpdatedBy = condemnedByUserId;

        // Record status change in AssetHistory
        _dbContext.AssetHistoryEntries.Add(new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = asset.Id,
            ActorUserId = condemnedByUserId,
            EventType = "STATUS_CHANGE",
            Description = !string.IsNullOrWhiteSpace(request.Reason) ? $"Asset condemned: {request.Reason.Trim()}" : "Asset condemned.",
            PreviousValue = JsonSerializer.Serialize(new { status = previousStatus }),
            NewValue = JsonSerializer.Serialize(new { status = asset.Status }),
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
        CancellationToken cancellationToken = default)
    {
        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.OrganizationId == organizationId, cancellationToken);

        if (asset == null)
        {
            throw new KeyNotFoundException($"Asset with ID {request.AssetId} not found.");
        }

        // FR-050: Guard: Asset.Status must be CONDEMNED
        if (!string.Equals(asset.Status, AssetStatusConstants.Condemned, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Disposal request can only be raised against a condemned asset. Asset status is '{asset.Status}'.");
        }

        var now = DateTimeOffset.UtcNow;

        var disposalRequest = new DisposalRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = asset.Id,
            InitiatedByUserId = initiatedByUserId,
            DisposalMethod = request.DisposalMethod,
            EstimatedResidualValue = request.EstimatedResidualValue,
            ValuationDate = request.ValuationDate,
            Status = DisposalStatus.PENDING,
            RequestedAt = now,
            Notes = request.Notes
        };

        // FR-050: Transition asset to DISPOSAL_REQUESTED
        var previousStatus = asset.Status;
        asset.Status = AssetStatusConstants.DisposalRequested;
        asset.UpdatedAt = now;
        asset.UpdatedBy = initiatedByUserId;

        _dbContext.DisposalRequests.Add(disposalRequest);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var initEmail = await _dbContext.Users.Where(u => u.Id == initiatedByUserId).Select(u => u.Email).FirstOrDefaultAsync(cancellationToken);

        return new DisposalResponse
        {
            Id = disposalRequest.Id,
            OrganizationId = disposalRequest.OrganizationId,
            AssetId = asset.Id,
            AssetCode = asset.AssetCode,
            AssetName = asset.Name,
            AssetCondition = asset.Condition,
            AssetStatus = asset.Status,
            InitiatedByUserId = initiatedByUserId,
            InitiatedByUserEmail = initEmail,
            ApprovedByUserId = null,
            ApprovedByUserEmail = null,
            DisposalMethod = disposalRequest.DisposalMethod,
            EstimatedResidualValue = disposalRequest.EstimatedResidualValue,
            ValuationDate = disposalRequest.ValuationDate,
            Status = disposalRequest.Status,
            RequestedAt = disposalRequest.RequestedAt,
            ApprovedAt = disposalRequest.ApprovedAt,
            DisposedAt = disposalRequest.DisposedAt,
            Notes = disposalRequest.Notes,
            PreconditionEvaluation = null
        };
    }

    public async Task<DisposalApprovalResult> ApproveDisposalAsync(
        Guid organizationId,
        Guid disposalRequestId,
        Guid approvingUserId,
        CancellationToken cancellationToken = default)
    {
        var disposalRequest = await _dbContext.DisposalRequests
            .Include(d => d.Asset)
            .FirstOrDefaultAsync(d => d.Id == disposalRequestId && d.OrganizationId == organizationId, cancellationToken);

        if (disposalRequest == null)
        {
            throw new KeyNotFoundException($"DisposalRequest with ID {disposalRequestId} not found.");
        }

        if (disposalRequest.Status != DisposalStatus.PENDING)
        {
            return new DisposalApprovalResult
            {
                Success = false,
                IsInvalidState = true,
                InvalidStateReason = $"Cannot approve disposal request in status '{disposalRequest.Status}'. Status must be '{DisposalStatus.PENDING}'."
            };
        }

        // Evaluate preconditions via DisposalPreconditionService
        var evalResult = await _preconditionService.EvaluateAsync(disposalRequestId, approvingUserId, cancellationToken);

        if (!evalResult.SeparationOfDutiesPassed)
        {
            return new DisposalApprovalResult
            {
                Success = false,
                IsForbidden = true,
                ForbiddenReason = evalResult.SeparationOfDutiesFailureReason ?? "Separation of duties violation: Approver cannot be the requester.",
                PreconditionResult = evalResult
            };
        }

        if (!evalResult.AllPassed)
        {
            return new DisposalApprovalResult
            {
                Success = false,
                PreconditionResult = evalResult
            };
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
        disposalRequest.Asset.Status = AssetStatusConstants.Disposed;
        disposalRequest.Asset.UpdatedAt = now;
        disposalRequest.Asset.UpdatedBy = approvingUserId;

        // Record DISPOSAL event in AssetHistory
        _dbContext.AssetHistoryEntries.Add(new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = disposalRequest.Asset.Id,
            ActorUserId = approvingUserId,
            EventType = "DISPOSAL",
            Description = $"Asset disposed via {disposalRequest.DisposalMethod}.",
            PreviousValue = JsonSerializer.Serialize(new { status = previousStatus }),
            NewValue = JsonSerializer.Serialize(new { status = disposalRequest.Asset.Status }),
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var initEmail = await _dbContext.Users.Where(u => u.Id == disposalRequest.InitiatedByUserId).Select(u => u.Email).FirstOrDefaultAsync(cancellationToken);
        var apprEmail = await _dbContext.Users.Where(u => u.Id == approvingUserId).Select(u => u.Email).FirstOrDefaultAsync(cancellationToken);

        var response = new DisposalResponse
        {
            Id = disposalRequest.Id,
            OrganizationId = disposalRequest.OrganizationId,
            AssetId = disposalRequest.Asset.Id,
            AssetCode = disposalRequest.Asset.AssetCode,
            AssetName = disposalRequest.Asset.Name,
            AssetCondition = disposalRequest.Asset.Condition,
            AssetStatus = disposalRequest.Asset.Status,
            InitiatedByUserId = disposalRequest.InitiatedByUserId,
            InitiatedByUserEmail = initEmail,
            ApprovedByUserId = disposalRequest.ApprovedByUserId,
            ApprovedByUserEmail = apprEmail,
            DisposalMethod = disposalRequest.DisposalMethod,
            EstimatedResidualValue = disposalRequest.EstimatedResidualValue,
            ValuationDate = disposalRequest.ValuationDate,
            Status = disposalRequest.Status,
            RequestedAt = disposalRequest.RequestedAt,
            ApprovedAt = disposalRequest.ApprovedAt,
            DisposedAt = disposalRequest.DisposedAt,
            Notes = disposalRequest.Notes,
            PreconditionEvaluation = evalResult
        };

        return new DisposalApprovalResult
        {
            Success = true,
            DisposalResponse = response,
            PreconditionResult = evalResult
        };
    }

    public async Task<List<DisposalResponse>> GetDisposalRequestsAsync(
        Guid organizationId,
        DisposalQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DisposalRequests
            .AsNoTracking()
            .Include(d => d.Asset)
            .Include(d => d.InitiatedByUser)
            .Include(d => d.ApprovedByUser)
            .Where(d => d.OrganizationId == organizationId);

        if (parameters.Status.HasValue)
        {
            query = query.Where(d => d.Status == parameters.Status.Value);
        }

        if (parameters.Method.HasValue)
        {
            query = query.Where(d => d.DisposalMethod == parameters.Method.Value);
        }

        var list = await query
            .OrderByDescending(d => d.RequestedAt)
            .ToListAsync(cancellationToken);

        return list.Select(d => MapToResponse(d, null)).ToList();
    }

    public async Task<DisposalResponse?> GetDisposalRequestByIdAsync(
        Guid organizationId,
        Guid disposalRequestId,
        Guid viewingUserId,
        CancellationToken cancellationToken = default)
    {
        var disposalRequest = await _dbContext.DisposalRequests
            .AsNoTracking()
            .Include(d => d.Asset)
            .Include(d => d.InitiatedByUser)
            .Include(d => d.ApprovedByUser)
            .FirstOrDefaultAsync(d => d.Id == disposalRequestId && d.OrganizationId == organizationId, cancellationToken);

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
