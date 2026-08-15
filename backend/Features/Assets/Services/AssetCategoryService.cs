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
            TypeCount = 0,
            AssetCount = 0
        };
    }
}