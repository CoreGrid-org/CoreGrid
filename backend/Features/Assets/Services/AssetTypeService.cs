using System.Linq.Expressions;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Assets.Services;

public class AssetTypeService : IAssetTypeService
{
    private static readonly string[] ValidDataTypes = ["TEXT", "NUMBER", "DATE", "BOOLEAN", "SELECT"];

    private static readonly Expression<Func<AssetType, AssetTypeDto>> ToTypeDtoExpression = t => new AssetTypeDto
    {
        Id = t.Id,
        Code = t.Code,
        Name = t.Name,
        AssetCategoryId = t.AssetCategoryId,
        CategoryName = t.AssetCategory != null ? t.AssetCategory.Name : string.Empty,
        CategoryCode = t.AssetCategory != null ? t.AssetCategory.Code : string.Empty,
        UsefulLifeYears = t.UsefulLifeYears,
        DefaultMaintenanceIntervalDays = t.DefaultMaintenanceIntervalDays,
        AttributeCount = t.AssetAttributeDefinitions.Count,
        IsActive = t.IsActive
    };

    private static readonly IReadOnlyDictionary<string, Expression<Func<AssetType, object?>>> TypeSortMap =
        new Dictionary<string, Expression<Func<AssetType, object?>>>
        {
            ["code"] = t => t.Code,
            ["name"] = t => t.Name,
        };

    // §5.3: the five inline AssetAttributeDefinitionDto initialisers
    // collapse into this one mapper.
    private static readonly Expression<Func<AssetAttributeDefinition, AssetAttributeDefinitionDto>> ToAttributeDtoExpression = d => new AssetAttributeDefinitionDto
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
    };

    private static readonly Func<AssetAttributeDefinition, AssetAttributeDefinitionDto> ToAttributeDto = ToAttributeDtoExpression.Compile();

    private readonly CoreGridDbContext _context;

    public AssetTypeService(CoreGridDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AssetTypeDto>> GetAssetTypesAsync(
        Guid organizationId,
        PagedQuery query,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var types = _context.AssetTypes
            .AsNoTracking()
            .Where(t => t.OrganizationId == organizationId);

        if (!includeInactive)
        {
            types = types.Where(t => t.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            types = types.Where(t => EF.Functions.ILike(t.Code, pattern) || EF.Functions.ILike(t.Name, pattern));
        }

        var sorted = types.ApplySort(query, TypeSortMap, defaultSortKey: "name");
        return await sorted.ToPagedResultAsync(query, ToTypeDtoExpression, cancellationToken);
    }

    public async Task<AssetTypeDto?> GetAssetTypeByIdAsync(
        Guid organizationId,
        Guid assetTypeId,
        CancellationToken cancellationToken)
    {
        return await _context.AssetTypes
            .AsNoTracking()
            .Where(t => t.OrganizationId == organizationId && t.Id == assetTypeId)
            .Select(ToTypeDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<AssetAttributeDefinitionDto>> GetAttributeDefinitionsAsync(
        Guid organizationId,
        Guid assetTypeId,
        CancellationToken cancellationToken)
    {
        var assetTypeExists = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(t => t.Id == assetTypeId && t.OrganizationId == organizationId, cancellationToken);

        if (!assetTypeExists)
        {
            return [];
        }

        return await _context.AssetAttributeDefinitions
            .AsNoTracking()
            .Where(d => d.AssetTypeId == assetTypeId)
            .OrderBy(d => d.DisplayOrder)
            .ThenBy(d => d.Name)
            .Select(ToAttributeDtoExpression)
            .ToListAsync(cancellationToken);
    }

    public async Task<AssetTypeDto> CreateAssetTypeAsync(
        Guid organizationId,
        Guid? userId,
        CreateAssetTypeRequest request,
        CancellationToken cancellationToken)
    {
        // [Required][Range] on the DTO makes a missing/invalid value 400 for
        // a model-bound HTTP caller before this method ever runs.
        var assetCategoryId = request.AssetCategoryId!.Value;
        var usefulLifeYears = request.UsefulLifeYears!.Value;
        var code = request.Code.Trim().ToUpperInvariant();

        var category = await _context.AssetCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == assetCategoryId && c.OrganizationId == organizationId, cancellationToken);

        if (category is null)
        {
            throw new ValidationException(nameof(request.AssetCategoryId), "Asset category was not found for this organization.");
        }

        var codeInUse = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(t => t.OrganizationId == organizationId && t.Code == code, cancellationToken);

        if (codeInUse)
        {
            throw new ConflictException($"An asset type with code '{code}' already exists.", "duplicate_code");
        }

        var now = DateTimeOffset.UtcNow;

        var assetType = new AssetType
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetCategoryId = assetCategoryId,
            Code = code,
            Name = request.Name.Trim(),
            UsefulLifeYears = usefulLifeYears,
            DefaultMaintenanceIntervalDays = request.DefaultMaintenanceIntervalDays,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        _context.AssetTypes.Add(assetType);
        await _context.SaveChangesAsync(cancellationToken);

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

    public async Task<AssetTypeDto?> UpdateAssetTypeAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId,
        UpdateAssetTypeRequest request,
        CancellationToken cancellationToken)
    {
        var assetType = await _context.AssetTypes
            .FirstOrDefaultAsync(t => t.Id == assetTypeId && t.OrganizationId == organizationId, cancellationToken);

        if (assetType is null)
        {
            return null;
        }

        var assetCategoryId = request.AssetCategoryId!.Value;
        var usefulLifeYears = request.UsefulLifeYears!.Value;
        var code = request.Code.Trim().ToUpperInvariant();

        var category = await _context.AssetCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == assetCategoryId && c.OrganizationId == organizationId, cancellationToken);

        if (category is null)
        {
            throw new ValidationException(nameof(request.AssetCategoryId), "Asset category was not found for this organization.");
        }

        var codeInUse = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(t => t.OrganizationId == organizationId && t.Code == code && t.Id != assetTypeId, cancellationToken);

        if (codeInUse)
        {
            throw new ConflictException($"An asset type with code '{code}' already exists.", "duplicate_code");
        }

        assetType.AssetCategoryId = assetCategoryId;
        assetType.Code = code;
        assetType.Name = request.Name.Trim();
        assetType.UsefulLifeYears = usefulLifeYears;
        assetType.DefaultMaintenanceIntervalDays = request.DefaultMaintenanceIntervalDays;
        assetType.UpdatedAt = DateTimeOffset.UtcNow;
        assetType.UpdatedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetAssetTypeByIdAsync(organizationId, assetTypeId, cancellationToken);
    }

    public async Task<AssetAttributeDefinitionDto?> CreateAttributeDefinitionAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId,
        CreateAssetAttributeDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var assetTypeExists = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(t => t.Id == assetTypeId && t.OrganizationId == organizationId, cancellationToken);

        if (!assetTypeExists)
        {
            return null;
        }

        var dataType = ValidateDataType(request.DataType, request.SelectOptions);
        var name = request.Name.Trim();

        var nameInUse = await _context.AssetAttributeDefinitions
            .AsNoTracking()
            .AnyAsync(d => d.AssetTypeId == assetTypeId && d.Name == name, cancellationToken);

        if (nameInUse)
        {
            throw new ConflictException($"An attribute named '{name}' already exists for this asset type.", "duplicate_name");
        }

        var displayOrder = request.DisplayOrder;

        if (!displayOrder.HasValue)
        {
            var maxOrder = await _context.AssetAttributeDefinitions
                .Where(d => d.AssetTypeId == assetTypeId)
                .Select(d => (int?)d.DisplayOrder)
                .MaxAsync(cancellationToken);

            displayOrder = (maxOrder ?? 0) + 1;
        }

        var now = DateTimeOffset.UtcNow;

        var definition = new AssetAttributeDefinition
        {
            Id = Guid.NewGuid(),
            AssetTypeId = assetTypeId,
            Name = name,
            DataType = dataType,
            IsRequired = request.IsRequired!.Value,
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
        await _context.SaveChangesAsync(cancellationToken);

        return ToAttributeDto(definition);
    }

    public async Task<AssetAttributeDefinitionDto?> UpdateAttributeDefinitionAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid attributeId,
        Guid? userId,
        UpdateAssetAttributeDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var assetTypeExists = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(t => t.Id == assetTypeId && t.OrganizationId == organizationId, cancellationToken);

        if (!assetTypeExists)
        {
            return null;
        }

        var definition = await _context.AssetAttributeDefinitions
            .FirstOrDefaultAsync(d => d.Id == attributeId && d.AssetTypeId == assetTypeId, cancellationToken);

        if (definition is null)
        {
            return null;
        }

        var dataType = ValidateDataType(request.DataType, request.SelectOptions);
        var name = request.Name.Trim();

        var nameInUse = await _context.AssetAttributeDefinitions
            .AsNoTracking()
            .AnyAsync(d => d.AssetTypeId == assetTypeId && d.Name == name && d.Id != attributeId, cancellationToken);

        if (nameInUse)
        {
            throw new ConflictException($"An attribute named '{name}' already exists for this asset type.", "duplicate_name");
        }

        definition.Name = name;
        definition.DataType = dataType;
        definition.IsRequired = request.IsRequired!.Value;
        definition.ValidationRule = request.ValidationRule;
        definition.SelectOptions = dataType == "SELECT" ? request.SelectOptions : null;
        definition.DisplayOrder = request.DisplayOrder!.Value;
        definition.UpdatedAt = DateTimeOffset.UtcNow;
        definition.UpdatedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return ToAttributeDto(definition);
    }

    // Hard-deletes the type (and its now-orphaned attribute definitions —
    // safe because zero Assets of this type means zero AssetAttributeValues
    // for any of its definitions either) if no Asset references it;
    // otherwise deactivates it instead so existing Assets keep working.
    public async Task<(bool Found, bool HardDeleted, AssetTypeDto? AssetType)> DeleteAssetTypeAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var assetType = await _context.AssetTypes
            .Include(t => t.AssetAttributeDefinitions)
            .FirstOrDefaultAsync(t => t.Id == assetTypeId && t.OrganizationId == organizationId, cancellationToken);

        if (assetType is null)
        {
            return (false, false, null);
        }

        var referencedByAsset = await _context.Assets
            .AsNoTracking()
            .AnyAsync(a => a.AssetTypeId == assetTypeId, cancellationToken);

        if (!referencedByAsset)
        {
            _context.AssetAttributeDefinitions.RemoveRange(assetType.AssetAttributeDefinitions);
            _context.AssetTypes.Remove(assetType);
            await _context.SaveChangesAsync(cancellationToken);
            return (true, true, null);
        }

        assetType.IsActive = false;
        assetType.UpdatedAt = DateTimeOffset.UtcNow;
        assetType.UpdatedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = await GetAssetTypeByIdAsync(organizationId, assetTypeId, cancellationToken);
        return (true, false, dto);
    }

    public async Task<AssetTypeDto?> SetAssetTypeActiveAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid? userId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var assetType = await _context.AssetTypes
            .FirstOrDefaultAsync(t => t.Id == assetTypeId && t.OrganizationId == organizationId, cancellationToken);

        if (assetType is null)
        {
            return null;
        }

        assetType.IsActive = isActive;
        assetType.UpdatedAt = DateTimeOffset.UtcNow;
        assetType.UpdatedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetAssetTypeByIdAsync(organizationId, assetTypeId, cancellationToken);
    }

    // Hard-deletes the definition if no AssetAttributeValue references it;
    // otherwise deactivates it instead so existing Assets keep displaying
    // their previously stored value for it.
    public async Task<(bool Found, bool HardDeleted, AssetAttributeDefinitionDto? Definition)> DeleteAttributeDefinitionAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid attributeId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var assetTypeExists = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(t => t.Id == assetTypeId && t.OrganizationId == organizationId, cancellationToken);

        if (!assetTypeExists)
        {
            return (false, false, null);
        }

        var definition = await _context.AssetAttributeDefinitions
            .FirstOrDefaultAsync(d => d.Id == attributeId && d.AssetTypeId == assetTypeId, cancellationToken);

        if (definition is null)
        {
            return (false, false, null);
        }

        var referencedByValue = await _context.AssetAttributeValues
            .AsNoTracking()
            .AnyAsync(v => v.AssetAttributeDefinitionId == attributeId, cancellationToken);

        if (!referencedByValue)
        {
            _context.AssetAttributeDefinitions.Remove(definition);
            await _context.SaveChangesAsync(cancellationToken);
            return (true, true, null);
        }

        definition.IsActive = false;
        definition.UpdatedAt = DateTimeOffset.UtcNow;
        definition.UpdatedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return (true, false, ToAttributeDto(definition));
    }

    public async Task<AssetAttributeDefinitionDto?> SetAttributeDefinitionActiveAsync(
        Guid organizationId,
        Guid assetTypeId,
        Guid attributeId,
        Guid? userId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var assetTypeExists = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(t => t.Id == assetTypeId && t.OrganizationId == organizationId, cancellationToken);

        if (!assetTypeExists)
        {
            return null;
        }

        var definition = await _context.AssetAttributeDefinitions
            .FirstOrDefaultAsync(d => d.Id == attributeId && d.AssetTypeId == assetTypeId, cancellationToken);

        if (definition is null)
        {
            return null;
        }

        definition.IsActive = isActive;
        definition.UpdatedAt = DateTimeOffset.UtcNow;
        definition.UpdatedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return ToAttributeDto(definition);
    }

    private static string ValidateDataType(string? rawDataType, List<string>? selectOptions)
    {
        var dataType = rawDataType?.Trim().ToUpperInvariant() ?? string.Empty;

        if (!ValidDataTypes.Contains(dataType))
        {
            throw new ValidationException(nameof(CreateAssetAttributeDefinitionRequest.DataType), $"Invalid attribute data type '{rawDataType}'.");
        }

        if (dataType == "SELECT" && (selectOptions is null || selectOptions.Count == 0))
        {
            throw new ValidationException(nameof(CreateAssetAttributeDefinitionRequest.SelectOptions), "SELECT attributes require at least one option.");
        }

        return dataType;
    }
}
