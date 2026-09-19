using System.Linq.Expressions;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using CoreGrid.Api.Features.Verification.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Verification.Services;

public class VerificationCampaignService : IVerificationCampaignService
{
    private static readonly Expression<Func<VerificationCampaign, CampaignDto>> ToDtoExpression = c => new CampaignDto
    {
        Id = c.Id,
        Name = c.Name,
        PeriodStart = c.PeriodStart,
        PeriodEnd = c.PeriodEnd,
        ScopeDepartmentId = c.ScopeDepartmentId,
        ScopeDepartmentName = c.ScopeDepartment != null ? c.ScopeDepartment.Name : null,
        ScopeLocationId = c.ScopeLocationId,
        ScopeLocationName = c.ScopeLocation != null ? c.ScopeLocation.Name : null,
        ScopeAssetCategoryId = c.ScopeAssetCategoryId,
        ScopeAssetCategoryName = c.ScopeAssetCategory != null ? c.ScopeAssetCategory.Name : null,
        ScopeAssetTypeId = c.ScopeAssetTypeId,
        ScopeAssetTypeName = c.ScopeAssetType != null ? c.ScopeAssetType.Name : null,
        Status = c.Status,
        TaskCount = 0,
        CompletedTaskCount = 0,
        OpenDiscrepancyCount = 0,
        CreatedAt = c.CreatedAt
    };

    private static readonly IReadOnlyDictionary<string, Expression<Func<VerificationCampaign, object?>>> SortMap =
        new Dictionary<string, Expression<Func<VerificationCampaign, object?>>>
        {
            ["name"] = c => c.Name,
            ["createdat"] = c => c.CreatedAt,
            ["periodstart"] = c => c.PeriodStart,
            ["periodend"] = c => c.PeriodEnd,
        };

    private readonly CoreGridDbContext _context;

    public VerificationCampaignService(CoreGridDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<CampaignDto>> GetCampaignsAsync(Guid organizationId, CampaignQueryParameters query, CancellationToken cancellationToken)
    {
        var campaignsQuery = _context.VerificationCampaigns
            .AsNoTracking()
            .Include(c => c.ScopeDepartment)
            .Include(c => c.ScopeLocation)
            .Include(c => c.ScopeAssetCategory)
            .Include(c => c.ScopeAssetType)
            .Where(c => c.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            campaignsQuery = campaignsQuery.Where(c => EF.Functions.ILike(c.Name, pattern));
        }

        var sorted = campaignsQuery.ApplySort(query, SortMap, defaultSortKey: "createdat");
        var result = await sorted.ToPagedResultAsync(query, ToDtoExpression, cancellationToken);

        await AttachCountsAsync(result.Items, cancellationToken);
        return result;
    }

    // B18: a real single-row query, not GetCampaignsAsync(...).FirstOrDefault(...).
    public async Task<CampaignDto?> GetCampaignByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var dto = await _context.VerificationCampaigns
            .AsNoTracking()
            .Where(c => c.Id == id && c.OrganizationId == organizationId)
            .Select(ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            return null;
        }

        await AttachCountsAsync([dto], cancellationToken);
        return dto;
    }

    // Batch-loads task/discrepancy counts for exactly the campaigns handed
    // in — one page's worth (or a single campaign), never the whole org.
    private async Task AttachCountsAsync(IReadOnlyList<CampaignDto> campaigns, CancellationToken cancellationToken)
    {
        if (campaigns.Count == 0)
        {
            return;
        }

        var campaignIds = campaigns.Select(c => c.Id).ToList();

        var taskCounts = await _context.VerificationTasks
            .AsNoTracking()
            .Where(t => campaignIds.Contains(t.CampaignId))
            .GroupBy(t => t.CampaignId)
            .Select(g => new
            {
                CampaignId = g.Key,
                Total = g.Count(),
                Completed = g.Count(t => t.Status == VerificationTaskStatus.Completed)
            })
            .ToDictionaryAsync(x => x.CampaignId, cancellationToken);

        var openDiscrepancyCounts = await _context.Discrepancies
            .AsNoTracking()
            .Where(d => campaignIds.Contains(d.CampaignId) && d.Status == DiscrepancyStatus.Open)
            .GroupBy(d => d.CampaignId)
            .Select(g => new { CampaignId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CampaignId, x => x.Count, cancellationToken);

        foreach (var campaign in campaigns)
        {
            if (taskCounts.TryGetValue(campaign.Id, out var counts))
            {
                campaign.TaskCount = counts.Total;
                campaign.CompletedTaskCount = counts.Completed;
            }

            campaign.OpenDiscrepancyCount = openDiscrepancyCounts.GetValueOrDefault(campaign.Id);
        }
    }

    public async Task<CampaignDto> CreateCampaignAsync(
        Guid organizationId,
        Guid userId,
        CreateCampaignRequest request,
        CancellationToken cancellationToken)
    {
        // [Required] on the DTO makes a missing value 400 for a
        // model-bound HTTP caller before this method ever runs.
        var periodStart = request.PeriodStart!.Value;
        var periodEnd = request.PeriodEnd!.Value;

        if (periodEnd < periodStart)
        {
            throw new ValidationException(nameof(request.PeriodEnd), "Campaign period end cannot be before its start.");
        }

        await ValidateScopeAsync(organizationId, request, cancellationToken);

        var campaign = new VerificationCampaign
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            ScopeDepartmentId = request.ScopeDepartmentId,
            ScopeLocationId = request.ScopeLocationId,
            ScopeAssetCategoryId = request.ScopeAssetCategoryId,
            ScopeAssetTypeId = request.ScopeAssetTypeId,
            Status = CampaignStatus.Active,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.VerificationCampaigns.Add(campaign);

        await GenerateTasksAsync(campaign, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetCampaignByIdAsync(organizationId, campaign.Id, cancellationToken)
            ?? throw new InvalidOperationException("Campaign could not be reloaded after creation.");
    }

    public async Task<CampaignDto?> UpdateCampaignAsync(
        Guid organizationId,
        Guid id,
        UpdateCampaignRequest request,
        CancellationToken cancellationToken)
    {
        // [Required] on the DTO makes a missing value 400 for a
        // model-bound HTTP caller before this method ever runs.
        var periodStart = request.PeriodStart!.Value;
        var periodEnd = request.PeriodEnd!.Value;
        var status = request.Status!.Value;

        if (periodEnd < periodStart)
        {
            throw new ValidationException(nameof(request.PeriodEnd), "Campaign period end cannot be before its start.");
        }

        var campaign = await _context.VerificationCampaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId, cancellationToken);

        if (campaign is null)
        {
            return null;
        }

        campaign.Name = request.Name.Trim();
        campaign.PeriodStart = periodStart;
        campaign.PeriodEnd = periodEnd;
        campaign.Status = status;

        var pendingTasks = await _context.VerificationTasks
            .Where(t => t.CampaignId == id && t.OrganizationId == organizationId && t.Status == VerificationTaskStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var task in pendingTasks)
        {
            task.DueDate = periodEnd;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetCampaignByIdAsync(organizationId, campaign.Id, cancellationToken);
    }

    // §5.7: one transaction (was three separate SaveChanges calls).
    public async Task<bool> DeleteCampaignAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var campaign = await _context.VerificationCampaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId, cancellationToken);

        if (campaign is null)
        {
            return false;
        }

        var taskIds = await _context.VerificationTasks
            .Where(t => t.CampaignId == id && t.OrganizationId == organizationId)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var discrepancies = await _context.Discrepancies
            .Where(d => d.CampaignId == id || taskIds.Contains(d.VerificationTaskId))
            .ToListAsync(cancellationToken);

        var tasks = await _context.VerificationTasks
            .Where(t => t.CampaignId == id && t.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        _context.Discrepancies.RemoveRange(discrepancies);
        _context.VerificationTasks.RemoveRange(tasks);
        _context.VerificationCampaigns.Remove(campaign);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task ValidateScopeAsync(Guid organizationId, CreateCampaignRequest request, CancellationToken cancellationToken)
    {
        if (request.ScopeDepartmentId.HasValue)
        {
            var exists = await _context.Departments.AsNoTracking()
                .AnyAsync(d => d.Id == request.ScopeDepartmentId.Value && d.OrganizationId == organizationId, cancellationToken);
            if (!exists) throw new ValidationException(nameof(request.ScopeDepartmentId), "Scope department was not found.");
        }

        if (request.ScopeLocationId.HasValue)
        {
            var exists = await _context.Locations.AsNoTracking()
                .AnyAsync(l => l.Id == request.ScopeLocationId.Value && l.OrganizationId == organizationId, cancellationToken);
            if (!exists) throw new ValidationException(nameof(request.ScopeLocationId), "Scope location was not found.");
        }

        if (request.ScopeAssetCategoryId.HasValue)
        {
            var exists = await _context.AssetCategories.AsNoTracking()
                .AnyAsync(c => c.Id == request.ScopeAssetCategoryId.Value && c.OrganizationId == organizationId, cancellationToken);
            if (!exists) throw new ValidationException(nameof(request.ScopeAssetCategoryId), "Scope asset category was not found.");
        }

        if (request.ScopeAssetTypeId.HasValue)
        {
            var exists = await _context.AssetTypes.AsNoTracking()
                .AnyAsync(t => t.Id == request.ScopeAssetTypeId.Value && t.OrganizationId == organizationId, cancellationToken);
            if (!exists) throw new ValidationException(nameof(request.ScopeAssetTypeId), "Scope asset type was not found.");
        }
    }

    // FR-057: generates one task per in-scope asset and assigns each to the
    // officer responsible for it. The schema has no location-ownership
    // concept, so "responsible officer" is interpreted as the first active
    // InventoryOfficer in the asset's own Department — tasks for a
    // department with no InventoryOfficer are created unassigned rather
    // than silently dropped, so an Administrator can still see and
    // reassign them.
    private async Task GenerateTasksAsync(VerificationCampaign campaign, CancellationToken cancellationToken)
    {
        var assetsQuery = _context.Assets
            .AsNoTracking()
            .Where(a => a.OrganizationId == campaign.OrganizationId && a.Status != "DISPOSED");

        if (campaign.ScopeDepartmentId.HasValue)
        {
            assetsQuery = assetsQuery.Where(a => a.DepartmentId == campaign.ScopeDepartmentId.Value);
        }

        if (campaign.ScopeLocationId.HasValue)
        {
            assetsQuery = assetsQuery.Where(a => a.LocationId == campaign.ScopeLocationId.Value);
        }

        if (campaign.ScopeAssetTypeId.HasValue)
        {
            assetsQuery = assetsQuery.Where(a => a.AssetTypeId == campaign.ScopeAssetTypeId.Value);
        }

        if (campaign.ScopeAssetCategoryId.HasValue)
        {
            assetsQuery = assetsQuery.Where(a => a.AssetType != null && a.AssetType.AssetCategoryId == campaign.ScopeAssetCategoryId.Value);
        }

        var assets = await assetsQuery
            .Select(a => new { a.Id, a.DepartmentId })
            .ToListAsync(cancellationToken);

        var officersByDepartment = await _context.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == campaign.OrganizationId
                && u.Role == CoreGridRole.InventoryOfficer
                && u.IsActive
                && u.DepartmentId != null)
            .OrderBy(u => u.CreatedAt)
            .GroupBy(u => u.DepartmentId!.Value)
            .ToDictionaryAsync(g => g.Key, g => g.First().Id, cancellationToken);

        var defaultOfficerId = await _context.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == campaign.OrganizationId
                && u.Role == CoreGridRole.InventoryOfficer
                && u.IsActive)
            .OrderBy(u => u.CreatedAt)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        foreach (var asset in assets)
        {
            var assignedUserId = officersByDepartment.TryGetValue(asset.DepartmentId, out var deptOfficerId)
                ? (Guid?)deptOfficerId
                : defaultOfficerId;

            _context.VerificationTasks.Add(new VerificationTask
            {
                Id = Guid.NewGuid(),
                OrganizationId = campaign.OrganizationId,
                CampaignId = campaign.Id,
                AssetId = asset.Id,
                AssignedToUserId = assignedUserId,
                DueDate = campaign.PeriodEnd,
                Status = VerificationTaskStatus.Pending,
                CreatedAt = now
            });
        }
    }
}
