using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Assets.Services;

public class AssetCategoryService : IAssetCategoryService
{
    private readonly CoreGridDbContext _context;

    public AssetCategoryService(CoreGridDbContext context)
    {
        _context = context;
    }

    public async Task<List<AssetCategoryDto>> GetCategoriesAsync(
        Guid organizationId)
    {
        return await _context.AssetCategories
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId)
            .OrderBy(c => c.Name)
            .Select(c => new AssetCategoryDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                IsActive = c.IsActive,

                TypeCount = c.AssetTypes.Count,

                AssetCount = c.AssetTypes
                    .SelectMany(t => t.Assets)
                    .Count()
            })
            .ToListAsync();
    }

    public async Task<AssetCategoryDto?> GetCategoryByIdAsync(
        Guid organizationId,
        Guid categoryId)
    {
        return await _context.AssetCategories
            .AsNoTracking()
            .Where(c =>
                c.OrganizationId == organizationId &&
                c.Id == categoryId)
            .Select(c => new AssetCategoryDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                IsActive = c.IsActive,

                TypeCount = c.AssetTypes.Count,

                AssetCount = c.AssetTypes
                    .SelectMany(t => t.Assets)
                    .Count()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AssetCategoryDto> CreateCategoryAsync(
        Guid organizationId,
        Guid? userId,
        CreateAssetCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new InvalidOperationException(
                "Category code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(
                "Category name is required.");
        }

        var code = request.Code.Trim().ToUpperInvariant();

        if (code.Length > 20)
        {
            throw new InvalidOperationException(
                "Category code cannot be longer than 20 characters.");
        }

        var codeInUse = await _context.AssetCategories
            .AsNoTracking()
            .AnyAsync(c =>
                c.OrganizationId == organizationId &&
                c.Code == code);

        if (codeInUse)
        {
            throw new InvalidOperationException(
                $"A category with code '{code}' already exists.");
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

        await _context.SaveChangesAsync();

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
        UpdateAssetCategoryRequest request)
    {
        var category = await _context.AssetCategories
            .FirstOrDefaultAsync(c =>
                c.Id == categoryId &&
                c.OrganizationId == organizationId);

        if (category is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new InvalidOperationException(
                "Category code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(
                "Category name is required.");
        }

        var code = request.Code.Trim().ToUpperInvariant();

        if (code.Length > 20)
        {
            throw new InvalidOperationException(
                "Category code cannot be longer than 20 characters.");
        }

        var codeInUse = await _context.AssetCategories
            .AsNoTracking()
            .AnyAsync(c =>
                c.OrganizationId == organizationId &&
                c.Code == code &&
                c.Id != categoryId);

        if (codeInUse)
        {
            throw new InvalidOperationException(
                $"A category with code '{code}' already exists.");
        }

        category.Code = code;
        category.Name = request.Name.Trim();
        category.UpdatedAt = DateTimeOffset.UtcNow;
        category.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        return await GetCategoryByIdAsync(organizationId, categoryId);
    }

    // Deletes the category if nothing references it; otherwise deactivates
    // it instead (existing AssetTypes/Assets that reference it keep working —
    // deactivation only hides it from pickers for creating new AssetTypes).
    public async Task<(bool Found, bool HardDeleted, AssetCategoryDto? Category)> DeleteCategoryAsync(
        Guid organizationId,
        Guid categoryId,
        Guid? userId)
    {
        var category = await _context.AssetCategories
            .FirstOrDefaultAsync(c =>
                c.Id == categoryId &&
                c.OrganizationId == organizationId);

        if (category is null)
        {
            return (false, false, null);
        }

        var referencedByAssetType = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(t => t.AssetCategoryId == categoryId);

        if (!referencedByAssetType)
        {
            _context.AssetCategories.Remove(category);
            await _context.SaveChangesAsync();
            return (true, true, null);
        }

        category.IsActive = false;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        category.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        var dto = await GetCategoryByIdAsync(organizationId, categoryId);
        return (true, false, dto);
    }

    public async Task<AssetCategoryDto?> SetCategoryActiveAsync(
        Guid organizationId,
        Guid categoryId,
        Guid? userId,
        bool isActive)
    {
        var category = await _context.AssetCategories
            .FirstOrDefaultAsync(c =>
                c.Id == categoryId &&
                c.OrganizationId == organizationId);

        if (category is null)
        {
            return null;
        }

        category.IsActive = isActive;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        category.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        return await GetCategoryByIdAsync(organizationId, categoryId);
    }
}