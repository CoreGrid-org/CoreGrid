using System.Linq.Expressions;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Assets.Services;

public class AssetCategoryService : IAssetCategoryService
{
    private static readonly Expression<Func<AssetCategory, AssetCategoryDto>> ToDtoExpression = c => new AssetCategoryDto
    {
        Id = c.Id,
        Code = c.Code,
        Name = c.Name,
        IsActive = c.IsActive,
        TypeCount = c.AssetTypes.Count,
        AssetCount = c.AssetTypes.SelectMany(t => t.Assets).Count()
    };

    private static readonly IReadOnlyDictionary<string, Expression<Func<AssetCategory, object?>>> SortMap =
        new Dictionary<string, Expression<Func<AssetCategory, object?>>>
        {
            ["code"] = c => c.Code,
            ["name"] = c => c.Name,
        };

    private readonly CoreGridDbContext _context;

    public AssetCategoryService(CoreGridDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AssetCategoryDto>> GetCategoriesAsync(
        Guid organizationId,
        PagedQuery query,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var categories = _context.AssetCategories
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId);

        if (!includeInactive)
        {
            categories = categories.Where(c => c.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            categories = categories.Where(c => EF.Functions.ILike(c.Code, pattern) || EF.Functions.ILike(c.Name, pattern));
        }

        var sorted = categories.ApplySort(query, SortMap, defaultSortKey: "name");
        return await sorted.ToPagedResultAsync(query, ToDtoExpression, cancellationToken);
    }

    public async Task<AssetCategoryDto?> GetCategoryByIdAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return await _context.AssetCategories
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId && c.Id == categoryId)
            .Select(ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AssetCategoryDto> CreateCategoryAsync(
        Guid organizationId,
        Guid? userId,
        CreateAssetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        var codeInUse = await _context.AssetCategories
            .AsNoTracking()
            .AnyAsync(c => c.OrganizationId == organizationId && c.Code == code, cancellationToken);

        if (codeInUse)
        {
            throw new ConflictException($"A category with code '{code}' already exists.", "duplicate_code");
        }

        var now = DateTimeOffset.UtcNow;

        var category = new AssetCategory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Code = code,
            Name = request.Name.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        _context.AssetCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return new AssetCategoryDto
        {
            Id = category.Id,
            Code = category.Code,
            Name = category.Name,
            IsActive = category.IsActive,
            TypeCount = 0,
            AssetCount = 0
        };
    }

    public async Task<AssetCategoryDto?> UpdateCategoryAsync(
        Guid organizationId,
        Guid categoryId,
        Guid? userId,
        UpdateAssetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _context.AssetCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.OrganizationId == organizationId, cancellationToken);

        if (category is null)
        {
            return null;
        }

        var code = request.Code.Trim().ToUpperInvariant();

        var codeInUse = await _context.AssetCategories
            .AsNoTracking()
            .AnyAsync(c => c.OrganizationId == organizationId && c.Code == code && c.Id != categoryId, cancellationToken);

        if (codeInUse)
        {
            throw new ConflictException($"A category with code '{code}' already exists.", "duplicate_code");
        }

        category.Code = code;
        category.Name = request.Name.Trim();
        category.UpdatedAt = DateTimeOffset.UtcNow;
        category.UpdatedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetCategoryByIdAsync(organizationId, categoryId, cancellationToken);
    }

    // Deletes the category if nothing references it; otherwise deactivates
    // it instead (existing AssetTypes/Assets that reference it keep working —
    // deactivation only hides it from pickers for creating new AssetTypes).
    public async Task<(bool Found, bool HardDeleted, AssetCategoryDto? Category)> DeleteCategoryAsync(
        Guid organizationId,
        Guid categoryId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var category = await _context.AssetCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.OrganizationId == organizationId, cancellationToken);

        if (category is null)
        {
            return (false, false, null);
        }

        var referencedByAssetType = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(t => t.AssetCategoryId == categoryId, cancellationToken);

        if (!referencedByAssetType)
        {
            _context.AssetCategories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);
            return (true, true, null);
        }

        category.IsActive = false;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        category.UpdatedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = await GetCategoryByIdAsync(organizationId, categoryId, cancellationToken);
        return (true, false, dto);
    }

    public async Task<AssetCategoryDto?> SetCategoryActiveAsync(
        Guid organizationId,
        Guid categoryId,
        Guid? userId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var category = await _context.AssetCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.OrganizationId == organizationId, cancellationToken);

        if (category is null)
        {
            return null;
        }

        category.IsActive = isActive;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        category.UpdatedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetCategoryByIdAsync(organizationId, categoryId, cancellationToken);
    }
}
