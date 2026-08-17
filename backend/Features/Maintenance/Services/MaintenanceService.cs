using System;
using System.Linq;
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

    // ------------------------------------------------------------------ //
    // FR-035 — Create maintenance record directly (Officer)               //
    // ------------------------------------------------------------------ //

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


    // FR-036 — Approve maintenance record (Officer / Administrator)       

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
}
