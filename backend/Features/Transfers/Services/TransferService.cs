using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Transfers.DTOs;

namespace CoreGrid.Api.Features.Transfers.Services;

public class TransferService : ITransferService
{
    private readonly CoreGridDbContext _dbContext;

    public TransferService(CoreGridDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TransferResponse> InitiateTransferAsync(
        Guid organizationId,
        InitiateTransferRequest request,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.OrganizationId == organizationId, cancellationToken);

        if (asset == null)
        {
            throw new KeyNotFoundException($"Asset with ID {request.AssetId} not found in this organization.");
        }

        // Guard: Asset.Status must be ACTIVE (FR-044).
        // If UNDER_MAINTENANCE, TRANSFER_REQUESTED, IN_TRANSIT, CONDEMNED, DISPOSAL_REQUESTED, or DISPOSED -> fail.
        if (!string.Equals(asset.Status, AssetStatusConstants.Active, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Asset cannot be transferred because its status is '{asset.Status}'. Asset must be '{AssetStatusConstants.Active}'.");
        }

        // Verify destination department and location exist within the organization
        var toDepartment = await _dbContext.Departments
            .FirstOrDefaultAsync(d => d.Id == request.ToDepartmentId && d.OrganizationId == organizationId, cancellationToken);
        if (toDepartment == null)
        {
            throw new KeyNotFoundException($"Destination Department with ID {request.ToDepartmentId} not found.");
        }

        var toLocation = await _dbContext.Locations
            .FirstOrDefaultAsync(l => l.Id == request.ToLocationId && l.OrganizationId == organizationId, cancellationToken);
        if (toLocation == null)
        {
            throw new KeyNotFoundException($"Destination Location with ID {request.ToLocationId} not found.");
        }

        var now = DateTimeOffset.UtcNow;

        var transfer = new AssetTransfer
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = asset.Id,
            FromDepartmentId = asset.DepartmentId,
            ToDepartmentId = request.ToDepartmentId,
            FromLocationId = asset.LocationId,
            ToLocationId = request.ToLocationId,
            InitiatedByUserId = initiatedByUserId,
            Status = TransferStatus.REQUESTED,
            RequestedAt = now
        };

        // Atomically set Asset.Status = TRANSFER_REQUESTED
        asset.Status = AssetStatusConstants.TransferRequested;
        asset.UpdatedAt = now;
        asset.UpdatedBy = initiatedByUserId;

        _dbContext.AssetTransfers.Add(transfer);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TransferResponse
        {
            Id = transfer.Id,
            OrganizationId = transfer.OrganizationId,
            AssetId = asset.Id,
            AssetCode = asset.AssetCode,
            AssetName = asset.Name,
            FromDepartmentId = transfer.FromDepartmentId,
            FromDepartmentName = asset.Department?.Name,
            ToDepartmentId = transfer.ToDepartmentId,
            ToDepartmentName = toDepartment.Name,
            FromLocationId = transfer.FromLocationId,
            FromLocationName = asset.Location?.Name,
            ToLocationId = transfer.ToLocationId,
            ToLocationName = toLocation.Name,
            InitiatedByUserId = transfer.InitiatedByUserId,
            InitiatedByUserEmail = transfer.InitiatedByUser?.Email,
            ApprovedByUserId = transfer.ApprovedByUserId,
            ApprovedByUserEmail = transfer.ApprovedByUser?.Email,
            ConfirmedByUserId = transfer.ConfirmedByUserId,
            ConfirmedByUserEmail = transfer.ConfirmedByUser?.Email,
            Status = transfer.Status,
            RequestedAt = transfer.RequestedAt,
            ApprovedAt = transfer.ApprovedAt,
            ConfirmedAt = transfer.ConfirmedAt,
            RejectionReason = transfer.RejectionReason
        };
    }

    public async Task<TransferResponse> ApproveTransferAsync(
        Guid organizationId,
        Guid transferId,
        Guid approvedByUserId,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _dbContext.AssetTransfers
            .Include(t => t.Asset)
            .FirstOrDefaultAsync(t => t.Id == transferId && t.OrganizationId == organizationId, cancellationToken);

        if (transfer == null)
        {
            throw new KeyNotFoundException($"AssetTransfer with ID {transferId} not found.");
        }

        if (transfer.Status != TransferStatus.REQUESTED)
        {
            throw new InvalidOperationException($"Cannot approve transfer in status '{transfer.Status}'. Transfer must be in '{TransferStatus.REQUESTED}' status.");
        }

        if (transfer.Asset == null)
        {
            throw new InvalidOperationException($"Associated Asset {transfer.AssetId} not found.");
        }

        var now = DateTimeOffset.UtcNow;

        // Transition transfer to APPROVED
        transfer.Status = TransferStatus.APPROVED;
        transfer.ApprovedByUserId = approvedByUserId;
        transfer.ApprovedAt = now;

        // Transition asset to IN_TRANSIT (FR-045)
        transfer.Asset.Status = AssetStatusConstants.InTransit;
        transfer.Asset.UpdatedAt = now;
        transfer.Asset.UpdatedBy = approvedByUserId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var fromDeptName = await _dbContext.Departments.Where(d => d.Id == transfer.FromDepartmentId).Select(d => d.Name).FirstOrDefaultAsync(cancellationToken);
        var toDeptName = await _dbContext.Departments.Where(d => d.Id == transfer.ToDepartmentId).Select(d => d.Name).FirstOrDefaultAsync(cancellationToken);
        var fromLocName = await _dbContext.Locations.Where(l => l.Id == transfer.FromLocationId).Select(l => l.Name).FirstOrDefaultAsync(cancellationToken);
        var toLocName = await _dbContext.Locations.Where(l => l.Id == transfer.ToLocationId).Select(l => l.Name).FirstOrDefaultAsync(cancellationToken);
        var initEmail = await _dbContext.Users.Where(u => u.Id == transfer.InitiatedByUserId).Select(u => u.Email).FirstOrDefaultAsync(cancellationToken);
        var apprEmail = await _dbContext.Users.Where(u => u.Id == approvedByUserId).Select(u => u.Email).FirstOrDefaultAsync(cancellationToken);

        return new TransferResponse
        {
            Id = transfer.Id,
            OrganizationId = transfer.OrganizationId,
            AssetId = transfer.AssetId,
            AssetCode = transfer.Asset.AssetCode,
            AssetName = transfer.Asset.Name,
            FromDepartmentId = transfer.FromDepartmentId,
            FromDepartmentName = fromDeptName,
            ToDepartmentId = transfer.ToDepartmentId,
            ToDepartmentName = toDeptName,
            FromLocationId = transfer.FromLocationId,
            FromLocationName = fromLocName,
            ToLocationId = transfer.ToLocationId,
            ToLocationName = toLocName,
            InitiatedByUserId = transfer.InitiatedByUserId,
            InitiatedByUserEmail = initEmail,
            ApprovedByUserId = transfer.ApprovedByUserId,
            ApprovedByUserEmail = apprEmail,
            ConfirmedByUserId = transfer.ConfirmedByUserId,
            ConfirmedByUserEmail = null,
            Status = transfer.Status,
            RequestedAt = transfer.RequestedAt,
            ApprovedAt = transfer.ApprovedAt,
            ConfirmedAt = transfer.ConfirmedAt,
            RejectionReason = transfer.RejectionReason
        };
    }

    public async Task<TransferResponse> ConfirmReceiptAsync(
        Guid organizationId,
        Guid transferId,
        Guid confirmedByUserId,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _dbContext.AssetTransfers
            .Include(t => t.Asset)
            .FirstOrDefaultAsync(t => t.Id == transferId && t.OrganizationId == organizationId, cancellationToken);

        if (transfer == null)
        {
            throw new KeyNotFoundException($"AssetTransfer with ID {transferId} not found.");
        }

        if (transfer.Status != TransferStatus.APPROVED)
        {
            throw new InvalidOperationException($"Cannot confirm receipt for transfer in status '{transfer.Status}'. Transfer must be in '{TransferStatus.APPROVED}' status.");
        }

        if (transfer.Asset == null)
        {
            throw new InvalidOperationException($"Associated Asset {transfer.AssetId} not found.");
        }

        var now = DateTimeOffset.UtcNow;

        // Transition transfer to COMPLETED
        transfer.Status = TransferStatus.COMPLETED;
        transfer.ConfirmedByUserId = confirmedByUserId;
        transfer.ConfirmedAt = now;

        // Update asset location/department and transition status back to ACTIVE (FR-046)
        transfer.Asset.DepartmentId = transfer.ToDepartmentId;
        transfer.Asset.LocationId = transfer.ToLocationId;
        transfer.Asset.Status = AssetStatusConstants.Active;
        transfer.Asset.UpdatedAt = now;
        transfer.Asset.UpdatedBy = confirmedByUserId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var fromDeptName = await _dbContext.Departments.Where(d => d.Id == transfer.FromDepartmentId).Select(d => d.Name).FirstOrDefaultAsync(cancellationToken);
        var toDeptName = await _dbContext.Departments.Where(d => d.Id == transfer.ToDepartmentId).Select(d => d.Name).FirstOrDefaultAsync(cancellationToken);
        var fromLocName = await _dbContext.Locations.Where(l => l.Id == transfer.FromLocationId).Select(l => l.Name).FirstOrDefaultAsync(cancellationToken);
        var toLocName = await _dbContext.Locations.Where(l => l.Id == transfer.ToLocationId).Select(l => l.Name).FirstOrDefaultAsync(cancellationToken);
        var initEmail = await _dbContext.Users.Where(u => u.Id == transfer.InitiatedByUserId).Select(u => u.Email).FirstOrDefaultAsync(cancellationToken);
        var apprEmail = transfer.ApprovedByUserId.HasValue ? await _dbContext.Users.Where(u => u.Id == transfer.ApprovedByUserId.Value).Select(u => u.Email).FirstOrDefaultAsync(cancellationToken) : null;
        var confEmail = await _dbContext.Users.Where(u => u.Id == confirmedByUserId).Select(u => u.Email).FirstOrDefaultAsync(cancellationToken);

        return new TransferResponse
        {
            Id = transfer.Id,
            OrganizationId = transfer.OrganizationId,
            AssetId = transfer.AssetId,
            AssetCode = transfer.Asset.AssetCode,
            AssetName = transfer.Asset.Name,
            FromDepartmentId = transfer.FromDepartmentId,
            FromDepartmentName = fromDeptName,
            ToDepartmentId = transfer.ToDepartmentId,
            ToDepartmentName = toDeptName,
            FromLocationId = transfer.FromLocationId,
            FromLocationName = fromLocName,
            ToLocationId = transfer.ToLocationId,
            ToLocationName = toLocName,
            InitiatedByUserId = transfer.InitiatedByUserId,
            InitiatedByUserEmail = initEmail,
            ApprovedByUserId = transfer.ApprovedByUserId,
            ApprovedByUserEmail = apprEmail,
            ConfirmedByUserId = transfer.ConfirmedByUserId,
            ConfirmedByUserEmail = confEmail,
            Status = transfer.Status,
            RequestedAt = transfer.RequestedAt,
            ApprovedAt = transfer.ApprovedAt,
            ConfirmedAt = transfer.ConfirmedAt,
            RejectionReason = transfer.RejectionReason
        };
    }

    public async Task<List<TransferResponse>> GetTransfersAsync(
        Guid organizationId,
        TransferQueryParameters parameters,
        CancellationToken cancellationToken = default)
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
            .Where(t => t.OrganizationId == organizationId);

        if (parameters.Status.HasValue)
        {
            query = query.Where(t => t.Status == parameters.Status.Value);
        }

        if (parameters.DepartmentId.HasValue)
        {
            query = query.Where(t => t.FromDepartmentId == parameters.DepartmentId.Value || t.ToDepartmentId == parameters.DepartmentId.Value);
        }

        var list = await query
            .OrderByDescending(t => t.RequestedAt)
            .ToListAsync(cancellationToken);

        return list.Select(MapToResponse).ToList();
    }

    public async Task<TransferResponse?> GetTransferByIdAsync(
        Guid organizationId,
        Guid transferId,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _dbContext.AssetTransfers
            .Include(t => t.Asset)
            .Include(t => t.FromDepartment)
            .Include(t => t.ToDepartment)
            .Include(t => t.FromLocation)
            .Include(t => t.ToLocation)
            .Include(t => t.InitiatedByUser)
            .Include(t => t.ApprovedByUser)
            .Include(t => t.ConfirmedByUser)
            .FirstOrDefaultAsync(t => t.Id == transferId && t.OrganizationId == organizationId, cancellationToken);

        return transfer != null ? MapToResponse(transfer) : null;
    }

    private static TransferResponse MapToResponse(AssetTransfer t)
    {
        return new TransferResponse
        {
            Id = t.Id,
            OrganizationId = t.OrganizationId,
            AssetId = t.AssetId,
            AssetCode = t.Asset?.AssetCode ?? string.Empty,
            AssetName = t.Asset?.Name ?? string.Empty,
            FromDepartmentId = t.FromDepartmentId,
            FromDepartmentName = t.FromDepartment?.Name,
            ToDepartmentId = t.ToDepartmentId,
            ToDepartmentName = t.ToDepartment?.Name,
            FromLocationId = t.FromLocationId,
            FromLocationName = t.FromLocation?.Name,
            ToLocationId = t.ToLocationId,
            ToLocationName = t.ToLocation?.Name,
            InitiatedByUserId = t.InitiatedByUserId,
            InitiatedByUserEmail = t.InitiatedByUser?.Email,
            ApprovedByUserId = t.ApprovedByUserId,
            ApprovedByUserEmail = t.ApprovedByUser?.Email,
            ConfirmedByUserId = t.ConfirmedByUserId,
            ConfirmedByUserEmail = t.ConfirmedByUser?.Email,
            Status = t.Status,
            RequestedAt = t.RequestedAt,
            ApprovedAt = t.ApprovedAt,
            ConfirmedAt = t.ConfirmedAt,
            RejectionReason = t.RejectionReason
        };
    }
}
