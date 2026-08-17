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
}
