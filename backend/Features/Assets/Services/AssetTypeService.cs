using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Assets.Services;

public class AssetTypeService : IAssetTypeService
{
    private readonly CoreGridDbContext _context;

    private static readonly string[] ValidDataTypes =
    [
        "TEXT",
        "NUMBER",
        "DATE",
        "BOOLEAN",
        "SELECT"
    ];

    public AssetTypeService(CoreGridDbContext context)
    {
        _context = context;
    }

    public async Task<List<AssetTypeDto>> GetAssetTypesAsync(
        Guid organizationId)
    {
        return await _context.AssetTypes
            .AsNoTracking()
            .Where(t => t.OrganizationId == organizationId)
            .OrderBy(t => t.Name)
            .Select(t => new AssetTypeDto
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,

                AssetCategoryId = t.AssetCategoryId,

                CategoryName = t.AssetCategory != null
                    ? t.AssetCategory.Name
                    : string.Empty,

                CategoryCode = t.AssetCategory != null
                    ? t.AssetCategory.Code
                    : string.Empty,

                UsefulLifeYears = t.UsefulLifeYears,

                DefaultMaintenanceIntervalDays =
                    t.DefaultMaintenanceIntervalDays,

                AttributeCount =
                    t.AssetAttributeDefinitions.Count,

                IsActive = t.IsActive
            })
            .ToListAsync();
    }

    public async Task<AssetTypeDto?> GetAssetTypeByIdAsync(
        Guid organizationId,
        Guid assetTypeId)
    {
        return await _context.AssetTypes
            .AsNoTracking()
            .Where(t =>
                t.OrganizationId == organizationId &&
                t.Id == assetTypeId)
            .Select(t => new AssetTypeDto
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,

                AssetCategoryId = t.AssetCategoryId,

                CategoryName = t.AssetCategory != null
                    ? t.AssetCategory.Name
                    : string.Empty,

                CategoryCode = t.AssetCategory != null
                    ? t.AssetCategory.Code
                    : string.Empty,

                UsefulLifeYears = t.UsefulLifeYears,

                DefaultMaintenanceIntervalDays =
                    t.DefaultMaintenanceIntervalDays,

                AttributeCount =
                    t.AssetAttributeDefinitions.Count,

                IsActive = t.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<AssetAttributeDefinitionDto>>
        GetAttributeDefinitionsAsync(
            Guid organizationId,
            Guid assetTypeId)
    {
        // Make sure this asset type belongs to
        // the current organization.
        var assetTypeExists =
            await _context.AssetTypes
                .AsNoTracking()
                .AnyAsync(t =>
                    t.Id == assetTypeId &&
                    t.OrganizationId == organizationId);

        if (!assetTypeExists)
        {
            return [];
        }

        return await _context.AssetAttributeDefinitions
            .AsNoTracking()
            .Where(d => d.AssetTypeId == assetTypeId)
            .OrderBy(d => d.DisplayOrder)
            .ThenBy(d => d.Name)
            .Select(d => new AssetAttributeDefinitionDto
            {
                Id = d.Id,
                AssetTypeId = d.AssetTypeId,
                Name = d.Name,
                DataType = d.DataType,
                IsRequired = d.IsRequired,
                ValidationRule = d.ValidationRule,
                SelectOptions = d.SelectOptions,
                DisplayOrder = d.DisplayOrder,
                IsActive = d.IsActive
            })
            .ToListAsync();
    }

    // =========================================================
    // CREATE ASSET TYPE
    // =========================================================

    public async Task<AssetTypeDto> CreateAssetTypeAsync(
        Guid organizationId,
        Guid? userId,
        CreateAssetTypeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new InvalidOperationException(
                "Type code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(
                "Type name is required.");
        }

        if (request.UsefulLifeYears <= 0)
        {
            throw new InvalidOperationException(
                "Useful life (years) must be greater than zero.");
        }

        if (request.DefaultMaintenanceIntervalDays.HasValue &&
            request.DefaultMaintenanceIntervalDays.Value <= 0)
        {
            throw new InvalidOperationException(
                "Default maintenance interval must be greater than zero when provided.");
        }

        var code = request.Code.Trim().ToUpperInvariant();

        if (code.Length > 20)
        {
            throw new InvalidOperationException(
                "Type code cannot be longer than 20 characters.");
        }

        var category = await _context.AssetCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == request.AssetCategoryId &&
                c.OrganizationId == organizationId);

        if (category is null)
        {
            throw new InvalidOperationException(
                "Asset category was not found for this organization.");
        }

        var codeInUse = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(t =>
                t.OrganizationId == organizationId &&
                t.Code == code);

        if (codeInUse)
        {
            throw new InvalidOperationException(
                $"An asset type with code '{code}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;

        var assetType = new AssetType
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetCategoryId = request.AssetCategoryId,
            Code = code,
            Name = request.Name.Trim(),
            UsefulLifeYears = request.UsefulLifeYears,
            DefaultMaintenanceIntervalDays = request.DefaultMaintenanceIntervalDays,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        _context.AssetTypes.Add(assetType);

        await _context.SaveChangesAsync();

        return new AssetTypeDto
        {
            Id = assetType.Id,
            Code = assetType.Code,
            Name = assetType.Name,
            AssetCategoryId = assetType.AssetCategoryId,
            CategoryName = category.Name,
            CategoryCode = category.Code,
            UsefulLifeYears = assetType.UsefulLifeYears,
            DefaultMaintenanceIntervalDays = assetType.DefaultMaintenanceIntervalDays,
            AttributeCount = 0,
            IsActive = assetType.IsActive
        };
    }

    // =========================================================
    // UPDATE ASSET TYPE
    // =========================================================

    public async Task<AssetTypeDto?> UpdateAssetTypeAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId,
        UpdateAssetTypeRequest request)
    {
        var assetType = await _context.AssetTypes
            .FirstOrDefaultAsync(t =>
                t.Id == assetTypeId &&
                t.OrganizationId == organizationId);

        if (assetType is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new InvalidOperationException(
                "Type code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(
                "Type name is required.");
        }

        if (request.UsefulLifeYears <= 0)
        {
            throw new InvalidOperationException(
                "Useful life (years) must be greater than zero.");
        }

        if (request.DefaultMaintenanceIntervalDays.HasValue &&
            request.DefaultMaintenanceIntervalDays.Value <= 0)
        {
            throw new InvalidOperationException(
                "Default maintenance interval must be greater than zero when provided.");
        }

        var code = request.Code.Trim().ToUpperInvariant();

        if (code.Length > 20)
        {
            throw new InvalidOperationException(
                "Type code cannot be longer than 20 characters.");
        }

        var category = await _context.AssetCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == request.AssetCategoryId &&
                c.OrganizationId == organizationId);

        if (category is null)
        {
            throw new InvalidOperationException(
                "Asset category was not found for this organization.");
        }

        var codeInUse = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(t =>
                t.OrganizationId == organizationId &&
                t.Code == code &&
                t.Id != assetTypeId);

        if (codeInUse)
        {
            throw new InvalidOperationException(
                $"An asset type with code '{code}' already exists.");
        }

        assetType.AssetCategoryId = request.AssetCategoryId;
        assetType.Code = code;
        assetType.Name = request.Name.Trim();
        assetType.UsefulLifeYears = request.UsefulLifeYears;
        assetType.DefaultMaintenanceIntervalDays = request.DefaultMaintenanceIntervalDays;
        assetType.UpdatedAt = DateTimeOffset.UtcNow;
        assetType.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        return await GetAssetTypeByIdAsync(organizationId, assetTypeId);
    }

    // =========================================================
    // CREATE ATTRIBUTE DEFINITION
    // =========================================================

    public async Task<AssetAttributeDefinitionDto?> CreateAttributeDefinitionAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId,
        CreateAssetAttributeDefinitionRequest request)
    {
        var assetTypeExists = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(t =>
                t.Id == assetTypeId &&
                t.OrganizationId == organizationId);

        if (!assetTypeExists)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(
                "Attribute name is required.");
        }

        var dataType = request.DataType?.Trim().ToUpperInvariant() ?? string.Empty;

        if (!ValidDataTypes.Contains(dataType))
        {
            throw new InvalidOperationException(
                $"Invalid attribute data type '{request.DataType}'.");
        }

        if (dataType == "SELECT" &&
            (request.SelectOptions is null || request.SelectOptions.Count == 0))
        {
            throw new InvalidOperationException(
                "SELECT attributes require at least one option.");
        }

        var name = request.Name.Trim();

        var nameInUse = await _context.AssetAttributeDefinitions
            .AsNoTracking()
            .AnyAsync(d =>
                d.AssetTypeId == assetTypeId &&
                d.Name == name);

        if (nameInUse)
        {
            throw new InvalidOperationException(
                $"An attribute named '{name}' already exists for this asset type.");
        }

        var displayOrder = request.DisplayOrder;

        if (!displayOrder.HasValue)
        {
            var maxOrder = await _context.AssetAttributeDefinitions
                .Where(d => d.AssetTypeId == assetTypeId)
                .Select(d => (int?)d.DisplayOrder)
                .MaxAsync();

            displayOrder = (maxOrder ?? 0) + 1;
        }

        var now = DateTimeOffset.UtcNow;

        var definition = new AssetAttributeDefinition
        {
            Id = Guid.NewGuid(),
            AssetTypeId = assetTypeId,
            Name = name,
            DataType = dataType,
            IsRequired = request.IsRequired,
            ValidationRule = request.ValidationRule,
            SelectOptions = dataType == "SELECT" ? request.SelectOptions : null,
            DisplayOrder = displayOrder.Value,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        _context.AssetAttributeDefinitions.Add(definition);

        await _context.SaveChangesAsync();

        return new AssetAttributeDefinitionDto
        {
            Id = definition.Id,
            AssetTypeId = definition.AssetTypeId,
            Name = definition.Name,
            DataType = definition.DataType,
            IsRequired = definition.IsRequired,
            ValidationRule = definition.ValidationRule,
            SelectOptions = definition.SelectOptions,
            DisplayOrder = definition.DisplayOrder,
            IsActive = definition.IsActive
        };
    }

    // =========================================================
    // UPDATE ATTRIBUTE DEFINITION
    // =========================================================

    public async Task<AssetAttributeDefinitionDto?> UpdateAttributeDefinitionAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid attributeId,
        Guid? userId,
        UpdateAssetAttributeDefinitionRequest request)
    {
        var assetTypeExists = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(t =>
                t.Id == assetTypeId &&
                t.OrganizationId == organizationId);

        if (!assetTypeExists)
        {
            return null;
        }

        var definition = await _context.AssetAttributeDefinitions
            .FirstOrDefaultAsync(d =>
                d.Id == attributeId &&
                d.AssetTypeId == assetTypeId);

        if (definition is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(
                "Attribute name is required.");
        }

        var dataType = request.DataType?.Trim().ToUpperInvariant() ?? string.Empty;

        if (!ValidDataTypes.Contains(dataType))
        {
            throw new InvalidOperationException(
                $"Invalid attribute data type '{request.DataType}'.");
        }

        if (dataType == "SELECT" &&
            (request.SelectOptions is null || request.SelectOptions.Count == 0))
        {
            throw new InvalidOperationException(
                "SELECT attributes require at least one option.");
        }

        var name = request.Name.Trim();

        var nameInUse = await _context.AssetAttributeDefinitions
            .AsNoTracking()
            .AnyAsync(d =>
                d.AssetTypeId == assetTypeId &&
                d.Name == name &&
                d.Id != attributeId);

        if (nameInUse)
        {
            throw new InvalidOperationException(
                $"An attribute named '{name}' already exists for this asset type.");
        }

        definition.Name = name;
        definition.DataType = dataType;
        definition.IsRequired = request.IsRequired;
        definition.ValidationRule = request.ValidationRule;
        definition.SelectOptions = dataType == "SELECT" ? request.SelectOptions : null;
        definition.DisplayOrder = request.DisplayOrder;
        definition.UpdatedAt = DateTimeOffset.UtcNow;
        definition.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        return new AssetAttributeDefinitionDto
        {
            Id = definition.Id,
            AssetTypeId = definition.AssetTypeId,
            Name = definition.Name,
            DataType = definition.DataType,
            IsRequired = definition.IsRequired,
            ValidationRule = definition.ValidationRule,
            SelectOptions = definition.SelectOptions,
            DisplayOrder = definition.DisplayOrder,
            IsActive = definition.IsActive
        };
    }

    // =========================================================
    // DELETE / REACTIVATE ASSET TYPE
    // =========================================================

    // Hard-deletes the type (and its now-orphaned attribute definitions —
    // safe because zero Assets of this type means zero AssetAttributeValues
    // for any of its definitions either) if no Asset references it;
    // otherwise deactivates it instead so existing Assets keep working.
    public async Task<(bool Found, bool HardDeleted, AssetTypeDto? AssetType)> DeleteAssetTypeAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId)
    {
        var assetType = await _context.AssetTypes
            .Include(t => t.AssetAttributeDefinitions)
            .FirstOrDefaultAsync(t =>
                t.Id == assetTypeId &&
                t.OrganizationId == organizationId);

        if (assetType is null)
        {
            return (false, false, null);
        }

        var referencedByAsset = await _context.Assets
            .AsNoTracking()
            .AnyAsync(a => a.AssetTypeId == assetTypeId);

        if (!referencedByAsset)
        {
            _context.AssetAttributeDefinitions.RemoveRange(assetType.AssetAttributeDefinitions);
            _context.AssetTypes.Remove(assetType);
            await _context.SaveChangesAsync();
            return (true, true, null);
        }

        assetType.IsActive = false;
        assetType.UpdatedAt = DateTimeOffset.UtcNow;
        assetType.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        var dto = await GetAssetTypeByIdAsync(organizationId, assetTypeId);
        return (true, false, dto);
    }

    public async Task<AssetTypeDto?> SetAssetTypeActiveAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId,
        bool isActive)
    {
        var assetType = await _context.AssetTypes
            .FirstOrDefaultAsync(t =>
                t.Id == assetTypeId &&
                t.OrganizationId == organizationId);

        if (assetType is null)
        {
            return null;
        }

        assetType.IsActive = isActive;
        assetType.UpdatedAt = DateTimeOffset.UtcNow;
        assetType.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        return await GetAssetTypeByIdAsync(organizationId, assetTypeId);
    }

    // =========================================================
    // DELETE / REACTIVATE ATTRIBUTE DEFINITION
    // =========================================================

    // Hard-deletes the definition if no AssetAttributeValue references it;
    // otherwise deactivates it instead so existing Assets keep displaying
    // their previously stored value for it.
    public async Task<(bool Found, bool HardDeleted, AssetAttributeDefinitionDto? Definition)> DeleteAttributeDefinitionAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid attributeId,
        Guid? userId)
    {
        var assetTypeExists = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(t =>
                t.Id == assetTypeId &&
                t.OrganizationId == organizationId);

        if (!assetTypeExists)
        {
            return (false, false, null);
        }

        var definition = await _context.AssetAttributeDefinitions
            .FirstOrDefaultAsync(d =>
                d.Id == attributeId &&
                d.AssetTypeId == assetTypeId);

        if (definition is null)
        {
            return (false, false, null);
        }

        var referencedByValue = await _context.AssetAttributeValues
            .AsNoTracking()
            .AnyAsync(v => v.AssetAttributeDefinitionId == attributeId);

        if (!referencedByValue)
        {
            _context.AssetAttributeDefinitions.Remove(definition);
            await _context.SaveChangesAsync();
            return (true, true, null);
        }

        definition.IsActive = false;
        definition.UpdatedAt = DateTimeOffset.UtcNow;
        definition.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        return (true, false, new AssetAttributeDefinitionDto
        {
            Id = definition.Id,
            AssetTypeId = definition.AssetTypeId,
            Name = definition.Name,
            DataType = definition.DataType,
            IsRequired = definition.IsRequired,
            ValidationRule = definition.ValidationRule,
            SelectOptions = definition.SelectOptions,
            DisplayOrder = definition.DisplayOrder,
            IsActive = definition.IsActive
        });
    }

    public async Task<AssetAttributeDefinitionDto?> SetAttributeDefinitionActiveAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid attributeId,
        Guid? userId,
        bool isActive)
    {
        var assetTypeExists = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(t =>
                t.Id == assetTypeId &&
                t.OrganizationId == organizationId);

        if (!assetTypeExists)
        {
            return null;
        }

        var definition = await _context.AssetAttributeDefinitions
            .FirstOrDefaultAsync(d =>
                d.Id == attributeId &&
                d.AssetTypeId == assetTypeId);

        if (definition is null)
        {
            return null;
        }

        definition.IsActive = isActive;
        definition.UpdatedAt = DateTimeOffset.UtcNow;
        definition.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        return new AssetAttributeDefinitionDto
        {
            Id = definition.Id,
            AssetTypeId = definition.AssetTypeId,
            Name = definition.Name,
            DataType = definition.DataType,
            IsRequired = definition.IsRequired,
            ValidationRule = definition.ValidationRule,
            SelectOptions = definition.SelectOptions,
            DisplayOrder = definition.DisplayOrder,
            IsActive = definition.IsActive
        };
    }
}