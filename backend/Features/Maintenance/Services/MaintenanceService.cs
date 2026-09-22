using System.Linq.Expressions;
using System.Text.Json;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Maintenance.DTOs;
using CoreGrid.Api.Features.Notifications.Services;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using CoreGrid.Api.Features.Shared.Scoping;
using CoreGrid.Api.Features.Shared.Storage;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Maintenance.Services;

public class MaintenanceService : IMaintenanceService
{
   // Defines the lifetime of generated photo URLs.
    private static readonly TimeSpan PhotoUrlExpiry = TimeSpan.FromMinutes(15);

    private static readonly string[] ValidConditions = AssetConditions.All;

   // Defines the maintenance record DTO projection.
    private static readonly Expression<Func<MaintenanceRecord, MaintenanceRecordDto>> ToDtoExpression = m => new MaintenanceRecordDto
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
    };

    private static readonly IReadOnlyDictionary<string, Expression<Func<MaintenanceRecord, object?>>> SortMap =
        new Dictionary<string, Expression<Func<MaintenanceRecord, object?>>>
        {
            ["priority"] = m => m.Priority,
            ["status"] = m => m.Status,
            ["estimatedcost"] = m => m.EstimatedCost,
            ["actualcost"] = m => m.ActualCost,
            ["completiondate"] = m => m.CompletionDate,
            ["createdat"] = m => m.CreatedAt,
        };

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

   
    private async Task ResolvePhotoUrlAsync(MaintenanceRecordDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(dto.PhotoUrl))
        {
            return;
        }

        dto.PhotoUrl = await _fileStorageService.GetPresignedUrlAsync(dto.PhotoUrl, PhotoUrlExpiry, cancellationToken);
    }

    public async Task<MaintenanceRecordDto?> GetMaintenanceRecordByIdAsync(
        Guid organizationId, DepartmentScope scope, Guid id, CancellationToken cancellationToken)
    {
        var dto = await _context.MaintenanceRecords
            .AsNoTracking()
            .Where(m => m.Id == id && m.OrganizationId == organizationId)
            .ApplyScope(scope, m => m.Asset != null ? (Guid?)m.Asset.DepartmentId : null)
            .Select(ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

        if (dto is not null)
        {
            await ResolvePhotoUrlAsync(dto, cancellationToken);
            await ResolveAssetTypeNamesAsync([dto], cancellationToken);
        }

        return dto;
    }

  // Resolves asset type names for the returned maintenance records.
    private async Task ResolveAssetTypeNamesAsync(IReadOnlyList<MaintenanceRecordDto> dtos, CancellationToken cancellationToken)
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
            .ToDictionaryAsync(x => x.Id, x => x.TypeName, cancellationToken);

        foreach (var dto in dtos)
        {
            dto.AssetTypeName = typeNamesByAssetId.GetValueOrDefault(dto.AssetId, string.Empty);
        }
    }

    public async Task<MaintenanceRecordDto?> ReportFaultAsync(
        Guid organizationId, Guid currentUserId, ReportFaultRequest request, CancellationToken cancellationToken)
    {
        // [Required] on the DTO makes a missing value 400 for a
        // model-bound HTTP caller before this method ever runs.
        var assetId = request.AssetId!.Value;

        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.OrganizationId == organizationId, cancellationToken);

        if (asset is null)
        {
            throw new ValidationException(nameof(request.AssetId), "Asset not found within the organization.");
        }

        var conditionUpper = ValidateCondition(request.ObservedCondition, nameof(request.ObservedCondition));

        var record = new MaintenanceRecord
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = assetId,
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
        await _context.SaveChangesAsync(cancellationToken);

        return await GetMaintenanceRecordByIdAsync(organizationId, DepartmentScope.Unrestricted, record.Id, cancellationToken);
    }

    
    // once the record is terminal (COMPLETED/CANCELLED) — same guard
    // CancelMaintenanceAsync uses — since there's nothing left to amend.
    public async Task<MaintenanceRecordDto?> AmendMaintenanceAsync(
        Guid organizationId, Guid currentUserId, Guid maintenanceId, AmendMaintenanceRequest request, CancellationToken cancellationToken)
    {
        var record = await _context.MaintenanceRecords
            .FirstOrDefaultAsync(m => m.Id == maintenanceId && m.OrganizationId == organizationId, cancellationToken);

        if (record is null)
        {
            return null;
        }

        if (record.Status is MaintenanceStatus.COMPLETED or MaintenanceStatus.CANCELLED)
        {
            throw new ConflictException($"Cannot amend a record with status {record.Status}.", "invalid_status_transition");
        }

        record.Type = request.Type!.Value;
        record.Priority = request.Priority!.Value;
        record.Description = request.Description.Trim();
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.UpdatedBy = currentUserId;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetMaintenanceRecordByIdAsync(organizationId, DepartmentScope.Unrestricted, record.Id, cancellationToken);
    }

    //Create maintenance record directly (Officer)
    public async Task<MaintenanceRecordDto?> CreateMaintenanceAsync(
        Guid organizationId, Guid currentUserId, CreateMaintenanceRequest request, CancellationToken cancellationToken)
    {
        // [Required] on the DTO makes a missing value 400 for a
        // model-bound HTTP caller before this method ever runs.
        var assetId = request.AssetId!.Value;
        var type = request.Type!.Value;
        var priority = request.Priority!.Value;

        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.OrganizationId == organizationId, cancellationToken);

        if (asset is null)
        {
            throw new ValidationException(nameof(request.AssetId), "Asset not found within the organisation.");
        }

        var conditionUpper = ValidateCondition(request.ObservedCondition, nameof(request.ObservedCondition));

        // If an assignee was specified, verify they belong to the same org.
        if (request.AssigneeId.HasValue)
        {
            var assigneeExists = await _context.Users
                .AnyAsync(u => u.Id == request.AssigneeId.Value && u.OrganizationId == organizationId, cancellationToken);

            if (!assigneeExists)
            {
                throw new ValidationException(nameof(request.AssigneeId), "Assignee not found within the organisation.");
            }
        }

        var record = new MaintenanceRecord
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = assetId,
            Description = request.Description.Trim(),
            ObservedCondition = conditionUpper,
            PhotoObjectKey = request.PhotoUrl,
            Type = type,
            Priority = priority,
            Status = MaintenanceStatus.REQUESTED,
            EstimatedCost = request.EstimatedCost,
            AssigneeId = request.AssigneeId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = currentUserId,
            UpdatedBy = currentUserId
        };

        _context.MaintenanceRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetMaintenanceRecordByIdAsync(organizationId, DepartmentScope.Unrestricted, record.Id, cancellationToken);
    }

    // FR-036 - Approve maintenance record (Officer / Administrator)
    public async Task<MaintenanceRecordDto?> ApproveMaintenanceAsync(
        Guid organizationId, Guid currentUserId, Guid maintenanceId, ApproveMaintenanceRequest request, CancellationToken cancellationToken)
    {
        // [Required][Range] on the DTO makes a missing/invalid value 400
        // for a model-bound HTTP caller before this method ever runs.
        var assigneeId = request.AssigneeId!.Value;
        var estimatedCost = request.EstimatedCost!.Value;

        var record = await _context.MaintenanceRecords
            .Include(m => m.Asset)
            .FirstOrDefaultAsync(m => m.Id == maintenanceId && m.OrganizationId == organizationId, cancellationToken)
            ?? throw NotFoundException.For(nameof(MaintenanceRecord), maintenanceId);

        // State-machine guard: only REQUESTED records can be approved.
        if (record.Status != MaintenanceStatus.REQUESTED)
        {
            throw new ConflictException($"Only a REQUESTED maintenance record can be approved. Current status: {record.Status}.", "invalid_status_transition");
        }

        // Verify the specified assignee belongs to this organisation.
        var assigneeExists = await _context.Users
            .AnyAsync(u => u.Id == assigneeId && u.OrganizationId == organizationId, cancellationToken);

        if (!assigneeExists)
        {
            throw new ValidationException(nameof(request.AssigneeId), "Assignee not found within the organisation.");
        }

        record.Status = MaintenanceStatus.APPROVED;
        record.AssigneeId = assigneeId;
        record.EstimatedCost = estimatedCost;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.UpdatedBy = currentUserId;

        await _context.SaveChangesAsync(cancellationToken);

        // FR-080: notify the newly assigned officer.
        await _notificationService.NotifyAsync(
            organizationId,
            assigneeId,
            NotificationTypes.MaintenanceAssigned,
            "Maintenance assigned to you",
            $"You've been assigned maintenance for {record.Asset?.AssetCode ?? "an asset"}: {record.Description}",
            "MaintenanceRecord",
            record.Id,
            cancellationToken);

        return await GetMaintenanceRecordByIdAsync(organizationId, DepartmentScope.Unrestricted, record.Id, cancellationToken);
    }

    // FR-037 / FR-039 - Start maintenance (APPROVED → IN_PROGRESS)
    public async Task<MaintenanceRecordDto?> StartMaintenanceAsync(
        Guid organizationId, Guid currentUserId, Guid maintenanceId, CancellationToken cancellationToken)
    {
        var record = await _context.MaintenanceRecords
            .Include(m => m.Asset)
            .FirstOrDefaultAsync(m => m.Id == maintenanceId && m.OrganizationId == organizationId, cancellationToken)
            ?? throw NotFoundException.For(nameof(MaintenanceRecord), maintenanceId);

        // State-machine guard: only APPROVED records can be started.
        if (record.Status != MaintenanceStatus.APPROVED)
        {
            throw new ConflictException($"Only an APPROVED maintenance record can be started. Current status: {record.Status}.", "invalid_status_transition");
        }

        // Guard: an assignee must be set before work can begin (Fig. 7).
        if (!record.AssigneeId.HasValue)
        {
            throw new ConflictException("The maintenance record must have an assignee before it can be started.", "missing_assignee");
        }

        var asset = record.Asset ?? throw new InvalidOperationException("Associated asset could not be loaded.");

        var previousAssetStatus = asset.Status;
        var now = DateTimeOffset.UtcNow;

        record.Status = MaintenanceStatus.IN_PROGRESS;
        record.UpdatedAt = now;
        record.UpdatedBy = currentUserId;

        // FR-039 - place the asset into UNDER_MAINTENANCE.
        asset.Status = AssetStatuses.UnderMaintenance;
        asset.UpdatedAt = now;
        asset.UpdatedBy = currentUserId;

        _context.AssetHistoryEntries.Add(new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = asset.Id,
            ActorUserId = currentUserId,
            EventType = AssetHistoryEventTypes.Maintenance,
            Description = $"Maintenance record {record.Id} started — asset placed UNDER_MAINTENANCE.",
            PreviousValue = JsonSerializer.Serialize(new { status = previousAssetStatus }),
            NewValue = JsonSerializer.Serialize(new { status = asset.Status }),
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);

        return await GetMaintenanceRecordByIdAsync(organizationId, DepartmentScope.Unrestricted, record.Id, cancellationToken);
    }

    //  Complete maintenance (IN_PROGRESS → COMPLETED)
    public async Task<MaintenanceRecordDto?> CompleteMaintenanceAsync(
        Guid organizationId, Guid currentUserId, Guid maintenanceId, CompleteMaintenanceRequest request, CancellationToken cancellationToken)
    {
        // [Required][Range] on the DTO makes a missing/invalid value 400
        // for a model-bound HTTP caller before this method ever runs.
        var actualCost = request.ActualCost!.Value;
        var completionDate = request.CompletionDate!.Value;

        var conditionUpper = ValidateCondition(request.ResultingCondition, nameof(request.ResultingCondition));

        if (completionDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ValidationException(nameof(request.CompletionDate), "Completion date cannot be in the future.");
        }

        var record = await _context.MaintenanceRecords
            .Include(m => m.Asset)
            .FirstOrDefaultAsync(m => m.Id == maintenanceId && m.OrganizationId == organizationId, cancellationToken)
            ?? throw NotFoundException.For(nameof(MaintenanceRecord), maintenanceId);

        //  a COMPLETED record cannot be completed again.
        if (record.Status == MaintenanceStatus.COMPLETED)
        {
            throw new ConflictException("This maintenance record has already been completed.", "already_completed");
        }

        // State-machine guard: only IN_PROGRESS records can be completed.
        if (record.Status != MaintenanceStatus.IN_PROGRESS)
        {
            throw new ConflictException($"Only an IN_PROGRESS maintenance record can be completed. Current status: {record.Status}.", "invalid_status_transition");
        }

        var asset = record.Asset ?? throw new InvalidOperationException("Associated asset could not be loaded.");

        // Cost variance tolerance check
        if (record.EstimatedCost.HasValue && record.EstimatedCost.Value > 0)
        {
            var policy = await _context.OrganizationPolicies
                .AsNoTracking()
                .Where(p => p.OrganizationId == organizationId
                            && (p.AssetTypeId == asset.AssetTypeId || p.AssetTypeId == null))
                .OrderBy(p => p.AssetTypeId == null ? 1 : 0) // asset-type-specific first
                .FirstOrDefaultAsync(cancellationToken);

            if (policy is not null && policy.CostVarianceTolerancePercent > 0)
            {
                var overrunPercent = ((actualCost - record.EstimatedCost.Value) / record.EstimatedCost.Value) * 100m;

                if (overrunPercent > policy.CostVarianceTolerancePercent && string.IsNullOrWhiteSpace(request.OverspendJustification))
                {
                    throw new BusinessRuleException(
                        $"Actual cost exceeds the estimate by {overrunPercent:F1}%, which is above the "
                        + $"organisation's {policy.CostVarianceTolerancePercent}% variance tolerance. "
                        + "Provide an OverspendJustification to proceed (BR1).",
                        "cost_variance_exceeded");
                }
            }
        }

        // Atomic transaction: all writes together
        var now = DateTimeOffset.UtcNow;
        var previousAssetStatus = asset.Status;
        var previousAssetCondition = asset.Condition;

        record.Status = MaintenanceStatus.COMPLETED;
        record.ActualCost = actualCost;
        record.WorkPerformed = request.WorkPerformed.Trim();
        record.CompletionDate = completionDate;
        record.ResultingCondition = conditionUpper;
        record.UpdatedAt = now;
        record.UpdatedBy = currentUserId;

        asset.Condition = conditionUpper;

        // FR-040 - Recalculate cumulative cost, repair count, last repair date.
        asset.CumulativeMaintenanceCost += actualCost;
        asset.RepairCount += 1;
        asset.LastRepairDate = completionDate;

        // BR2 - UNSERVICEABLE resulting condition → CONDEMNED, not ACTIVE.
        asset.Status = conditionUpper == AssetConditions.Unserviceable ? AssetStatuses.Condemned : AssetStatuses.Active;
        asset.UpdatedAt = now;
        asset.UpdatedBy = currentUserId;

        _context.AssetHistoryEntries.Add(new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = asset.Id,
            ActorUserId = currentUserId,
            EventType = AssetHistoryEventTypes.Maintenance,
            Description = $"Maintenance record {record.Id} completed. "
                        + $"Asset condition updated from {previousAssetCondition} to {conditionUpper}. "
                        + $"Asset status set to {asset.Status}.",
            PreviousValue = JsonSerializer.Serialize(new
            {
                status = previousAssetStatus,
                condition = previousAssetCondition,
                cumulativeMaintenanceCost = asset.CumulativeMaintenanceCost - actualCost,
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
        await _context.SaveChangesAsync(cancellationToken);

        // FR-080: notify whoever originally reported/requested this work,
        // if that's someone other than the person completing it. AC4:
        // NotifyAsync never throws, so a notification failure here can
        // never undo the completion that already committed above.
        if (record.CreatedBy.HasValue && record.CreatedBy.Value != currentUserId)
        {
            await _notificationService.NotifyAsync(
                organizationId,
                record.CreatedBy.Value,
                NotificationTypes.MaintenanceCompleted,
                "Maintenance completed",
                $"Maintenance for {asset.AssetCode} has been completed. Resulting condition: {conditionUpper}.",
                "MaintenanceRecord",
                record.Id,
                cancellationToken);
        }

        return await GetMaintenanceRecordByIdAsync(organizationId, DepartmentScope.Unrestricted, record.Id, cancellationToken);
    }

    public async Task<MaintenanceRecordDto?> CancelMaintenanceAsync(
        Guid organizationId, Guid currentUserId, Guid maintenanceId, CancelMaintenanceRequest request, CancellationToken cancellationToken)
    {
        var record = await _context.MaintenanceRecords
            .Include(m => m.Asset)
            .FirstOrDefaultAsync(m => m.Id == maintenanceId && m.OrganizationId == organizationId, cancellationToken)
            ?? throw NotFoundException.For(nameof(MaintenanceRecord), maintenanceId);

        if (record.Status is MaintenanceStatus.COMPLETED or MaintenanceStatus.CANCELLED)
        {
            throw new ConflictException($"Cannot cancel a record with status {record.Status}.", "invalid_status_transition");
        }

        var asset = record.Asset;
        var previousAssetStatus = asset?.Status;

        record.Status = MaintenanceStatus.CANCELLED;
        record.CancellationReason = request.Reason;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        record.UpdatedBy = currentUserId;

        if (asset != null && asset.Status == AssetStatuses.UnderMaintenance)
        {
            asset.Status = AssetStatuses.Active;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            asset.UpdatedBy = currentUserId;

            _context.AssetHistoryEntries.Add(new AssetHistory
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                AssetId = asset.Id,
                ActorUserId = currentUserId,
                // B6: this used to be the literal string "MAINTENANCE_CANCELLED",
                // which is not one of CK_AssetHistory_EventType's allowed
                // values — cancelling an IN_PROGRESS record threw a DB
                // check-constraint violation (500) instead of succeeding.
                EventType = AssetHistoryEventTypes.Maintenance,
                Description = $"Maintenance record {record.Id} cancelled. Asset status reverted to ACTIVE.",
                PreviousValue = JsonSerializer.Serialize(new { status = previousAssetStatus }),
                NewValue = JsonSerializer.Serialize(new { status = asset.Status }),
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

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
                NotificationTypes.MaintenanceCancelled,
                "Maintenance cancelled",
                $"Maintenance for {asset?.AssetCode ?? "an asset"} was cancelled" +
                    (string.IsNullOrWhiteSpace(request.Reason) ? "." : $": {request.Reason}"),
                "MaintenanceRecord",
                record.Id,
                cancellationToken);
        }

        return await GetMaintenanceRecordByIdAsync(organizationId, DepartmentScope.Unrestricted, record.Id, cancellationToken);
    }

    public async Task<PagedResult<MaintenanceRecordDto>> ListMaintenanceRecordsAsync(
        Guid organizationId, DepartmentScope scope, MaintenanceRecordFilter filter, CancellationToken cancellationToken)
    {
        var query = _context.MaintenanceRecords
            .AsNoTracking()
            .Where(m => m.OrganizationId == organizationId)
            .ApplyScope(scope, m => m.Asset != null ? (Guid?)m.Asset.DepartmentId : null);

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

        var sorted = query.ApplySort(filter, SortMap, defaultSortKey: "createdat");
        var result = await sorted.ToPagedResultAsync(filter, ToDtoExpression, cancellationToken);

        await Task.WhenAll(result.Items.Select(dto => ResolvePhotoUrlAsync(dto, cancellationToken)));
        await ResolveAssetTypeNamesAsync(result.Items, cancellationToken);

        return result;
    }

    private static string ValidateCondition(string condition, string fieldName)
    {
        var normalized = condition.Trim().ToUpperInvariant();

        if (!ValidConditions.Contains(normalized))
        {
            throw new ValidationException(fieldName, $"Invalid condition '{condition}'. Use: NEW, GOOD, FAIR, POOR or UNSERVICEABLE.");
        }

        return normalized;
    }
}
