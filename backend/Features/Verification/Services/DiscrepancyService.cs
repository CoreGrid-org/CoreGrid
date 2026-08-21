using System.Text.Json;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Verification.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Verification.Services;

public class DiscrepancyService : IDiscrepancyService
{
    private readonly CoreGridDbContext _context;

    public DiscrepancyService(CoreGridDbContext context)
    {
        _context = context;
    }

    public async Task<List<DiscrepancyDto>> GetDiscrepanciesAsync(
        Guid organizationId,
        Guid? campaignId,
        bool onlyOpen)
    {
        var query = _context.Discrepancies
            .AsNoTracking()
            .Include(d => d.Asset)
            .Include(d => d.RaisedByUser)
            .Where(d => d.OrganizationId == organizationId);

        if (campaignId.HasValue)
        {
            query = query.Where(d => d.CampaignId == campaignId.Value);
        }

        if (onlyOpen)
        {
            query = query.Where(d => d.Status == DiscrepancyStatus.Open);
        }

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DiscrepancyDto
            {
                Id = d.Id,
                CampaignId = d.CampaignId,
                VerificationTaskId = d.VerificationTaskId,
                AssetId = d.AssetId,
                AssetCode = d.Asset != null ? d.Asset.AssetCode : string.Empty,
                Type = d.Type,
                IsAutomatic = d.IsAutomatic,
                RaisedByUserId = d.RaisedByUserId,
                RaisedByEmail = d.RaisedByUser != null ? d.RaisedByUser.Email : null,
                Description = d.Description,
                PhotoUrl = d.PhotoUrl,
                Status = d.Status,
                ResolutionType = d.ResolutionType,
                ResolutionExplanation = d.ResolutionExplanation,
                CorrectiveAction = d.CorrectiveAction,
                RegisterCorrected = d.RegisterCorrected,
                ResolvedByUserId = d.ResolvedByUserId,
                ResolvedAt = d.ResolvedAt,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();
    }

    // FR-061: manual discrepancy raising, for a condition the automatic
    // comparison (FR-060, run on task completion) can't detect — e.g.
    // Surplus or a free-form Data Mismatch.
    public async Task<DiscrepancyDto?> RaiseManualAsync(
        Guid organizationId,
        Guid taskId,
        Guid currentUserId,
        RaiseDiscrepancyRequest request)
    {
        var task = await _context.VerificationTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.OrganizationId == organizationId);

        if (task is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new InvalidOperationException("A description is required to raise a discrepancy.");
        }

        var discrepancy = new Discrepancy
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            CampaignId = task.CampaignId,
            VerificationTaskId = task.Id,
            AssetId = task.AssetId,
            Type = request.Type,
            IsAutomatic = false,
            RaisedByUserId = currentUserId,
            Description = request.Description.Trim(),
            PhotoUrl = request.PhotoUrl,
            Status = DiscrepancyStatus.Open,
            RegisterCorrected = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Discrepancies.Add(discrepancy);
        await _context.SaveChangesAsync();

        return (await GetDiscrepanciesAsync(organizationId, null, false))
            .FirstOrDefault(d => d.Id == discrepancy.Id);
    }

    // FR-062: resolve a discrepancy, optionally applying the correction to
    // the register. Only ConditionMismatch and LocationMismatch have a
    // single, unambiguous register field to correct — see
    // ResolveDiscrepancyRequest.ApplyCorrection.
    public async Task<DiscrepancyDto?> ResolveAsync(
        Guid organizationId,
        Guid discrepancyId,
        Guid currentUserId,
        ResolveDiscrepancyRequest request)
    {
        var discrepancy = await _context.Discrepancies
            .Include(d => d.VerificationTask)
            .Include(d => d.Asset)
            .FirstOrDefaultAsync(d => d.Id == discrepancyId && d.OrganizationId == organizationId);

        if (discrepancy is null)
        {
            return null;
        }

        if (discrepancy.Status != DiscrepancyStatus.Open)
        {
            throw new InvalidOperationException("This discrepancy has already been resolved.");
        }

        var resolutionType = request.ResolutionType?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!DiscrepancyResolutionTypes.All.Contains(resolutionType))
        {
            throw new InvalidOperationException(
                $"Resolution type must be one of: {string.Join(", ", DiscrepancyResolutionTypes.All)}.");
        }

        if (string.IsNullOrWhiteSpace(request.ResolutionExplanation))
        {
            throw new InvalidOperationException("A resolution explanation is required.");
        }

        // FR-062 AC3: NO_ACTION requires a justification of at least 20 characters.
        if (resolutionType == DiscrepancyResolutionTypes.NoAction
            && request.ResolutionExplanation.Trim().Length < DiscrepancyResolutionTypes.NoActionMinimumJustificationLength)
        {
            throw new InvalidOperationException(
                $"NO_ACTION requires a justification of at least {DiscrepancyResolutionTypes.NoActionMinimumJustificationLength} characters.");
        }

        // FR-062 BR2: WRITTEN_OFF requires the asset to have been verified
        // Missing in at least one completed verification.
        if (resolutionType == DiscrepancyResolutionTypes.WrittenOff)
        {
            var everVerifiedMissing = await _context.VerificationTasks.AsNoTracking().AnyAsync(
                t => t.AssetId == discrepancy.AssetId
                    && t.Status == VerificationTaskStatus.Completed
                    && t.AssertedPresent == false);

            if (!everVerifiedMissing)
            {
                throw new InvalidOperationException(
                    "WRITTEN_OFF requires the asset to have been verified Missing in at least one completed verification.");
            }
        }

        if (request.ApplyCorrection)
        {
            ApplyRegisterCorrection(discrepancy, currentUserId);
        }

        discrepancy.ResolutionType = resolutionType;
        discrepancy.ResolutionExplanation = request.ResolutionExplanation.Trim();
        discrepancy.CorrectiveAction = request.CorrectiveAction?.Trim();
        discrepancy.RegisterCorrected = request.ApplyCorrection;
        discrepancy.Status = DiscrepancyStatus.Resolved;
        discrepancy.ResolvedByUserId = currentUserId;
        discrepancy.ResolvedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return (await GetDiscrepanciesAsync(organizationId, null, false))
            .FirstOrDefault(d => d.Id == discrepancy.Id);
    }

    private void ApplyRegisterCorrection(Discrepancy discrepancy, Guid currentUserId)
    {
        var asset = discrepancy.Asset
            ?? throw new InvalidOperationException("The asset for this discrepancy could not be found.");
        var task = discrepancy.VerificationTask
            ?? throw new InvalidOperationException("The verification task for this discrepancy could not be found.");

        var now = DateTimeOffset.UtcNow;

        switch (discrepancy.Type)
        {
            case DiscrepancyType.ConditionMismatch:
            {
                if (string.IsNullOrEmpty(task.AssertedCondition))
                {
                    throw new InvalidOperationException(
                        "No asserted condition was recorded on the originating task to correct the register to.");
                }

                var previousCondition = asset.Condition;
                asset.Condition = task.AssertedCondition;
                asset.UpdatedAt = now;
                asset.UpdatedBy = currentUserId;

                _context.AssetHistoryEntries.Add(new AssetHistory
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = discrepancy.OrganizationId,
                    AssetId = asset.Id,
                    ActorUserId = currentUserId,
                    EventType = "VERIFICATION",
                    Description = $"Condition corrected from a resolved discrepancy ({discrepancy.Id}).",
                    PreviousValue = JsonSerializer.Serialize(new { condition = previousCondition }),
                    NewValue = JsonSerializer.Serialize(new { condition = asset.Condition }),
                    CreatedAt = now
                });
                break;
            }
            case DiscrepancyType.LocationMismatch:
            {
                if (task.AssertedLocationId is null)
                {
                    throw new InvalidOperationException(
                        "No asserted location was recorded on the originating task to correct the register to.");
                }

                var newLocationDepartmentId = _context.Locations
                    .AsNoTracking()
                    .Where(l => l.Id == task.AssertedLocationId.Value)
                    .Select(l => l.DepartmentId)
                    .FirstOrDefault();

                var previousLocationId = asset.LocationId;
                var previousDepartmentId = asset.DepartmentId;

                asset.LocationId = task.AssertedLocationId.Value;
                asset.DepartmentId = newLocationDepartmentId;
                asset.UpdatedAt = now;
                asset.UpdatedBy = currentUserId;

                _context.AssetHistoryEntries.Add(new AssetHistory
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = discrepancy.OrganizationId,
                    AssetId = asset.Id,
                    ActorUserId = currentUserId,
                    EventType = "VERIFICATION",
                    Description = $"Location corrected from a resolved discrepancy ({discrepancy.Id}).",
                    PreviousValue = JsonSerializer.Serialize(new { locationId = previousLocationId, departmentId = previousDepartmentId }),
                    NewValue = JsonSerializer.Serialize(new { locationId = asset.LocationId, departmentId = asset.DepartmentId }),
                    CreatedAt = now
                });
                break;
            }
            default:
                throw new InvalidOperationException(
                    "Register correction is only supported for condition and location mismatches.");
        }
    }
}
