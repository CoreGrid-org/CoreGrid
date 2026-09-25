using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Maintenance.DTOs;
using CoreGrid.Api.Features.Notifications.Services;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Storage;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Maintenance.Services;

public class MaintenanceService : IMaintenanceService
{
    // FR-034: a photo is only ever handed out as a signed, time-limited
    // URL, minted fresh on every authorized read — never persisted.
    private static readonly TimeSpan PhotoUrlExpiry = TimeSpan.FromMinutes(15);

    private readonly CoreGridDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IFileStorageService _fileStorageService;

    public MaintenanceService(
        CoreGridDbContext context,
        INotificationService notificationService,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _notificationService = notificationService;
        _fileStorageService = fileStorageService;
    }

    // Resolves a stored R2 object key into a fresh presigned URL — a no-op
    // when there's no photo. Called only after the record's own read
    // authorization has already been checked (the controller's role gate),
    // so this never hands out a link nobody was cleared to see.
    private async Task ResolvePhotoUrlAsync(MaintenanceRecordDto dto)
    {
        if (string.IsNullOrEmpty(dto.PhotoUrl))
        {
            return;
        }

        dto.PhotoUrl = await _fileStorageService.GetPresignedUrlAsync(dto.PhotoUrl, PhotoUrlExpiry, default);
    }

    public async Task<MaintenanceRecordDto?> GetMaintenanceRecordByIdAsync(Guid organizationId, Guid id)
    {
        var dto = await _context.MaintenanceRecords
            .AsNoTracking()
            .Include(m => m.Asset)
            .Include(m => m.Assignee)
            .Where(m => m.Id == id && m.OrganizationId == organizationId)
            .Select(m => new MaintenanceRecordDto
            {
                Id = m.Id,
                AssetId = m.AssetId,
                AssetCode = m.Asset != null ? m.Asset.AssetCode : string.Empty,
                AssetName = m.Asset != null ? m.Asset.Name : string.Empty,
                Description = m.Description,
                ObservedCondition = m.ObservedCondition,
                PhotoUrl = m.PhotoObjectKey, // resolved to a real presigned URL below
                Type = m.Type,
                Priority = m.Priority,
                Status = m.Status,
                EstimatedCost = m.EstimatedCost,
                ActualCost = m.ActualCost,
                WorkPerformed = m.WorkPerformed,
                CompletionDate = m.CompletionDate,
                ResultingCondition = m.ResultingCondition,
                AssigneeId = m.AssigneeId,
                AssigneeEmail = m.Assignee != null ? m.Assignee.Email : null,
                CancellationReason = m.CancellationReason,
                CreatedAt = m.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (dto is not null)
        {
            await ResolvePhotoUrlAsync(dto);
            await ResolveAssetTypeNamesAsync([dto]);
        }

        return dto;
    }

    // FR-084: populates AssetTypeName via a dedicated single-hop query
    // (Assets → AssetType) rather than a two-hop MaintenanceRecord → Asset
    // → AssetType navigation inside the main Select — EF Core's InMemory
    // provider (used by this project's unit tests) doesn't reliably
    // translate a nested null-conditional two levels deep and silently
    // excludes matching rows instead of just nulling the field. One extra
    // query, but it works identically against InMemory and the real
    // Postgres provider, and it's a single indexed lookup either way.
    private async Task ResolveAssetTypeNamesAsync(IReadOnlyList<MaintenanceRecordDto> dtos)
    {
        var assetIds = dtos.Select(d => d.AssetId).Distinct().ToList();
        if (assetIds.Count == 0)
        {
            return;
        }

        var typeNamesByAssetId = await _context.Assets
            .AsNoTracking()
            .Where(a => assetIds.Contains(a.Id))
            .Select(a => new { a.Id, TypeName = a.AssetType != null ? a.AssetType.Name : string.Empty })
            .ToDictionaryAsync(x => x.Id, x => x.TypeName);

        foreach (var dto in dtos)
        {
            dto.AssetTypeName = typeNamesByAssetId.GetValueOrDefault(dto.AssetId, string.Empty);
        }
    }

    public async Task<MaintenanceRecordDto?> ReportFaultAsync(Guid organizationId, Guid currentUserId, ReportFaultRequest request)
    {
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.OrganizationId == organizationId);

        if (asset is null)
        {
            throw new InvalidOperationException("Asset not found within the organization.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new InvalidOperationException("Description is required when reporting a fault.");
        }

        var validConditions = new[] { "NEW", "GOOD", "FAIR", "POOR", "UNSERVICEABLE" };
        var conditionUpper = request.ObservedCondition.Trim().ToUpper();
        if (!validConditions.Contains(conditionUpper))
        {
            throw new InvalidOperationException("Invalid observed condition specified.");
        }

        var record = new MaintenanceRecord
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = request.AssetId,
            Description = request.Description.Trim(),
            ObservedCondition = conditionUpper,
            PhotoObjectKey = request.PhotoUrl,
            Type = MaintenanceType.CORRECTIVE,
            Priority = MaintenancePriority.MEDIUM, // Default priority for reported faults
            Status = MaintenanceStatus.REQUESTED,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = currentUserId,
            UpdatedBy = currentUserId
        };

        _context.MaintenanceRecords.Add(record);
        await _context.SaveChangesAsync();

        return await GetMaintenanceRecordByIdAsync(organizationId, record.Id);
    }


    // FR-035 - Create maintenance record directly (Officer)              

    public async Task<MaintenanceRecordDto?> CreateMaintenanceAsync(
        Guid organizationId,
        Guid currentUserId,
        CreateMaintenanceRequest request)
    {
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.OrganizationId == organizationId);

        if (asset is null)
        {
            throw new InvalidOperationException("Asset not found within the organisation.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new InvalidOperationException("Description is required.");
        }

        var validConditions = new[] { "NEW", "GOOD", "FAIR", "POOR", "UNSERVICEABLE" };
        var conditionUpper = request.ObservedCondition.Trim().ToUpper();
        if (!validConditions.Contains(conditionUpper))
        {
            throw new InvalidOperationException("Invalid observed condition. Use: NEW, GOOD, FAIR, POOR or UNSERVICEABLE.");
        }

        // If an assignee was specified, verify they belong to the same org.
        if (request.AssigneeId.HasValue)
        {
            var assigneeExists = await _context.Users
                .AnyAsync(u => u.Id == request.AssigneeId.Value && u.OrganizationId == organizationId);

            if (!assigneeExists)
            {
                throw new InvalidOperationException("Assignee not found within the organisation.");
            }
        }

        if (request.EstimatedCost.HasValue && request.EstimatedCost.Value < 0)
        {
            throw new InvalidOperationException("Estimated cost must be a non-negative value.");
        }

        var record = new MaintenanceRecord
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = request.AssetId,
            Description = request.Description.Trim(),
            ObservedCondition = conditionUpper,
            PhotoObjectKey = request.PhotoUrl,
            Type = request.Type,
            Priority = request.Priority,
            Status = MaintenanceStatus.REQUESTED,
            EstimatedCost = request.EstimatedCost,
            AssigneeId = request.AssigneeId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = currentUserId,
            UpdatedBy = currentUserId
        };

        _context.MaintenanceRecords.Add(record);
        await _context.SaveChangesAsync();

        return await GetMaintenanceRecordByIdAsync(organizationId, record.Id);
    }


    // FR-036 - Approve maintenance record (Officer / Administrator)       

    public async Task<MaintenanceRecordDto?> ApproveMaintenanceAsync(
        Guid organizationId,
        Guid currentUserId,
        Guid maintenanceId,
        ApproveMaintenanceRequest request)
    {
        if (request.EstimatedCost < 0)
        {
            throw new InvalidOperationException("Estimated cost must be a non-negative value.");
        }

        var record = await _context.MaintenanceRecords
            .Include(m => m.Asset)
            .FirstOrDefaultAsync(m => m.Id == maintenanceId && m.OrganizationId == organizationId);

        if (record is null)
        {
            throw new KeyNotFoundException("Maintenance record not found.");
        }

        // State-machine guard: only REQUESTED records can be approved.
        if (record.Status != MaintenanceStatus.REQUESTED)
        {
            throw new InvalidOperationException(
                $"Only a REQUESTED maintenance record can be approved. Current status: {record.Status}.");
        }

        // Verify the specified assignee belongs to this organisation.
        var assigneeExists = await _context.Users
            .AnyAsync(u => u.Id == request.AssigneeId && u.OrganizationId == organizationId);

        if (!assigneeExists)
        {
            throw new InvalidOperationException("Assignee not found within the organisation.");
        }

        record.Status = MaintenanceStatus.APPROVED;
        record.AssigneeId = request.AssigneeId;
        record.EstimatedCost = request.EstimatedCost;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.UpdatedBy = currentUserId;

        await _context.SaveChangesAsync();

        // FR-080: notify the newly assigned officer.
        await _notificationService.NotifyAsync(
            organizationId,
            request.AssigneeId,
            "MAINTENANCE_ASSIGNED",
            "Maintenance assigned to you",
            $"You've been assigned maintenance for {record.Asset?.AssetCode ?? "an asset"}: {record.Description}",
            "MaintenanceRecord",
            record.Id,
            default);

        return await GetMaintenanceRecordByIdAsync(organizationId, record.Id);
    }

    // FR-037 / FR-039 - Start maintenance (APPROVED → IN_PROGRESS)        //
  

    public async Task<MaintenanceRecordDto?> StartMaintenanceAsync(
        Guid organizationId,
        Guid currentUserId,
        Guid maintenanceId)
    {
        var record = await _context.MaintenanceRecords
            .Include(m => m.Asset)
            .FirstOrDefaultAsync(m => m.Id == maintenanceId && m.OrganizationId == organizationId);

        if (record is null)
        {
            throw new KeyNotFoundException("Maintenance record not found.");
        }

        // State-machine guard: only APPROVED records can be started.
        if (record.Status != MaintenanceStatus.APPROVED)
        {
            throw new InvalidOperationException(
                $"Only an APPROVED maintenance record can be started. Current status: {record.Status}.");
        }

        // Guard: an assignee must be set before work can begin (Fig. 7).
        if (!record.AssigneeId.HasValue)
        {
            throw new InvalidOperationException(
                "The maintenance record must have an assignee before it can be started.");
        }

        var asset = record.Asset;
        if (asset is null)
        {
            throw new InvalidOperationException("Associated asset could not be loaded.");
        }

        var previousAssetStatus = asset.Status;
        var now = DateTimeOffset.UtcNow;

        // Transition maintenance record.
        record.Status = MaintenanceStatus.IN_PROGRESS;
        record.UpdatedAt = now;
        record.UpdatedBy = currentUserId;

        // FR-039 - place the asset into UNDER_MAINTENANCE.
        asset.Status = "UNDER_MAINTENANCE";
        asset.UpdatedAt = now;
        asset.UpdatedBy = currentUserId;

        // Write an AssetHistory entry for the status change.
        _context.AssetHistoryEntries.Add(new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = asset.Id,
            ActorUserId = currentUserId,
            EventType = "MAINTENANCE",
            Description = $"Maintenance record {record.Id} started — asset placed UNDER_MAINTENANCE.",
            PreviousValue = JsonSerializer.Serialize(new { status = previousAssetStatus }),
            NewValue = JsonSerializer.Serialize(new { status = asset.Status }),
            CreatedAt = now
        });

        await _context.SaveChangesAsync();

        return await GetMaintenanceRecordByIdAsync(organizationId, record.Id);
    }


    // FR-038 / FR-040 - Complete maintenance (IN_PROGRESS → COMPLETED)   //


    public async Task<MaintenanceRecordDto?> CompleteMaintenanceAsync(
        Guid organizationId,
        Guid currentUserId,
        Guid maintenanceId,
        CompleteMaintenanceRequest request)
    {


        if (request.ActualCost < 0)
        {
            throw new InvalidOperationException("Actual cost must be a non-negative value.");
        }

        var workLength = request.WorkPerformed?.Trim().Length ?? 0;
        if (workLength < 10 || workLength > 2000)
        {
            throw new InvalidOperationException(
                "Work performed description must be between 10 and 2,000 characters.");
        }

        var validConditions = new[] { "NEW", "GOOD", "FAIR", "POOR", "UNSERVICEABLE" };
        var conditionUpper = request.ResultingCondition.Trim().ToUpper();
        if (!validConditions.Contains(conditionUpper))
        {
            throw new InvalidOperationException(
                "Invalid resulting condition. Use: NEW, GOOD, FAIR, POOR or UNSERVICEABLE.");
        }

        // Future dates are allowed by user request.

        //  Load record + asset -

        var record = await _context.MaintenanceRecords
            .Include(m => m.Asset)
            .FirstOrDefaultAsync(m => m.Id == maintenanceId && m.OrganizationId == organizationId);

        if (record is null)
        {
            throw new KeyNotFoundException("Maintenance record not found.");
        }

        // AC1 - a COMPLETED record cannot be completed again.
        if (record.Status == MaintenanceStatus.COMPLETED)
        {
            throw new InvalidOperationException(
                "This maintenance record has already been completed.");
        }

        // State-machine guard: only IN_PROGRESS records can be completed.
        if (record.Status != MaintenanceStatus.IN_PROGRESS)
        {
            throw new InvalidOperationException(
                $"Only an IN_PROGRESS maintenance record can be completed. Current status: {record.Status}.");
        }

        var asset = record.Asset;
        if (asset is null)
        {
            throw new InvalidOperationException("Associated asset could not be loaded.");
        }

        // BR1 - Cost variance tolerance check


        if (record.EstimatedCost.HasValue && record.EstimatedCost.Value > 0)
        {
            var policy = await _context.OrganizationPolicies
                .AsNoTracking()
                .Where(p => p.OrganizationId == organizationId
                            && (p.AssetTypeId == asset.AssetTypeId || p.AssetTypeId == null))
                .OrderBy(p => p.AssetTypeId == null ? 1 : 0) // asset-type-specific first
                .FirstOrDefaultAsync();

            if (policy is not null && policy.CostVarianceTolerancePercent > 0)
            {
                var overrunPercent =
                    ((request.ActualCost - record.EstimatedCost.Value) / record.EstimatedCost.Value) * 100m;

                if (overrunPercent > policy.CostVarianceTolerancePercent)
                {
                    if (string.IsNullOrWhiteSpace(request.OverspendJustification))
                    {
                        throw new InvalidOperationException(
                            $"Actual cost exceeds the estimate by {overrunPercent:F1}%, which is above the "
                            + $"organisation's {policy.CostVarianceTolerancePercent}% variance tolerance. "
                            + "Provide an OverspendJustification to proceed (BR1).");
                    }
                }
            }
        }

        //  BR3 - Atomic transaction: all writes together 

        var now = DateTimeOffset.UtcNow;
        var previousAssetStatus = asset.Status;
        var previousAssetCondition = asset.Condition;

        // Transition maintenance record.
        record.Status = MaintenanceStatus.COMPLETED;
        record.ActualCost = request.ActualCost;
        record.WorkPerformed = request.WorkPerformed?.Trim() ?? string.Empty;
        record.CompletionDate = request.CompletionDate;
        record.ResultingCondition = conditionUpper;
        record.UpdatedAt = now;
        record.UpdatedBy = currentUserId;

        // Update asset condition.
        asset.Condition = conditionUpper;

        // FR-040 - Recalculate cumulative cost, repair count, last repair date.
        asset.CumulativeMaintenanceCost += request.ActualCost;
        asset.RepairCount += 1;
        asset.LastRepairDate = request.CompletionDate;

        // BR2 - UNSERVICEABLE resulting condition → CONDEMNED, not ACTIVE.
        asset.Status = conditionUpper == "UNSERVICEABLE" ? "CONDEMNED" : "ACTIVE";
        asset.UpdatedAt = now;
        asset.UpdatedBy = currentUserId;

        // Write AssetHistory - MAINTENANCE event capturing the full transition.
        _context.AssetHistoryEntries.Add(new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = asset.Id,
            ActorUserId = currentUserId,
            EventType = "MAINTENANCE",
            Description = $"Maintenance record {record.Id} completed. "
                        + $"Asset condition updated from {previousAssetCondition} to {conditionUpper}. "
                        + $"Asset status set to {asset.Status}.",
            PreviousValue = JsonSerializer.Serialize(new
            {
                status = previousAssetStatus,
                condition = previousAssetCondition,
                cumulativeMaintenanceCost = asset.CumulativeMaintenanceCost - request.ActualCost,
                repairCount = asset.RepairCount - 1
            }),
            NewValue = JsonSerializer.Serialize(new
            {
                status = asset.Status,
                condition = asset.Condition,
                cumulativeMaintenanceCost = asset.CumulativeMaintenanceCost,
                repairCount = asset.RepairCount,
                lastRepairDate = asset.LastRepairDate
            }),
            CreatedAt = now
        });

        // Single SaveChangesAsync - satisfies BR3 (atomic).
        await _context.SaveChangesAsync();

        // FR-080: notify whoever originally reported/requested this work,
        // if that's someone other than the person completing it. AC4:
        // NotifyAsync never throws, so a notification failure here can
        // never undo the completion that already committed above.
        if (record.CreatedBy.HasValue && record.CreatedBy.Value != currentUserId)
        {
            await _notificationService.NotifyAsync(
                organizationId,
                record.CreatedBy.Value,
                "MAINTENANCE_COMPLETED",
                "Maintenance completed",
                $"Maintenance for {asset.AssetCode} has been completed. Resulting condition: {conditionUpper}.",
                "MaintenanceRecord",
                record.Id,
                default);
        }

        return await GetMaintenanceRecordByIdAsync(organizationId, record.Id);
    }

    public async Task<MaintenanceRecordDto?> CancelMaintenanceAsync(
        Guid organizationId,
        Guid currentUserId,
        Guid maintenanceId,
        CancelMaintenanceRequest request)
    {
        var record = await _context.MaintenanceRecords
            .Include(m => m.Asset)
            .FirstOrDefaultAsync(m => m.Id == maintenanceId && m.OrganizationId == organizationId);

        if (record is null)
        {
            throw new KeyNotFoundException("Maintenance record not found.");
        }

        if (record.Status == MaintenanceStatus.COMPLETED || record.Status == MaintenanceStatus.CANCELLED)
        {
            throw new InvalidOperationException($"Cannot cancel a record with status {record.Status}.");
        }

        var asset = record.Asset;
        var previousAssetStatus = asset?.Status;

        record.Status = MaintenanceStatus.CANCELLED;
        record.CancellationReason = request.Reason;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.UpdatedBy = currentUserId;

        if (asset != null && asset.Status == "UNDER_MAINTENANCE")
        {
            asset.Status = "ACTIVE";
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            asset.UpdatedBy = currentUserId;

            _context.AssetHistoryEntries.Add(new AssetHistory
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                AssetId = asset.Id,
                ActorUserId = currentUserId,
                EventType = "MAINTENANCE_CANCELLED",
                Description = $"Maintenance record {record.Id} cancelled. Asset status reverted to ACTIVE.",
                PreviousValue = JsonSerializer.Serialize(new { status = previousAssetStatus }),
                NewValue = JsonSerializer.Serialize(new { status = asset.Status }),
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        // FR-080: notify the assignee and the original reporter (if either
        // is someone other than whoever cancelled it), so nobody keeps
        // working toward a record that no longer exists.
        var recipientIds = new[] { record.AssigneeId, record.CreatedBy }
            .Where(id => id.HasValue && id.Value != currentUserId)
            .Select(id => id!.Value)
            .Distinct();

        foreach (var recipientId in recipientIds)
        {
            await _notificationService.NotifyAsync(
                organizationId,
                recipientId,
                "MAINTENANCE_CANCELLED",
                "Maintenance cancelled",
                $"Maintenance for {asset?.AssetCode ?? "an asset"} was cancelled" +
                    (string.IsNullOrWhiteSpace(request.Reason) ? "." : $": {request.Reason}"),
                "MaintenanceRecord",
                record.Id,
                default);
        }

        return await GetMaintenanceRecordByIdAsync(organizationId, record.Id);
    }

    public async Task<PagedResult<MaintenanceRecordDto>> ListMaintenanceRecordsAsync(
        Guid organizationId,
        MaintenanceRecordFilter filter)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : Math.Min(filter.PageSize, 100);

        var query = _context.MaintenanceRecords
            .AsNoTracking()
            .Include(m => m.Asset)
            .Include(m => m.Assignee)
            .Where(m => m.OrganizationId == organizationId)
            .AsQueryable();

        if (filter.AssetId.HasValue)
        {
            query = query.Where(m => m.AssetId == filter.AssetId.Value);
        }

        // FR-042: department isn't a column on MaintenanceRecord itself —
        // filter via the owning Asset, same join AssetService's own
        // DepartmentId filter uses.
        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(m => m.Asset != null && m.Asset.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.AssigneeId.HasValue)
        {
            query = query.Where(m => m.AssigneeId == filter.AssigneeId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(m => m.Status == filter.Status.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(m => m.Type == filter.Type.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(m => m.Priority == filter.Priority.Value);
        }

        if (filter.DateFrom.HasValue)
        {
            var from = new DateTimeOffset(filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(m => m.CreatedAt >= from);
        }

        if (filter.DateTo.HasValue)
        {
            // Inclusive of the whole "to" day.
            var to = new DateTimeOffset(filter.DateTo.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            query = query.Where(m => m.CreatedAt <= to);
        }

        var totalCount = await query.CountAsync();

        var sortBy = filter.SortBy?.Trim().ToLowerInvariant() ?? "createdat";
        var descending = !string.Equals(filter.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        query = sortBy switch
        {
            "priority" => descending ? query.OrderByDescending(m => m.Priority) : query.OrderBy(m => m.Priority),
            "status" => descending ? query.OrderByDescending(m => m.Status) : query.OrderBy(m => m.Status),
            "estimatedcost" => descending ? query.OrderByDescending(m => m.EstimatedCost) : query.OrderBy(m => m.EstimatedCost),
            "actualcost" => descending ? query.OrderByDescending(m => m.ActualCost) : query.OrderBy(m => m.ActualCost),
            "completiondate" => descending ? query.OrderByDescending(m => m.CompletionDate) : query.OrderBy(m => m.CompletionDate),
            _ => descending ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt),
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MaintenanceRecordDto
            {
                Id = m.Id,
                AssetId = m.AssetId,
                AssetCode = m.Asset != null ? m.Asset.AssetCode : string.Empty,
                AssetName = m.Asset != null ? m.Asset.Name : string.Empty,
                Description = m.Description,
                ObservedCondition = m.ObservedCondition,
                PhotoUrl = m.PhotoObjectKey, // resolved to a real presigned URL below
                Type = m.Type,
                Priority = m.Priority,
                Status = m.Status,
                EstimatedCost = m.EstimatedCost,
                ActualCost = m.ActualCost,
                WorkPerformed = m.WorkPerformed,
                CompletionDate = m.CompletionDate,
                ResultingCondition = m.ResultingCondition,
                AssigneeId = m.AssigneeId,
                AssigneeEmail = m.Assignee != null ? m.Assignee.Email : null,
                CancellationReason = m.CancellationReason,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        await Task.WhenAll(items.Select(ResolvePhotoUrlAsync));
        await ResolveAssetTypeNamesAsync(items);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<MaintenanceRecordDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }
}
