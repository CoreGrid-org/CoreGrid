using System.Linq.Expressions;
using System.Text.Json;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using CoreGrid.Api.Features.Shared.Storage;
using CoreGrid.Api.Features.Verification.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Verification.Services;

public class DiscrepancyService : IDiscrepancyService
{
    private static readonly Expression<Func<Discrepancy, DiscrepancyDto>> ToDtoExpression = d => new DiscrepancyDto
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
    };

    private readonly CoreGridDbContext _context;
    private readonly IFileStorageService? _storage;

    public DiscrepancyService(CoreGridDbContext context, IFileStorageService? storage = null)
    {
        _context = context;
        _storage = storage;
    }

    // Stored photo reference -> a URL the browser can show (fresh signed link
    // for a private key, older full URLs unchanged), never the raw key.
    private async Task<DiscrepancyDto> WithDisplayPhotoAsync(DiscrepancyDto dto, CancellationToken cancellationToken)
    {
        dto.PhotoUrl = _storage is null
            ? (dto.PhotoUrl?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true ? dto.PhotoUrl : null)
            : await PhotoKeys.ToDisplayUrlAsync(_storage, dto.PhotoUrl, cancellationToken);
        return dto;
    }

    public async Task<PagedResult<DiscrepancyDto>> GetDiscrepanciesAsync(
        Guid organizationId,
        DiscrepancyQueryParameters query,
        CancellationToken cancellationToken)
    {
        var discrepancies = _context.Discrepancies
            .AsNoTracking()
            .Where(d => d.OrganizationId == organizationId);

        if (query.CampaignId.HasValue)
        {
            discrepancies = discrepancies.Where(d => d.CampaignId == query.CampaignId.Value);
        }

        if (query.OnlyOpen)
        {
            discrepancies = discrepancies.Where(d => d.Status == DiscrepancyStatus.Open);
        }

        var ordered = discrepancies.OrderByDescending(d => d.CreatedAt);
        var page = await ordered.ToPagedResultAsync(query, ToDtoExpression, cancellationToken);
        foreach (var dto in page.Items) await WithDisplayPhotoAsync(dto, cancellationToken);
        return page;
    }

    // FR-061: manual discrepancy raising, for a condition the automatic
    // comparison (FR-060, run on task completion) can't detect — e.g.
    // Surplus or a free-form Data Mismatch.
    public async Task<DiscrepancyDto?> RaiseManualAsync(
        Guid organizationId,
        Guid taskId,
        Guid currentUserId,
        RaiseDiscrepancyRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _context.VerificationTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.OrganizationId == organizationId, cancellationToken);

        if (task is null)
        {
            return null;
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
            PhotoUrl = PhotoKeys.RequireOwn(PhotoKeys.Verification, request.PhotoUrl, organizationId, nameof(request.PhotoUrl)),
            Status = DiscrepancyStatus.Open,
            RegisterCorrected = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Discrepancies.Add(discrepancy);
        await _context.SaveChangesAsync(cancellationToken);

        // B18: a real single-row query, not GetDiscrepanciesAsync(...).FirstOrDefault(...).
        return await GetByIdAsync(organizationId, discrepancy.Id, cancellationToken);
    }

    // FR-062: resolve a discrepancy, optionally applying the correction to
    // the register. Only ConditionMismatch and LocationMismatch have a
    // single, unambiguous register field to correct — see
    // ResolveDiscrepancyRequest.ApplyCorrection.
    public async Task<DiscrepancyDto?> ResolveAsync(
        Guid organizationId,
        Guid discrepancyId,
        Guid currentUserId,
        ResolveDiscrepancyRequest request,
        CancellationToken cancellationToken)
    {
        var discrepancy = await _context.Discrepancies
            .Include(d => d.VerificationTask)
            .Include(d => d.Asset)
            .FirstOrDefaultAsync(d => d.Id == discrepancyId && d.OrganizationId == organizationId, cancellationToken);

        if (discrepancy is null)
        {
            return null;
        }

        if (discrepancy.Status != DiscrepancyStatus.Open)
        {
            throw new ConflictException("This discrepancy has already been resolved.", "already_resolved");
        }

        var resolutionType = request.ResolutionType.Trim().ToUpperInvariant();
        if (!DiscrepancyResolutionTypes.All.Contains(resolutionType))
        {
            throw new ValidationException(nameof(request.ResolutionType), $"Resolution type must be one of: {string.Join(", ", DiscrepancyResolutionTypes.All)}.");
        }

        // FR-062 AC3: NO_ACTION requires a justification of at least 20 characters.
        if (resolutionType == DiscrepancyResolutionTypes.NoAction
            && request.ResolutionExplanation.Trim().Length < DiscrepancyResolutionTypes.NoActionMinimumJustificationLength)
        {
            throw new ValidationException(
                nameof(request.ResolutionExplanation),
                $"NO_ACTION requires a justification of at least {DiscrepancyResolutionTypes.NoActionMinimumJustificationLength} characters.");
        }

        // FR-062 BR2: WRITTEN_OFF requires the asset to have been verified
        // Missing in at least one completed verification.
        if (resolutionType == DiscrepancyResolutionTypes.WrittenOff)
        {
            var everVerifiedMissing = await _context.VerificationTasks.AsNoTracking().AnyAsync(
                t => t.AssetId == discrepancy.AssetId
                    && t.Status == VerificationTaskStatus.Completed
                    && t.AssertedPresent == false,
                cancellationToken);

            if (!everVerifiedMissing)
            {
                throw new BusinessRuleException(
                    "WRITTEN_OFF requires the asset to have been verified Missing in at least one completed verification.",
                    "written_off_precondition_failed");
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

        await _context.SaveChangesAsync(cancellationToken);

        // B18: a real single-row query, not GetDiscrepanciesAsync(...).FirstOrDefault(...).
        return await GetByIdAsync(organizationId, discrepancy.Id, cancellationToken);
    }

    private async Task<DiscrepancyDto?> GetByIdAsync(Guid organizationId, Guid discrepancyId, CancellationToken cancellationToken)
    {
        var dto = await _context.Discrepancies
            .AsNoTracking()
            .Where(d => d.Id == discrepancyId && d.OrganizationId == organizationId)
            .Select(ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);
        return dto is null ? null : await WithDisplayPhotoAsync(dto, cancellationToken);
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
                    throw new BusinessRuleException(
                        "No asserted condition was recorded on the originating task to correct the register to.",
                        "no_asserted_condition");
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
                    EventType = AssetHistoryEventTypes.Verification,
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
                    throw new BusinessRuleException(
                        "No asserted location was recorded on the originating task to correct the register to.",
                        "no_asserted_location");
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
                    EventType = AssetHistoryEventTypes.Verification,
                    Description = $"Location corrected from a resolved discrepancy ({discrepancy.Id}).",
                    PreviousValue = JsonSerializer.Serialize(new { locationId = previousLocationId, departmentId = previousDepartmentId }),
                    NewValue = JsonSerializer.Serialize(new { locationId = asset.LocationId, departmentId = asset.DepartmentId }),
                    CreatedAt = now
                });
                break;
            }
            default:
                throw new BusinessRuleException(
                    "Register correction is only supported for condition and location mismatches.",
                    "correction_not_supported");
        }
    }
}
