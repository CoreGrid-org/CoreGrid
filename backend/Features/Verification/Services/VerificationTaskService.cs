using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Verification.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Verification.Services;

public class VerificationTaskService : IVerificationTaskService
{
    private static readonly string[] ValidConditions = ["NEW", "GOOD", "FAIR", "POOR", "UNSERVICEABLE"];

    private readonly CoreGridDbContext _context;

    public VerificationTaskService(CoreGridDbContext context)
    {
        _context = context;
    }

    public async Task<List<VerificationTaskDto>> GetTasksAsync(
        Guid organizationId,
        Guid? campaignId,
        Guid? assignedToUserId,
        bool onlyPending)
    {
        var query = _context.VerificationTasks
            .AsNoTracking()
            .Include(t => t.Campaign)
            .Include(t => t.Asset)
            .Include(t => t.AssignedToUser)
            .Where(t => t.OrganizationId == organizationId);

        if (campaignId.HasValue)
        {
            query = query.Where(t => t.CampaignId == campaignId.Value);
        }

        if (assignedToUserId.HasValue)
        {
            query = query.Where(t => t.AssignedToUserId == assignedToUserId.Value);
        }

        if (onlyPending)
        {
            query = query.Where(t => t.Status == VerificationTaskStatus.Pending);
        }

        return await query
            .OrderBy(t => t.DueDate)
            .Select(t => new VerificationTaskDto
            {
                Id = t.Id,
                CampaignId = t.CampaignId,
                CampaignName = t.Campaign != null ? t.Campaign.Name : string.Empty,
                AssetId = t.AssetId,
                AssetCode = t.Asset != null ? t.Asset.AssetCode : string.Empty,
                AssetName = t.Asset != null ? t.Asset.Name : string.Empty,
                AssignedToUserId = t.AssignedToUserId,
                AssignedToEmail = t.AssignedToUser != null ? t.AssignedToUser.Email : null,
                DueDate = t.DueDate,
                Status = t.Status,
                AssertedPresent = t.AssertedPresent,
                AssertedLocationId = t.AssertedLocationId,
                AssertedCondition = t.AssertedCondition,
                CompletedAt = t.CompletedAt
            })
            .ToListAsync();
    }

    public async Task<VerificationTaskDto?> CompleteTaskAsync(
        Guid organizationId,
        Guid taskId,
        Guid currentUserId,
        bool currentUserCanActOnAnyTask,
        CompleteVerificationTaskRequest request)
    {
        var task = await _context.VerificationTasks
            .Include(t => t.Asset)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.OrganizationId == organizationId);

        if (task is null)
        {
            return null;
        }

        if (task.Asset is null)
        {
            throw new InvalidOperationException("The asset for this task could not be found.");
        }

        if (task.Status != VerificationTaskStatus.Pending)
        {
            throw new InvalidOperationException("This task has already been completed.");
        }

        if (!currentUserCanActOnAnyTask
            && task.AssignedToUserId.HasValue
            && task.AssignedToUserId.Value != currentUserId)
        {
            throw new InvalidOperationException("This task is assigned to a different officer.");
        }

        if (request.AssertedPresent)
        {
            if (request.AssertedLocationId is null || string.IsNullOrWhiteSpace(request.AssertedCondition))
            {
                throw new InvalidOperationException(
                    "Location and condition must be asserted when the asset is present.");
            }

            var normalizedCondition = request.AssertedCondition.Trim().ToUpperInvariant();
            if (!ValidConditions.Contains(normalizedCondition))
            {
                throw new InvalidOperationException(
                    $"Condition must be one of: {string.Join(", ", ValidConditions)}.");
            }

            var locationExists = await _context.Locations.AsNoTracking()
                .AnyAsync(l => l.Id == request.AssertedLocationId.Value && l.OrganizationId == organizationId);
            if (!locationExists)
            {
                throw new InvalidOperationException("Asserted location was not found.");
            }

            task.AssertedCondition = normalizedCondition;
            task.AssertedLocationId = request.AssertedLocationId;
        }

        task.AssertedPresent = request.AssertedPresent;
        task.Status = VerificationTaskStatus.Completed;
        task.CompletedByUserId = currentUserId;
        task.CompletedAt = DateTimeOffset.UtcNow;

        RaiseAutomaticDiscrepancies(task);

        await _context.SaveChangesAsync();

        return (await GetTasksAsync(organizationId, null, null, false))
            .FirstOrDefault(t => t.Id == taskId);
    }

    // FR-060: compares the officer's assertion against the register and
    // auto-raises a discrepancy per mismatch. Only the three classifications
    // derivable from FR-059's own assertions (presence, location, condition)
    // can be detected this way — Surplus (an unregistered asset found in the
    // field) and DataMismatch remain manual-only (FR-061), since neither is
    // representable from a task that's already tied to one known asset.
    private void RaiseAutomaticDiscrepancies(VerificationTask task)
    {
        var asset = task.Asset!;

        if (task.AssertedPresent == false)
        {
            AddDiscrepancy(task, DiscrepancyType.Missing,
                $"Automatic: asset '{asset.AssetCode}' was not found during verification.");
            return;
        }

        if (task.AssertedLocationId.HasValue && task.AssertedLocationId.Value != asset.LocationId)
        {
            AddDiscrepancy(task, DiscrepancyType.LocationMismatch,
                $"Automatic: register location does not match the asserted location.");
        }

        if (!string.IsNullOrEmpty(task.AssertedCondition) && task.AssertedCondition != asset.Condition)
        {
            AddDiscrepancy(task, DiscrepancyType.ConditionMismatch,
                $"Automatic: register condition '{asset.Condition}' does not match the asserted condition '{task.AssertedCondition}'.");
        }
    }

    private void AddDiscrepancy(VerificationTask task, DiscrepancyType type, string description)
    {
        _context.Discrepancies.Add(new Discrepancy
        {
            Id = Guid.NewGuid(),
            OrganizationId = task.OrganizationId,
            CampaignId = task.CampaignId,
            VerificationTaskId = task.Id,
            AssetId = task.AssetId,
            Type = type,
            IsAutomatic = true,
            RaisedByUserId = null,
            Description = description,
            Status = DiscrepancyStatus.Open,
            RegisterCorrected = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
