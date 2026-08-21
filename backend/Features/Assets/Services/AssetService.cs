using System.Globalization;
using System.Text.Json;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Assets.Helpers;
using CoreGrid.Api.Features.Shared;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Assets.Services;

public class AssetService : IAssetService
{
    private readonly CoreGridDbContext _context;

    private static readonly string[] ValidConditions =
    [
        "NEW",
        "GOOD",
        "FAIR",
        "POOR",
        "UNSERVICEABLE"
    ];

    public AssetService(CoreGridDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // 1. GET ASSETS
    // Search + Filter + Sort + Pagination
    // =========================================================

    public async Task<PagedResult<AssetDto>> GetAssetsAsync(
        Guid organizationId,
        AssetQueryParameters parameters)
    {
        var page = parameters.Page < 1
            ? 1
            : parameters.Page;

        var pageSize = parameters.PageSize < 1
            ? 20
            : Math.Min(parameters.PageSize, 100);

        var query = _context.Assets
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId);

        // -------------------------
        // Search (universal — asset code/name, category, type, and
        // dynamic attribute values all match a single search term)
        // -------------------------

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim();
            var pattern = $"%{search}%";

            var searchAsNumber = decimal.TryParse(search, out var parsedNumber)
                ? parsedNumber
                : (decimal?)null;

            var searchAsDate = DateOnly.TryParse(search, out var parsedDate)
                ? parsedDate
                : (DateOnly?)null;

            query = query.Where(a =>
                EF.Functions.ILike(a.AssetCode, pattern) ||
                EF.Functions.ILike(a.Name, pattern) ||
                (a.AssetType != null && EF.Functions.ILike(a.AssetType.Name, pattern)) ||
                (a.AssetType != null && EF.Functions.ILike(a.AssetType.Code, pattern)) ||
                (a.AssetType != null && a.AssetType.AssetCategory != null &&
                    EF.Functions.ILike(a.AssetType.AssetCategory.Name, pattern)) ||
                (a.AssetType != null && a.AssetType.AssetCategory != null &&
                    EF.Functions.ILike(a.AssetType.AssetCategory.Code, pattern)) ||
                a.AssetAttributeValues.Any(v =>
                    (v.ValueText != null && EF.Functions.ILike(v.ValueText, pattern)) ||
                    (searchAsNumber.HasValue && v.ValueNumber == searchAsNumber.Value) ||
                    (searchAsDate.HasValue && v.ValueDate == searchAsDate.Value) ||
                    (v.AssetAttributeDefinition != null &&
                        EF.Functions.ILike(v.AssetAttributeDefinition.Name, pattern))));
        }

        // -------------------------
        // Filters
        // -------------------------

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(a =>
                a.AssetType != null &&
                a.AssetType.AssetCategoryId == parameters.CategoryId.Value);
        }

        if (parameters.AssetTypeId.HasValue)
        {
            query = query.Where(a =>
                a.AssetTypeId == parameters.AssetTypeId.Value);
        }

        if (parameters.DepartmentId.HasValue)
        {
            query = query.Where(a =>
                a.DepartmentId == parameters.DepartmentId.Value);
        }

        if (parameters.LocationId.HasValue)
        {
            query = query.Where(a =>
                a.LocationId == parameters.LocationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = parameters.Status
                .Trim()
                .ToUpperInvariant();

            query = query.Where(a =>
                a.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Condition))
        {
            var condition = parameters.Condition
                .Trim()
                .ToUpperInvariant();

            query = query.Where(a =>
                a.Condition == condition);
        }

        // Count before pagination
        var totalCount = await query.CountAsync();

        // -------------------------
        // Sorting
        // -------------------------

        var sortBy = parameters.SortBy?
            .Trim()
            .ToLowerInvariant() ?? "name";

        var descending = string.Equals(
            parameters.SortDirection,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        query = sortBy switch
        {
            "assetcode" => descending
                ? query.OrderByDescending(a => a.AssetCode)
                : query.OrderBy(a => a.AssetCode),

            "name" => descending
                ? query.OrderByDescending(a => a.Name)
                : query.OrderBy(a => a.Name),

            "status" => descending
                ? query.OrderByDescending(a => a.Status)
                : query.OrderBy(a => a.Status),

            "condition" => descending
                ? query.OrderByDescending(a => a.Condition)
                : query.OrderBy(a => a.Condition),

            "acquisitiondate" => descending
                ? query.OrderByDescending(a => a.AcquisitionDate)
                : query.OrderBy(a => a.AcquisitionDate),

            "acquisitioncost" => descending
                ? query.OrderByDescending(a => a.AcquisitionCost)
                : query.OrderBy(a => a.AcquisitionCost),

            _ => query.OrderBy(a => a.Name)
        };

        // -------------------------
        // Pagination + DTO
        // -------------------------

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AssetDto
            {
                Id = a.Id,

                AssetCode = a.AssetCode,
                Name = a.Name,

                AssetTypeId = a.AssetTypeId,
                AssetTypeName = a.AssetType != null
                    ? a.AssetType.Name
                    : string.Empty,

                DepartmentId = a.DepartmentId,
                DepartmentName = a.Department != null
                    ? a.Department.Name
                    : string.Empty,

                LocationId = a.LocationId,
                LocationName = a.Location != null
                    ? a.Location.Name
                    : string.Empty,

                Status = a.Status,
                Condition = a.Condition,

                AcquisitionDate = a.AcquisitionDate,
                AcquisitionCost = a.AcquisitionCost,

                QrPayload = a.QrPayload
            })
            .ToListAsync();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<AssetDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    // =========================================================
    // 2. GET ASSET BY ID
    // Asset Detail + Dynamic Attributes
    // =========================================================

    public async Task<AssetDetailDto?> GetAssetByIdAsync(
        Guid organizationId,
        Guid assetId)
    {
        return await _context.Assets
            .AsNoTracking()
            .Where(a =>
                a.OrganizationId == organizationId &&
                a.Id == assetId)
            .Select(a => new AssetDetailDto
            {
                Id = a.Id,

                AssetCode = a.AssetCode,
                Name = a.Name,

                AssetTypeId = a.AssetTypeId,
                AssetTypeName = a.AssetType != null
                    ? a.AssetType.Name
                    : string.Empty,

                DepartmentId = a.DepartmentId,
                DepartmentName = a.Department != null
                    ? a.Department.Name
                    : string.Empty,

                LocationId = a.LocationId,
                LocationName = a.Location != null
                    ? a.Location.Name
                    : string.Empty,

                Status = a.Status,
                Condition = a.Condition,

                AcquisitionDate = a.AcquisitionDate,
                AcquisitionCost = a.AcquisitionCost,
                ResidualValue = a.ResidualValue,

                QrPayload = a.QrPayload,

                Attributes = a.AssetAttributeValues
                    .OrderBy(v =>
                        v.AssetAttributeDefinition != null
                            ? v.AssetAttributeDefinition.DisplayOrder
                            : 0)
                    .Select(v => new AssetAttributeValueDto
                    {
                        AttributeDefinitionId =
                            v.AssetAttributeDefinitionId,

                        Name = v.AssetAttributeDefinition != null
                            ? v.AssetAttributeDefinition.Name
                            : string.Empty,

                        DataType = v.AssetAttributeDefinition != null
                            ? v.AssetAttributeDefinition.DataType
                            : string.Empty,

                        IsRequired =
                            v.AssetAttributeDefinition != null &&
                            v.AssetAttributeDefinition.IsRequired,

                        ValueText = v.ValueText,
                        ValueNumber = v.ValueNumber,
                        ValueDate = v.ValueDate,
                        ValueBoolean = v.ValueBoolean
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    // =========================================================
    // 3. CREATE ASSET
    // =========================================================

    public async Task<AssetDetailDto> CreateAssetAsync(
        Guid organizationId,
        Guid? userId,
        CreateAssetRequest request)
    {
        // -------------------------
        // Basic validation
        // -------------------------

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(
                "Asset name is required.");
        }

        if (request.AcquisitionCost < 0)
        {
            throw new InvalidOperationException(
                "Acquisition cost cannot be negative.");
        }

        if (request.ResidualValue < 0)
        {
            throw new InvalidOperationException(
                "Residual value cannot be negative.");
        }

        // -------------------------
        // Validate Asset Type
        // -------------------------

        var assetType = await _context.AssetTypes
            .AsNoTracking()
            .Include(at => at.AssetCategory)
            .FirstOrDefaultAsync(at =>
                at.Id == request.AssetTypeId &&
                at.OrganizationId == organizationId);

        if (assetType is null)
        {
            throw new InvalidOperationException(
                "Asset type was not found for this organization.");
        }

        // -------------------------
        // Validate Department
        // -------------------------

        var departmentExists = await _context.Departments
            .AsNoTracking()
            .AnyAsync(d =>
                d.Id == request.DepartmentId &&
                d.OrganizationId == organizationId &&
                d.IsActive);

        if (!departmentExists)
        {
            throw new InvalidOperationException(
                "Department was not found or is inactive.");
        }

        // -------------------------
        // Validate Location
        // -------------------------

        var location = await _context.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(l =>
                l.Id == request.LocationId &&
                l.OrganizationId == organizationId &&
                l.IsActive);

        if (location is null)
        {
            throw new InvalidOperationException(
                "Location was not found or is inactive.");
        }

        if (location.DepartmentId != request.DepartmentId)
        {
            throw new InvalidOperationException(
                "Selected location does not belong to the selected department.");
        }

        // -------------------------
        // Validate Condition
        // -------------------------

        var condition = NormalizeCondition(request.Condition);

        // -------------------------
        // Load Attribute Definitions
        // -------------------------

        var attributeDefinitions =
            await _context.AssetAttributeDefinitions
                .AsNoTracking()
                .Where(a =>
                    a.AssetTypeId == request.AssetTypeId)
                .ToListAsync();

        ValidateAttributes(
            attributeDefinitions,
            request.Attributes);

        // -------------------------
        // Generate Asset Code
        // -------------------------

        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        var organizationCode = OrganizationCodeGenerator.Generate(
            organization?.Name ?? "ORG");

        var existingCount = await _context.Assets
            .CountAsync(a =>
                a.OrganizationId == organizationId &&
                a.AssetTypeId == request.AssetTypeId);

        var nextSequence = existingCount + 1;

        var categoryCode = assetType.AssetCategory?.Code ?? "GEN";

        var assetCode = AssetCodeGenerator.Generate(
            organizationCode,
            categoryCode,
            assetType.Code,
            nextSequence);

        // Safety check
        while (await _context.Assets.AnyAsync(a =>
                   a.OrganizationId == organizationId &&
                   a.AssetCode == assetCode))
        {
            nextSequence++;

            assetCode = AssetCodeGenerator.Generate(
                organizationCode,
                categoryCode,
                assetType.Code,
                nextSequence);
        }

        var now = DateTimeOffset.UtcNow;

        // -------------------------
        // Create Asset
        // -------------------------

        var asset = new Asset
        {
            Id = Guid.NewGuid(),

            OrganizationId = organizationId,

            AssetTypeId = request.AssetTypeId,
            DepartmentId = request.DepartmentId,
            LocationId = request.LocationId,

            AssetCode = assetCode,
            Name = request.Name.Trim(),

            Status = "ACTIVE",
            Condition = condition,

            AcquisitionDate = request.AcquisitionDate,
            AcquisitionCost = request.AcquisitionCost,
            ResidualValue = request.ResidualValue,

            CumulativeMaintenanceCost = 0,
            RepairCount = 0,

            QrPayload = assetCode,

            CreatedAt = now,
            UpdatedAt = now,

            CreatedBy = userId,
            UpdatedBy = userId
        };

        // -------------------------
        // Dynamic Attribute Values
        // -------------------------

        foreach (var requestValue in request.Attributes)
        {
            asset.AssetAttributeValues.Add(
                CreateAttributeValue(
                    asset.Id,
                    requestValue,
                    now,
                    userId));
        }

        _context.Assets.Add(asset);

        _context.AssetHistoryEntries.Add(new AssetHistory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetId = asset.Id,
            ActorUserId = userId,
            EventType = AssetHistoryEventTypes.StatusChange,
            Description = "Asset registered.",
            PreviousValue = null,
            NewValue = JsonSerializer.Serialize(new { status = asset.Status }),
            CreatedAt = now
        });

        await _context.SaveChangesAsync();

        return await GetAssetByIdAsync(
                   organizationId,
                   asset.Id)
               ?? throw new InvalidOperationException(
                   "Asset was created but could not be retrieved.");
    }

    // =========================================================
    // 4. UPDATE ASSET
    // =========================================================

    public async Task<AssetDetailDto?> UpdateAssetAsync(
        Guid organizationId,
        Guid assetId,
        Guid? userId,
        UpdateAssetRequest request)
    {
        var asset = await _context.Assets
            .Include(a => a.AssetAttributeValues)
            .FirstOrDefaultAsync(a =>
                a.Id == assetId &&
                a.OrganizationId == organizationId);

        if (asset is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(
                "Asset name is required.");
        }

        if (request.AcquisitionCost < 0)
        {
            throw new InvalidOperationException(
                "Acquisition cost cannot be negative.");
        }

        if (request.ResidualValue < 0)
        {
            throw new InvalidOperationException(
                "Residual value cannot be negative.");
        }

        // -------------------------
        // Validate Asset Type
        // -------------------------

        var assetTypeExists = await _context.AssetTypes
            .AsNoTracking()
            .AnyAsync(at =>
                at.Id == request.AssetTypeId &&
                at.OrganizationId == organizationId);

        if (!assetTypeExists)
        {
            throw new InvalidOperationException(
                "Asset type was not found for this organization.");
        }

        // -------------------------
        // Validate Department
        // -------------------------

        var departmentExists = await _context.Departments
            .AsNoTracking()
            .AnyAsync(d =>
                d.Id == request.DepartmentId &&
                d.OrganizationId == organizationId &&
                d.IsActive);

        if (!departmentExists)
        {
            throw new InvalidOperationException(
                "Department was not found or is inactive.");
        }

        // -------------------------
        // Validate Location
        // -------------------------

        var location = await _context.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(l =>
                l.Id == request.LocationId &&
                l.OrganizationId == organizationId &&
                l.IsActive);

        if (location is null)
        {
            throw new InvalidOperationException(
                "Location was not found or is inactive.");
        }

        if (location.DepartmentId != request.DepartmentId)
        {
            throw new InvalidOperationException(
                "Selected location does not belong to the selected department.");
        }

        // -------------------------
        // Load new Asset Type attributes
        // -------------------------

        var attributeDefinitions =
            await _context.AssetAttributeDefinitions
                .AsNoTracking()
                .Where(a =>
                    a.AssetTypeId == request.AssetTypeId)
                .ToListAsync();

        ValidateAttributes(
            attributeDefinitions,
            request.Attributes);

        var now = DateTimeOffset.UtcNow;

        // -------------------------
        // Snapshot "before" state for AssetHistory (FR-026)
        // -------------------------

        var previousFields = new AssetFieldSnapshot(
            asset.AssetTypeId,
            asset.DepartmentId,
            asset.LocationId,
            asset.Name,
            asset.AcquisitionDate,
            asset.AcquisitionCost,
            asset.ResidualValue);

        var previousAttributes = BuildAttributeSnapshot(
            asset.AssetAttributeValues);

        // -------------------------
        // Update basic information
        // -------------------------

        asset.AssetTypeId = request.AssetTypeId;
        asset.DepartmentId = request.DepartmentId;
        asset.LocationId = request.LocationId;

        asset.Name = request.Name.Trim();

        asset.AcquisitionDate = request.AcquisitionDate;
        asset.AcquisitionCost = request.AcquisitionCost;
        asset.ResidualValue = request.ResidualValue;

        asset.UpdatedAt = now;
        asset.UpdatedBy = userId;

        // -------------------------
        // Replace dynamic values
        // -------------------------

        _context.AssetAttributeValues.RemoveRange(
            asset.AssetAttributeValues);

        var newAttributeValues = new List<AssetAttributeValue>();

        foreach (var requestValue in request.Attributes)
        {
            var attributeValue = CreateAttributeValue(
                asset.Id,
                requestValue,
                now,
                userId);

            newAttributeValues.Add(attributeValue);

            _context.AssetAttributeValues.Add(attributeValue);
        }

        // -------------------------
        // Record what changed in AssetHistory (FR-026)
        // -------------------------

        var newFields = new AssetFieldSnapshot(
            asset.AssetTypeId,
            asset.DepartmentId,
            asset.LocationId,
            asset.Name,
            asset.AcquisitionDate,
            asset.AcquisitionCost,
            asset.ResidualValue);

        var newAttributes = BuildAttributeSnapshot(newAttributeValues);

        var amendment = DiffAssetFields(
            previousFields,
            newFields,
            previousAttributes,
            newAttributes);

        if (amendment is not null)
        {
            _context.AssetHistoryEntries.Add(new AssetHistory
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                AssetId = asset.Id,
                ActorUserId = userId,
                EventType = AssetHistoryEventTypes.FieldAmendment,
                Description = amendment.Value.Description,
                PreviousValue = JsonSerializer.Serialize(amendment.Value.Previous),
                NewValue = JsonSerializer.Serialize(amendment.Value.New),
                CreatedAt = now
            });
        }

        await _context.SaveChangesAsync();

        return await GetAssetByIdAsync(
            organizationId,
            asset.Id);
    }

    // =========================================================
    // 5. UPDATE CONDITION
    // =========================================================

    public async Task<bool> UpdateConditionAsync(
        Guid organizationId,
        Guid assetId,
        Guid? userId,
        UpdateAssetConditionRequest request)
    {
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a =>
                a.Id == assetId &&
                a.OrganizationId == organizationId);

        if (asset is null)
        {
            return false;
        }

        var condition = NormalizeCondition(
            request.Condition);

        var previousCondition = asset.Condition;
        var now = DateTimeOffset.UtcNow;

        asset.Condition = condition;
        asset.UpdatedAt = now;
        asset.UpdatedBy = userId;

        if (!string.Equals(previousCondition, condition, StringComparison.Ordinal))
        {
            _context.AssetHistoryEntries.Add(new AssetHistory
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                AssetId = asset.Id,
                ActorUserId = userId,
                EventType = AssetHistoryEventTypes.FieldAmendment,
                Description = $"Condition changed from {previousCondition} to {condition}.",
                PreviousValue = JsonSerializer.Serialize(new { condition = previousCondition }),
                NewValue = JsonSerializer.Serialize(new { condition }),
                CreatedAt = now
            });
        }

        await _context.SaveChangesAsync();

        return true;
    }

    // =========================================================
    // 6. GET ASSET BY QR / ASSET CODE
    // =========================================================

    public async Task<AssetDetailDto?> GetAssetByQrCodeAsync(
        Guid organizationId,
        string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalizedCode = code.Trim();

        var assetId = await _context.Assets
            .AsNoTracking()
            .Where(a =>
                a.OrganizationId == organizationId &&
                (a.AssetCode == normalizedCode ||
                 a.QrPayload == normalizedCode))
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync();

        if (!assetId.HasValue)
        {
            return null;
        }

        return await GetAssetByIdAsync(
            organizationId,
            assetId.Value);
    }

    // =========================================================
    // ORGANIZATION CODE
    // =========================================================

    public async Task<string> GetOrganizationCodeAsync(Guid organizationId)
    {
        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        return OrganizationCodeGenerator.Generate(organization?.Name ?? "ORG");
    }

    // =========================================================
    // 7. GET ASSET HISTORY
    // =========================================================

    public async Task<PagedResult<AssetHistoryDto>?> GetAssetHistoryAsync(
        Guid organizationId,
        Guid assetId,
        AssetHistoryQueryParameters parameters)
    {
        var assetExists = await _context.Assets
            .AsNoTracking()
            .AnyAsync(a =>
                a.Id == assetId &&
                a.OrganizationId == organizationId);

        if (!assetExists)
        {
            return null;
        }

        var page = parameters.Page < 1
            ? 1
            : parameters.Page;

        var pageSize = parameters.PageSize is < 1 or > 100
            ? 20
            : parameters.PageSize;

        var query = _context.AssetHistoryEntries
            .AsNoTracking()
            .Where(h =>
                h.AssetId == assetId &&
                h.OrganizationId == organizationId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new AssetHistoryDto
            {
                Id = h.Id,
                AssetId = h.AssetId,

                ActorUserId = h.ActorUserId,
                ActorEmail = h.ActorUser != null
                    ? h.ActorUser.Email
                    : null,

                EventType = h.EventType,
                Description = h.Description,

                PreviousValue = h.PreviousValue,
                NewValue = h.NewValue,

                CreatedAt = h.CreatedAt
            })
            .ToListAsync();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<AssetHistoryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    // =========================================================
    // PRIVATE HELPERS
    // =========================================================

    // "Before"/"after" snapshot of the amendable core fields on Asset,
    // used to diff UpdateAssetAsync changes into an AssetHistory entry.
    private readonly record struct AssetFieldSnapshot(
        Guid AssetTypeId,
        Guid DepartmentId,
        Guid LocationId,
        string Name,
        DateOnly AcquisitionDate,
        decimal AcquisitionCost,
        decimal ResidualValue);

    private readonly record struct AssetAmendment(
        string Description,
        Dictionary<string, object?> Previous,
        Dictionary<string, object?> New);

    private static Dictionary<Guid, string?> BuildAttributeSnapshot(
        IEnumerable<AssetAttributeValue> values)
    {
        return values.ToDictionary(
            v => v.AssetAttributeDefinitionId,
            AttributeValueToString);
    }

    private static string? AttributeValueToString(AssetAttributeValue value)
    {
        if (value.ValueText is not null)
        {
            return value.ValueText;
        }

        if (value.ValueNumber.HasValue)
        {
            return value.ValueNumber.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (value.ValueDate.HasValue)
        {
            return value.ValueDate.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        if (value.ValueBoolean.HasValue)
        {
            return value.ValueBoolean.Value.ToString();
        }

        return null;
    }

    private static AssetAmendment? DiffAssetFields(
        AssetFieldSnapshot previous,
        AssetFieldSnapshot next,
        Dictionary<Guid, string?> previousAttributes,
        Dictionary<Guid, string?> newAttributes)
    {
        var previousChanges = new Dictionary<string, object?>();
        var newChanges = new Dictionary<string, object?>();
        var changedLabels = new List<string>();

        void TrackChange<T>(string label, T previousValue, T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(previousValue, newValue))
            {
                return;
            }

            changedLabels.Add(label);
            previousChanges[label] = previousValue;
            newChanges[label] = newValue;
        }

        TrackChange("assetTypeId", previous.AssetTypeId, next.AssetTypeId);
        TrackChange("departmentId", previous.DepartmentId, next.DepartmentId);
        TrackChange("locationId", previous.LocationId, next.LocationId);
        TrackChange("name", previous.Name, next.Name);
        TrackChange("acquisitionDate", previous.AcquisitionDate, next.AcquisitionDate);
        TrackChange("acquisitionCost", previous.AcquisitionCost, next.AcquisitionCost);
        TrackChange("residualValue", previous.ResidualValue, next.ResidualValue);

        var changedAttributeCount = 0;

        foreach (var definitionId in previousAttributes.Keys.Union(newAttributes.Keys))
        {
            previousAttributes.TryGetValue(definitionId, out var previousValue);
            newAttributes.TryGetValue(definitionId, out var newValue);

            if (string.Equals(previousValue, newValue, StringComparison.Ordinal))
            {
                continue;
            }

            changedAttributeCount++;
            previousChanges[$"attribute:{definitionId}"] = previousValue;
            newChanges[$"attribute:{definitionId}"] = newValue;
        }

        if (changedAttributeCount > 0)
        {
            changedLabels.Add(
                changedAttributeCount == 1
                    ? "1 attribute"
                    : $"{changedAttributeCount} attributes");
        }

        if (changedLabels.Count == 0)
        {
            return null;
        }

        return new AssetAmendment(
            $"Updated: {string.Join(", ", changedLabels)}.",
            previousChanges,
            newChanges);
    }

    private static string NormalizeCondition(
        string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            throw new InvalidOperationException(
                "Asset condition is required.");
        }

        var normalized = condition
            .Trim()
            .ToUpperInvariant();

        if (!ValidConditions.Contains(normalized))
        {
            throw new InvalidOperationException(
                $"Invalid asset condition '{condition}'.");
        }

        return normalized;
    }

    private static void ValidateAttributes(
        List<AssetAttributeDefinition> definitions,
        List<AssetAttributeValueRequest> requestValues)
    {
        // Prevent the same attribute being submitted twice.
        var duplicateAttribute = requestValues
            .GroupBy(v =>
                v.AssetAttributeDefinitionId)
            .FirstOrDefault(g =>
                g.Count() > 1);

        if (duplicateAttribute is not null)
        {
            throw new InvalidOperationException(
                "The same asset attribute cannot be submitted more than once.");
        }

        // -------------------------
        // Required attributes
        // -------------------------

        foreach (var definition in definitions
                     .Where(d => d.IsRequired))
        {
            var suppliedValue = requestValues
                .FirstOrDefault(v =>
                    v.AssetAttributeDefinitionId ==
                    definition.Id);

            if (suppliedValue is null)
            {
                throw new InvalidOperationException(
                    $"Required attribute '{definition.Name}' is missing.");
            }
        }

        // -------------------------
        // Validate submitted values
        // -------------------------

        foreach (var requestValue in requestValues)
        {
            var definition = definitions
                .FirstOrDefault(d =>
                    d.Id ==
                    requestValue.AssetAttributeDefinitionId);

            if (definition is null)
            {
                throw new InvalidOperationException(
                    "One or more attributes do not belong to the selected asset type.");
            }

            ValidateAttributeValue(
                definition,
                requestValue);
        }
    }

    private static void ValidateAttributeValue(
        AssetAttributeDefinition definition,
        AssetAttributeValueRequest value)
    {
        var populatedValues = 0;

        if (value.ValueText is not null)
        {
            populatedValues++;
        }

        if (value.ValueNumber.HasValue)
        {
            populatedValues++;
        }

        if (value.ValueDate.HasValue)
        {
            populatedValues++;
        }

        if (value.ValueBoolean.HasValue)
        {
            populatedValues++;
        }

        if (populatedValues != 1)
        {
            throw new InvalidOperationException(
                $"Attribute '{definition.Name}' must contain exactly one value.");
        }

        switch (definition.DataType)
        {
            case "TEXT":

                if (value.ValueText is null)
                {
                    throw new InvalidOperationException(
                        $"Attribute '{definition.Name}' requires a text value.");
                }

                break;

            case "SELECT":

                if (value.ValueText is null)
                {
                    throw new InvalidOperationException(
                        $"Attribute '{definition.Name}' requires a selected value.");
                }

                if (definition.SelectOptions is not null &&
                    !definition.SelectOptions.Contains(
                        value.ValueText))
                {
                    throw new InvalidOperationException(
                        $"Invalid option for attribute '{definition.Name}'.");
                }

                break;

            case "NUMBER":

                if (!value.ValueNumber.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Attribute '{definition.Name}' requires a number.");
                }

                break;

            case "DATE":

                if (!value.ValueDate.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Attribute '{definition.Name}' requires a date.");
                }

                break;

            case "BOOLEAN":

                if (!value.ValueBoolean.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Attribute '{definition.Name}' requires a boolean value.");
                }

                break;

            default:

                throw new InvalidOperationException(
                    $"Unsupported attribute type '{definition.DataType}'.");
        }
    }

    private static AssetAttributeValue CreateAttributeValue(
        Guid assetId,
        AssetAttributeValueRequest request,
        DateTimeOffset now,
        Guid? userId)
    {
        return new AssetAttributeValue
        {
            Id = Guid.NewGuid(),

            AssetId = assetId,

            AssetAttributeDefinitionId =
                request.AssetAttributeDefinitionId,

            ValueText = request.ValueText,
            ValueNumber = request.ValueNumber,
            ValueDate = request.ValueDate,
            ValueBoolean = request.ValueBoolean,

            CreatedAt = now,
            UpdatedAt = now,

            CreatedBy = userId,
            UpdatedBy = userId
        };
    }
}