using System.Linq.Expressions;
using System.Text.Json;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using CoreGrid.Api.Features.Verification.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Verification.Services;

public class VerificationTaskService : IVerificationTaskService
{
    private static readonly string[] ValidConditions = ["NEW", "GOOD", "FAIR", "POOR", "UNSERVICEABLE"];

    private static readonly Expression<Func<VerificationTask, VerificationTaskDto>> ToDtoExpression = t => new VerificationTaskDto
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
        AssertedLocationName = t.AssertedLocation != null ? t.AssertedLocation.Name : null,
        AssertedCondition = t.AssertedCondition,
        CompletedAt = t.CompletedAt
    };

    private static readonly IReadOnlyDictionary<string, Expression<Func<VerificationTask, object?>>> SortMap =
        new Dictionary<string, Expression<Func<VerificationTask, object?>>>
        {
            ["duedate"] = t => t.DueDate,
            ["status"] = t => t.Status,
        };

    private readonly CoreGridDbContext _context;

    public VerificationTaskService(CoreGridDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<VerificationTaskDto>> GetTasksAsync(
        Guid organizationId,
        Guid? assignedToUserId,
        VerificationTaskQueryParameters query,
        CancellationToken cancellationToken)
    {
        var tasks = _context.VerificationTasks
            .AsNoTracking()
            .Where(t => t.OrganizationId == organizationId);

        if (query.CampaignId.HasValue)
        {
            tasks = tasks.Where(t => t.CampaignId == query.CampaignId.Value);
        }

        if (assignedToUserId.HasValue)
        {
            tasks = tasks.Where(t => t.AssignedToUserId == assignedToUserId.Value);
        }

        if (query.OnlyPending)
        {
            tasks = tasks.Where(t => t.Status == VerificationTaskStatus.Pending);
        }

        var sorted = tasks.ApplySort(query, SortMap, defaultSortKey: "duedate");
        return await sorted.ToPagedResultAsync(query, ToDtoExpression, cancellationToken);
    }

    public async Task<VerificationTaskDto?> CompleteTaskAsync(
        Guid organizationId,
        Guid taskId,
        Guid currentUserId,
        bool currentUserCanActOnAnyTask,
        CompleteVerificationTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _context.VerificationTasks
            .Include(t => t.Asset)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.OrganizationId == organizationId, cancellationToken);

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
            throw new ConflictException("This task has already been completed.", "already_completed");
        }

        if (!currentUserCanActOnAnyTask
            && task.AssignedToUserId.HasValue
            && task.AssignedToUserId.Value != currentUserId)
        {
            throw new ForbiddenException("This task is assigned to a different officer.");
        }

        // [Required] on the DTO makes a missing value 400 for a
        // model-bound HTTP caller before this method ever runs.
        var assertedPresent = request.AssertedPresent!.Value;

        if (assertedPresent)
        {
            if (request.AssertedLocationId is null || string.IsNullOrWhiteSpace(request.AssertedCondition))
            {
                throw new ValidationException("Attributes", "Location and condition must be asserted when the asset is present.");
            }

            var normalizedCondition = request.AssertedCondition.Trim().ToUpperInvariant();
            if (!ValidConditions.Contains(normalizedCondition))
            {
                throw new ValidationException(nameof(request.AssertedCondition), $"Condition must be one of: {string.Join(", ", ValidConditions)}.");
            }

            var locationExists = await _context.Locations.AsNoTracking()
                .AnyAsync(l => l.Id == request.AssertedLocationId.Value && l.OrganizationId == organizationId, cancellationToken);
            if (!locationExists)
            {
                throw new ValidationException(nameof(request.AssertedLocationId), "Asserted location was not found.");
            }

            task.AssertedCondition = normalizedCondition;
            task.AssertedLocationId = request.AssertedLocationId;
        }

        var now = DateTimeOffset.UtcNow;

        task.AssertedPresent = assertedPresent;
        task.Status = VerificationTaskStatus.Completed;
        task.CompletedByUserId = currentUserId;
        task.CompletedAt = now;

        RaiseAutomaticDiscrepancies(task);

        // FR-027 (B12): lifecycle history for the verification itself,
        // independent of any auto-raised discrepancy.
        _context.AssetHistoryEntries.Add(new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = task.AssetId,
            ActorUserId = currentUserId,
            EventType = "VERIFICATION",
            Description = assertedPresent
                ? $"Verified present at completion of task {task.Id}."
                : $"Verified NOT present at completion of task {task.Id}.",
            PreviousValue = null,
            NewValue = JsonSerializer.Serialize(new
            {
                assertedPresent,
                assertedLocationId = task.AssertedLocationId,
                assertedCondition = task.AssertedCondition
            }),
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);

        // B18: a real single-row query, not GetTasksAsync(...).FirstOrDefault(...).
        return await _context.VerificationTasks
            .AsNoTracking()
            .Where(t => t.Id == taskId && t.OrganizationId == organizationId)
            .Select(ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);
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
