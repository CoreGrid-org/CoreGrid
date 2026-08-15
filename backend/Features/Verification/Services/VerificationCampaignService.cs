using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Verification.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Verification.Services;

public class VerificationCampaignService : IVerificationCampaignService
{
    private readonly CoreGridDbContext _context;

    public VerificationCampaignService(CoreGridDbContext context)
    {
        _context = context;
    }

    public async Task<List<CampaignDto>> GetCampaignsAsync(Guid organizationId)
    {
        var campaigns = await _context.VerificationCampaigns
            .AsNoTracking()
            .Include(c => c.ScopeDepartment)
            .Include(c => c.ScopeLocation)
            .Include(c => c.ScopeAssetCategory)
            .Include(c => c.ScopeAssetType)
            .Where(c => c.OrganizationId == organizationId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

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
            .ToDictionaryAsync(x => x.CampaignId);

        var openDiscrepancyCounts = await _context.Discrepancies
            .AsNoTracking()
            .Where(d => campaignIds.Contains(d.CampaignId) && d.Status == DiscrepancyStatus.Open)
            .GroupBy(d => d.CampaignId)
            .Select(g => new { CampaignId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CampaignId, x => x.Count);

        return campaigns.Select(c => new CampaignDto
        {
            Id = c.Id,
            Name = c.Name,
            PeriodStart = c.PeriodStart,
            PeriodEnd = c.PeriodEnd,
            ScopeDepartmentId = c.ScopeDepartmentId,
            ScopeDepartmentName = c.ScopeDepartment?.Name,
            ScopeLocationId = c.ScopeLocationId,
            ScopeLocationName = c.ScopeLocation?.Name,
            ScopeAssetCategoryId = c.ScopeAssetCategoryId,
            ScopeAssetCategoryName = c.ScopeAssetCategory?.Name,
            ScopeAssetTypeId = c.ScopeAssetTypeId,
            ScopeAssetTypeName = c.ScopeAssetType?.Name,
            Status = c.Status,
            TaskCount = taskCounts.TryGetValue(c.Id, out var tc) ? tc.Total : 0,
            CompletedTaskCount = taskCounts.TryGetValue(c.Id, out var tc2) ? tc2.Completed : 0,
            OpenDiscrepancyCount = openDiscrepancyCounts.GetValueOrDefault(c.Id),
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<CampaignDto?> GetCampaignByIdAsync(Guid organizationId, Guid id)
    {
        var campaigns = await GetCampaignsAsync(organizationId);
        return campaigns.FirstOrDefault(c => c.Id == id);
    }

    public async Task<CampaignDto> CreateCampaignAsync(
        Guid organizationId,
        Guid userId,
        CreateCampaignRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Campaign name is required.");
        }

        if (request.PeriodEnd < request.PeriodStart)
        {
            throw new InvalidOperationException("Campaign period end cannot be before its start.");
        }

        await ValidateScopeAsync(organizationId, request);

        var campaign = new VerificationCampaign
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            ScopeDepartmentId = request.ScopeDepartmentId,
            ScopeLocationId = request.ScopeLocationId,
            ScopeAssetCategoryId = request.ScopeAssetCategoryId,
            ScopeAssetTypeId = request.ScopeAssetTypeId,
            Status = CampaignStatus.Active,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.VerificationCampaigns.Add(campaign);

        await GenerateTasksAsync(campaign);

        await _context.SaveChangesAsync();

        return await GetCampaignByIdAsync(organizationId, campaign.Id)
            ?? throw new InvalidOperationException("Campaign could not be reloaded after creation.");
    }

    private async Task ValidateScopeAsync(Guid organizationId, CreateCampaignRequest request)
    {
        if (request.ScopeDepartmentId.HasValue)
        {
            var exists = await _context.Departments.AsNoTracking()
                .AnyAsync(d => d.Id == request.ScopeDepartmentId.Value && d.OrganizationId == organizationId);
            if (!exists) throw new InvalidOperationException("Scope department was not found.");
        }

        if (request.ScopeLocationId.HasValue)
        {
            var exists = await _context.Locations.AsNoTracking()
                .AnyAsync(l => l.Id == request.ScopeLocationId.Value && l.OrganizationId == organizationId);
            if (!exists) throw new InvalidOperationException("Scope location was not found.");
        }

        if (request.ScopeAssetCategoryId.HasValue)
        {
            var exists = await _context.AssetCategories.AsNoTracking()
                .AnyAsync(c => c.Id == request.ScopeAssetCategoryId.Value && c.OrganizationId == organizationId);
            if (!exists) throw new InvalidOperationException("Scope asset category was not found.");
        }

        if (request.ScopeAssetTypeId.HasValue)
        {
            var exists = await _context.AssetTypes.AsNoTracking()
                .AnyAsync(t => t.Id == request.ScopeAssetTypeId.Value && t.OrganizationId == organizationId);
            if (!exists) throw new InvalidOperationException("Scope asset type was not found.");
        }
    }

    // FR-057: generates one task per in-scope asset and assigns each to the
    // officer responsible for it. The schema has no location-ownership
    // concept, so "responsible officer" is interpreted as the first active
    // InventoryOfficer in the asset's own Department — tasks for a
    // department with no InventoryOfficer are created unassigned rather
    // than silently dropped, so an Administrator can still see and
    // reassign them.
    private async Task GenerateTasksAsync(VerificationCampaign campaign)
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
            .ToListAsync();

        var officersByDepartment = await _context.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == campaign.OrganizationId
                && u.Role == CoreGridRole.InventoryOfficer
                && u.IsActive
                && u.DepartmentId != null)
            .OrderBy(u => u.CreatedAt)
            .GroupBy(u => u.DepartmentId!.Value)
            .ToDictionaryAsync(g => g.Key, g => g.First().Id);

        var now = DateTimeOffset.UtcNow;

        foreach (var asset in assets)
        {
            _context.VerificationTasks.Add(new VerificationTask
            {
                Id = Guid.NewGuid(),
                OrganizationId = campaign.OrganizationId,
                CampaignId = campaign.Id,
                AssetId = asset.Id,
                AssignedToUserId = officersByDepartment.GetValueOrDefault(asset.DepartmentId),
                DueDate = campaign.PeriodEnd,
                Status = VerificationTaskStatus.Pending,
                CreatedAt = now
            });
        }
    }
}
