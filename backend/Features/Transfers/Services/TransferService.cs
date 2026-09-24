using System.Linq.Expressions;
using System.Text.Json;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using CoreGrid.Api.Features.Shared.Scoping;
using CoreGrid.Api.Features.Transfers.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Transfers.Services;

public class TransferService : ITransferService
{
    private static readonly Expression<Func<AssetTransfer, TransferResponse>> ToResponseExpression = t => new TransferResponse
    {
        Id = t.Id,
        OrganizationId = t.OrganizationId,
        AssetId = t.AssetId,
        AssetCode = t.Asset != null ? t.Asset.AssetCode : string.Empty,
        AssetName = t.Asset != null ? t.Asset.Name : string.Empty,
        FromDepartmentId = t.FromDepartmentId,
        FromDepartmentName = t.FromDepartment != null ? t.FromDepartment.Name : null,
        ToDepartmentId = t.ToDepartmentId,
        ToDepartmentName = t.ToDepartment != null ? t.ToDepartment.Name : null,
        FromLocationId = t.FromLocationId,
        FromLocationName = t.FromLocation != null ? t.FromLocation.Name : null,
        ToLocationId = t.ToLocationId,
        ToLocationName = t.ToLocation != null ? t.ToLocation.Name : null,
        InitiatedByUserId = t.InitiatedByUserId,
        InitiatedByUserEmail = t.InitiatedByUser != null ? t.InitiatedByUser.Email : null,
        ApprovedByUserId = t.ApprovedByUserId,
        ApprovedByUserEmail = t.ApprovedByUser != null ? t.ApprovedByUser.Email : null,
        ConfirmedByUserId = t.ConfirmedByUserId,
        ConfirmedByUserEmail = t.ConfirmedByUser != null ? t.ConfirmedByUser.Email : null,
        Status = t.Status,
        RequestedAt = t.RequestedAt,
        ApprovedAt = t.ApprovedAt,
        ConfirmedAt = t.ConfirmedAt,
        RejectionReason = t.RejectionReason
    };

    private static readonly IReadOnlyDictionary<string, Expression<Func<AssetTransfer, object?>>> SortMap =
        new Dictionary<string, Expression<Func<AssetTransfer, object?>>>
        {
            ["requestedat"] = t => t.RequestedAt,
            ["approvedat"] = t => t.ApprovedAt,
            ["confirmedat"] = t => t.ConfirmedAt,
            ["status"] = t => t.Status,
        };

    private readonly CoreGridDbContext _dbContext;

    public TransferService(CoreGridDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TransferResponse> InitiateTransferAsync(
        Guid organizationId,
        InitiateTransferRequest request,
        Guid initiatedByUserId,
        CancellationToken cancellationToken)
    {
        // [Required] on the DTO makes a missing value 400 for a
        // model-bound HTTP caller before this method ever runs.
        var assetId = request.AssetId!.Value;
        var toDepartmentId = request.ToDepartmentId!.Value;
        var toLocationId = request.ToLocationId!.Value;

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.OrganizationId == organizationId, cancellationToken)
            ?? throw new ValidationException(nameof(request.AssetId), $"Asset with ID {assetId} not found in this organization.");

        // Guard: Asset.Status must be ACTIVE (FR-044).
        if (!string.Equals(asset.Status, AssetStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException(
                $"Asset cannot be transferred because its status is '{asset.Status}'. Asset must be '{AssetStatuses.Active}'.",
                "asset_not_active");
        }

        var toDepartment = await _dbContext.Departments
            .FirstOrDefaultAsync(d => d.Id == toDepartmentId && d.OrganizationId == organizationId, cancellationToken)
            ?? throw new ValidationException(nameof(request.ToDepartmentId), $"Destination Department with ID {toDepartmentId} not found.");

        var toLocation = await _dbContext.Locations
            .FirstOrDefaultAsync(l => l.Id == toLocationId && l.OrganizationId == organizationId, cancellationToken)
            ?? throw new ValidationException(nameof(request.ToLocationId), $"Destination Location with ID {toLocationId} not found.");

        var now = DateTimeOffset.UtcNow;
        var previousAssetStatus = asset.Status;

        var transfer = new AssetTransfer
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = asset.Id,
            FromDepartmentId = asset.DepartmentId,
            ToDepartmentId = toDepartmentId,
            FromLocationId = asset.LocationId,
            ToLocationId = toLocationId,
            InitiatedByUserId = initiatedByUserId,
            Status = TransferStatus.REQUESTED,
            RequestedAt = now
        };

        // Atomically set Asset.Status = TRANSFER_REQUESTED
        asset.Status = AssetStatuses.TransferRequested;
        asset.UpdatedAt = now;
        asset.UpdatedBy = initiatedByUserId;

        _dbContext.AssetTransfers.Add(transfer);

        // FR-027 (B12): lifecycle history for the transfer request.
        _dbContext.AssetHistoryEntries.Add(new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = asset.Id,
            ActorUserId = initiatedByUserId,
            EventType = AssetHistoryEventTypes.Transfer,
            Description = $"Transfer requested to {toDepartment.Name} / {toLocation.Name}.",
            PreviousValue = JsonSerializer.Serialize(new { status = previousAssetStatus }),
            NewValue = JsonSerializer.Serialize(new { status = asset.Status, toDepartmentId, toLocationId }),
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetTransferByIdAsync(organizationId, DepartmentScope.Unrestricted, transfer.Id, cancellationToken)
            ?? throw new InvalidOperationException("Transfer was created but could not be retrieved.");
    }

    public async Task<TransferResponse> ApproveTransferAsync(
        Guid organizationId,
        Guid transferId,
        Guid approvedByUserId,
        CancellationToken cancellationToken)
    {
        var transfer = await _dbContext.AssetTransfers
            .Include(t => t.Asset)
            .FirstOrDefaultAsync(t => t.Id == transferId && t.OrganizationId == organizationId, cancellationToken)
            ?? throw NotFoundException.For(nameof(AssetTransfer), transferId);

        if (transfer.Status != TransferStatus.REQUESTED)
        {
            throw new ConflictException(
                $"Cannot approve transfer in status '{transfer.Status}'. Transfer must be in '{TransferStatus.REQUESTED}' status.",
                "invalid_status_transition");
        }

        var asset = transfer.Asset ?? throw new InvalidOperationException($"Associated Asset {transfer.AssetId} not found.");

        var now = DateTimeOffset.UtcNow;
        var previousAssetStatus = asset.Status;

        transfer.Status = TransferStatus.APPROVED;
        transfer.ApprovedByUserId = approvedByUserId;
        transfer.ApprovedAt = now;

        // Transition asset to IN_TRANSIT (FR-045)
        asset.Status = AssetStatuses.InTransit;
        asset.UpdatedAt = now;
        asset.UpdatedBy = approvedByUserId;

        // FR-027 (B12): lifecycle history for the approval.
        _dbContext.AssetHistoryEntries.Add(new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = asset.Id,
            ActorUserId = approvedByUserId,
            EventType = AssetHistoryEventTypes.Transfer,
            Description = "Transfer approved; asset in transit.",
            PreviousValue = JsonSerializer.Serialize(new { status = previousAssetStatus }),
            NewValue = JsonSerializer.Serialize(new { status = asset.Status }),
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetTransferByIdAsync(organizationId, DepartmentScope.Unrestricted, transfer.Id, cancellationToken)
            ?? throw new InvalidOperationException("Transfer was approved but could not be retrieved.");
    }

    public async Task<TransferResponse> RejectTransferAsync(
        Guid organizationId,
        Guid transferId,
        Guid rejectedByUserId,
        RejectTransferRequest request,
        CancellationToken cancellationToken)
    {
        var transfer = await _dbContext.AssetTransfers
            .Include(t => t.Asset)
            .FirstOrDefaultAsync(t => t.Id == transferId && t.OrganizationId == organizationId, cancellationToken)
            ?? throw NotFoundException.For(nameof(AssetTransfer), transferId);

        if (transfer.Status != TransferStatus.REQUESTED)
        {
            throw new ConflictException(
                $"Cannot reject transfer in status '{transfer.Status}'. Transfer must be in '{TransferStatus.REQUESTED}' status.",
                "invalid_status_transition");
        }

        var asset = transfer.Asset ?? throw new InvalidOperationException($"Associated Asset {transfer.AssetId} not found.");

        var now = DateTimeOffset.UtcNow;
        var previousAssetStatus = asset.Status;
        var reason = request.Reason.Trim();

        transfer.Status = TransferStatus.REJECTED;
        transfer.RejectionReason = reason;

        // The asset never left the requesting department — release the
        // TRANSFER_REQUESTED hold back to ACTIVE (SRS §9.4).
        asset.Status = AssetStatuses.Active;
        asset.UpdatedAt = now;
        asset.UpdatedBy = rejectedByUserId;

        _dbContext.AssetHistoryEntries.Add(new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = asset.Id,
            ActorUserId = rejectedByUserId,
            EventType = AssetHistoryEventTypes.Transfer,
            Description = $"Transfer rejected: {reason}",
            PreviousValue = JsonSerializer.Serialize(new { status = previousAssetStatus }),
            NewValue = JsonSerializer.Serialize(new { status = asset.Status }),
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetTransferByIdAsync(organizationId, DepartmentScope.Unrestricted, transfer.Id, cancellationToken)
            ?? throw new InvalidOperationException("Transfer was rejected but could not be retrieved.");
    }

    public async Task<TransferResponse> ConfirmReceiptAsync(
        Guid organizationId,
        Guid transferId,
        Guid confirmedByUserId,
        CoreGridRole callerRole,
        Guid? callerDepartmentId,
        CancellationToken cancellationToken)
    {
        var transfer = await _dbContext.AssetTransfers
            .Include(t => t.Asset)
            .FirstOrDefaultAsync(t => t.Id == transferId && t.OrganizationId == organizationId, cancellationToken)
            ?? throw NotFoundException.For(nameof(AssetTransfer), transferId);

        if (transfer.Status != TransferStatus.APPROVED)
        {
            throw new ConflictException(
                $"Cannot confirm receipt for transfer in status '{transfer.Status}'. Transfer must be in '{TransferStatus.APPROVED}' status.",
                "invalid_status_transition");
        }

        // Appendix B: an InventoryOfficer confirming receipt must belong
        // to the destination department — Administrator is exempt.
        if (callerRole == CoreGridRole.InventoryOfficer && callerDepartmentId != transfer.ToDepartmentId)
        {
            throw new ForbiddenException("You can only confirm receipt for transfers into your own department.");
        }

        var asset = transfer.Asset ?? throw new InvalidOperationException($"Associated Asset {transfer.AssetId} not found.");

        var now = DateTimeOffset.UtcNow;
        var previousAssetStatus = asset.Status;
        var previousDepartmentId = asset.DepartmentId;
        var previousLocationId = asset.LocationId;

        transfer.Status = TransferStatus.COMPLETED;
        transfer.ConfirmedByUserId = confirmedByUserId;
        transfer.ConfirmedAt = now;

        // Update asset location/department and transition status back to ACTIVE (FR-046)
        asset.DepartmentId = transfer.ToDepartmentId;
        asset.LocationId = transfer.ToLocationId;
        asset.Status = AssetStatuses.Active;
        asset.UpdatedAt = now;
        asset.UpdatedBy = confirmedByUserId;

        // FR-027 (B12): lifecycle history for the completed transfer.
        _dbContext.AssetHistoryEntries.Add(new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = asset.Id,
            ActorUserId = confirmedByUserId,
            EventType = AssetHistoryEventTypes.Transfer,
            Description = "Transfer completed; asset received.",
            PreviousValue = JsonSerializer.Serialize(new { status = previousAssetStatus, departmentId = previousDepartmentId, locationId = previousLocationId }),
            NewValue = JsonSerializer.Serialize(new { status = asset.Status, departmentId = asset.DepartmentId, locationId = asset.LocationId }),
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetTransferByIdAsync(organizationId, DepartmentScope.Unrestricted, transfer.Id, cancellationToken)
            ?? throw new InvalidOperationException("Transfer was confirmed but could not be retrieved.");
    }

    public async Task<PagedResult<TransferResponse>> GetTransfersAsync(
        Guid organizationId,
        DepartmentScope scope,
        TransferQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.AssetTransfers
            .AsNoTracking()
            .Include(t => t.Asset)
            .Include(t => t.FromDepartment)
            .Include(t => t.ToDepartment)
            .Include(t => t.FromLocation)
            .Include(t => t.ToLocation)
            .Include(t => t.InitiatedByUser)
            .Include(t => t.ApprovedByUser)
            .Include(t => t.ConfirmedByUser)
            .Where(t => t.OrganizationId == organizationId)
            .ApplyScope(scope, t => t.Asset != null ? (Guid?)t.Asset.DepartmentId : null);

        if (parameters.Status.HasValue)
        {
            query = query.Where(t => t.Status == parameters.Status.Value);
        }

        if (parameters.DepartmentId.HasValue)
        {
            query = query.Where(t => t.FromDepartmentId == parameters.DepartmentId.Value || t.ToDepartmentId == parameters.DepartmentId.Value);
        }

        var sorted = query.ApplySort(parameters, SortMap, defaultSortKey: "requestedat");
        return await sorted.ToPagedResultAsync(parameters, ToResponseExpression, cancellationToken);
    }

    public async Task<TransferResponse?> GetTransferByIdAsync(
        Guid organizationId,
        DepartmentScope scope,
        Guid transferId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.AssetTransfers
            .AsNoTracking()
            .Include(t => t.Asset)
            .Include(t => t.FromDepartment)
            .Include(t => t.ToDepartment)
            .Include(t => t.FromLocation)
            .Include(t => t.ToLocation)
            .Include(t => t.InitiatedByUser)
            .Include(t => t.ApprovedByUser)
            .Include(t => t.ConfirmedByUser)
            .Where(t => t.Id == transferId && t.OrganizationId == organizationId)
            .ApplyScope(scope, t => t.Asset != null ? (Guid?)t.Asset.DepartmentId : null)
            .Select(ToResponseExpression)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // FR-047: Complete transfer history per asset showing origin, destination, requester, approver, receiver, and all timestamps
    public async Task<PagedResult<TransferResponse>> GetTransferHistoryForAssetAsync(
        Guid organizationId,
        DepartmentScope scope,
        Guid assetId,
        PagedQuery query,
        CancellationToken cancellationToken)
    {
        var transfers = _dbContext.AssetTransfers
            .AsNoTracking()
            .Include(t => t.Asset)
            .Include(t => t.FromDepartment)
            .Include(t => t.ToDepartment)
            .Include(t => t.FromLocation)
            .Include(t => t.ToLocation)
            .Include(t => t.InitiatedByUser)
            .Include(t => t.ApprovedByUser)
            .Include(t => t.ConfirmedByUser)
            .Where(t => t.OrganizationId == organizationId && t.AssetId == assetId)
            .ApplyScope(scope, t => t.Asset != null ? (Guid?)t.Asset.DepartmentId : null)
            .OrderByDescending(t => t.RequestedAt);

        return await transfers.ToPagedResultAsync(query, ToResponseExpression, cancellationToken);
    }
}
