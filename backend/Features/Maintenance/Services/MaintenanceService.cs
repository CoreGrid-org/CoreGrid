using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Maintenance.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Maintenance.Services;

public class MaintenanceService : IMaintenanceService
{
    private readonly CoreGridDbContext _context;

    public MaintenanceService(CoreGridDbContext context)
    {
        _context = context;
    }

    public async Task<MaintenanceRecordDto?> GetMaintenanceRecordByIdAsync(Guid organizationId, Guid id)
    {
        return await _context.MaintenanceRecords
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
                PhotoUrl = m.PhotoUrl,
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
            PhotoUrl = request.PhotoUrl,
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
            PhotoUrl = request.PhotoUrl,
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

        if (request.CompletionDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException(
                "Completion date cannot be in the future.");
        }

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
        record.WorkPerformed = request.WorkPerformed.Trim();
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

        return await GetMaintenanceRecordByIdAsync(organizationId, record.Id);
    }
}
