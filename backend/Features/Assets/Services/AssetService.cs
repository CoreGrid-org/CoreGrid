using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Assets.Helpers;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Finance;
using CoreGrid.Api.Features.Shared.Paging;
using CoreGrid.Api.Features.Shared.Scoping;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Assets.Services;

public class AssetService : IAssetService
{
    private readonly CoreGridDbContext _context;

    private static readonly string[] ValidConditions = ["NEW", "GOOD", "FAIR", "POOR", "UNSERVICEABLE"];

    // §5.3: one AssetDto projection, used by GetAssetsAsync.
    private static readonly Expression<Func<Asset, AssetDto>> ToAssetDtoExpression = a => new AssetDto
    {
        Id = a.Id,
        AssetCode = a.AssetCode,
        Name = a.Name,
        AssetTypeId = a.AssetTypeId,
        AssetTypeName = a.AssetType != null ? a.AssetType.Name : string.Empty,
        DepartmentId = a.DepartmentId,
        DepartmentName = a.Department != null ? a.Department.Name : string.Empty,
        LocationId = a.LocationId,
        LocationName = a.Location != null ? a.Location.Name : string.Empty,
        Status = a.Status,
        Condition = a.Condition,
        AcquisitionDate = a.AcquisitionDate,
        AcquisitionCost = a.AcquisitionCost,
        QrPayload = a.QrPayload
    };

    private static readonly IReadOnlyDictionary<string, Expression<Func<Asset, object?>>> AssetSortMap =
        new Dictionary<string, Expression<Func<Asset, object?>>>
        {
            ["assetcode"] = a => a.AssetCode,
            ["name"] = a => a.Name,
            ["status"] = a => a.Status,
            ["condition"] = a => a.Condition,
            ["acquisitiondate"] = a => a.AcquisitionDate,
            ["acquisitioncost"] = a => a.AcquisitionCost,
        };

    // §5.3: one AssetDetailDto projection (paired with the AssetType's
    // UsefulLifeYears so the live residual-value recompute below needs no
    // second round-trip the way this used to).
    private record AssetDetailProjection(AssetDetailDto Dto, int UsefulLifeYears);

    private static readonly Expression<Func<Asset, AssetDetailProjection>> ToAssetDetailProjectionExpression = a => new AssetDetailProjection(
        new AssetDetailDto
        {
            Id = a.Id,
            AssetCode = a.AssetCode,
            Name = a.Name,
            AssetTypeId = a.AssetTypeId,
            AssetTypeName = a.AssetType != null ? a.AssetType.Name : string.Empty,
            DepartmentId = a.DepartmentId,
            DepartmentName = a.Department != null ? a.Department.Name : string.Empty,
            LocationId = a.LocationId,
            LocationName = a.Location != null ? a.Location.Name : string.Empty,
            Status = a.Status,
            Condition = a.Condition,
            AcquisitionDate = a.AcquisitionDate,
            AcquisitionCost = a.AcquisitionCost,
            ResidualValue = a.ResidualValue,
            QrPayload = a.QrPayload,
            // §5.3: the stray `.ToList().ToList()` collapses to one call.
            Attributes = a.AssetAttributeValues
                .OrderBy(v => v.AssetAttributeDefinition != null ? v.AssetAttributeDefinition.DisplayOrder : 0)
                .Select(v => new AssetAttributeValueDto
                {
                    AttributeDefinitionId = v.AssetAttributeDefinitionId,
                    Name = v.AssetAttributeDefinition != null ? v.AssetAttributeDefinition.Name : string.Empty,
                    DataType = v.AssetAttributeDefinition != null ? v.AssetAttributeDefinition.DataType : string.Empty,
                    IsRequired = v.AssetAttributeDefinition != null && v.AssetAttributeDefinition.IsRequired,
                    ValueText = v.ValueText,
                    ValueNumber = v.ValueNumber,
                    ValueDate = v.ValueDate,
                    ValueBoolean = v.ValueBoolean
                })
                .ToList()
        },
        a.AssetType != null ? a.AssetType.UsefulLifeYears : 0);

    public AssetService(CoreGridDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // 1. GET ASSETS — Search + Filter + Sort + Pagination
    // =========================================================

    public async Task<PagedResult<AssetDto>> GetAssetsAsync(
        Guid organizationId,
        DepartmentScope scope,
        AssetQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var query = _context.Assets
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId)
            .ApplyScope(scope, a => (Guid?)a.DepartmentId);

        // Universal search — asset code/name, category, type, and dynamic
        // attribute values all match a single search term.
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim();
            var pattern = $"%{search}%";

            var searchAsNumber = decimal.TryParse(search, out var parsedNumber) ? parsedNumber : (decimal?)null;
            var searchAsDate = DateOnly.TryParse(search, out var parsedDate) ? parsedDate : (DateOnly?)null;

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
                    (v.AssetAttributeDefinition != null && EF.Functions.ILike(v.AssetAttributeDefinition.Name, pattern))));
        }

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(a => a.AssetType != null && a.AssetType.AssetCategoryId == parameters.CategoryId.Value);
        }

        if (parameters.AssetTypeId.HasValue)
        {
            query = query.Where(a => a.AssetTypeId == parameters.AssetTypeId.Value);
        }

        if (parameters.DepartmentId.HasValue)
        {
            query = query.Where(a => a.DepartmentId == parameters.DepartmentId.Value);
        }

        if (parameters.LocationId.HasValue)
        {
            query = query.Where(a => a.LocationId == parameters.LocationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = parameters.Status.Trim().ToUpperInvariant();
            query = query.Where(a => a.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Condition))
        {
            var condition = parameters.Condition.Trim().ToUpperInvariant();
            query = query.Where(a => a.Condition == condition);
        }

        var sorted = query.ApplySort(parameters, AssetSortMap, defaultSortKey: "name");
        return await sorted.ToPagedResultAsync(parameters, ToAssetDtoExpression, cancellationToken);
    }

    // =========================================================
    // 2. GET ASSET BY ID — Asset Detail + Dynamic Attributes
    // =========================================================

    public async Task<AssetDetailDto?> GetAssetByIdAsync(
        Guid organizationId,
        DepartmentScope scope,
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var projection = await _context.Assets
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId && a.Id == assetId)
            .ApplyScope(scope, a => (Guid?)a.DepartmentId)
            .Select(ToAssetDetailProjectionExpression)
            .FirstOrDefaultAsync(cancellationToken);

        if (projection is null)
        {
            return null;
        }

        projection.Dto.ResidualValue = StraightLineDepreciation.CalculateResidualValue(
            projection.Dto.AcquisitionCost, projection.Dto.AcquisitionDate, projection.UsefulLifeYears);

        return projection.Dto;
    }

    // =========================================================
    // 3. CREATE ASSET
    // =========================================================

    public async Task<AssetDetailDto> CreateAssetAsync(
        Guid organizationId,
        Guid? userId,
        CreateAssetRequest request,
        CancellationToken cancellationToken)
    {
        // [Required] on the DTO makes these 400 for a model-bound HTTP
        // caller before this method ever runs; the .Value access below is
        // then always safe.
        var assetTypeId = request.AssetTypeId!.Value;
        var departmentId = request.DepartmentId!.Value;
        var locationId = request.LocationId!.Value;
        var acquisitionDate = request.AcquisitionDate!.Value;
        var acquisitionCost = request.AcquisitionCost!.Value;

        var assetType = await _context.AssetTypes
            .AsNoTracking()
            .Include(at => at.AssetCategory)
            .FirstOrDefaultAsync(at => at.Id == assetTypeId && at.OrganizationId == organizationId, cancellationToken);

        if (assetType is null)
        {
            throw new ValidationException(nameof(request.AssetTypeId), "Asset type was not found for this organization.");
        }

        var departmentExists = await _context.Departments
            .AsNoTracking()
            .AnyAsync(d => d.Id == departmentId && d.OrganizationId == organizationId && d.IsActive, cancellationToken);

        if (!departmentExists)
        {
            throw new ValidationException(nameof(request.DepartmentId), "Department was not found or is inactive.");
        }

        var location = await _context.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == locationId && l.OrganizationId == organizationId && l.IsActive, cancellationToken);

        if (location is null)
        {
            throw new ValidationException(nameof(request.LocationId), "Location was not found or is inactive.");
        }

        if (location.DepartmentId != departmentId)
        {
            throw new ValidationException(nameof(request.LocationId), "Selected location does not belong to the selected department.");
        }

        var condition = NormalizeCondition(request.Condition);

        var attributeDefinitions = await _context.AssetAttributeDefinitions
            .AsNoTracking()
            .Where(a => a.AssetTypeId == assetTypeId)
            .ToListAsync(cancellationToken);

        ValidateAttributes(attributeDefinitions, request.Attributes);

        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

        var organizationCode = OrganizationCodeGenerator.Generate(organization?.Name ?? "ORG");

        var existingCount = await _context.Assets
            .CountAsync(a => a.OrganizationId == organizationId && a.AssetTypeId == assetTypeId, cancellationToken);

        var nextSequence = existingCount + 1;
        var categoryCode = assetType.AssetCategory?.Code ?? "GEN";
        var assetCode = AssetCodeGenerator.Generate(organizationCode, categoryCode, assetType.Code, nextSequence);

        // Safety check
        while (await _context.Assets.AnyAsync(a => a.OrganizationId == organizationId && a.AssetCode == assetCode, cancellationToken))
        {
            nextSequence++;
            assetCode = AssetCodeGenerator.Generate(organizationCode, categoryCode, assetType.Code, nextSequence);
        }

        var now = DateTimeOffset.UtcNow;

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AssetTypeId = assetTypeId,
            DepartmentId = departmentId,
            LocationId = locationId,
            AssetCode = assetCode,
            Name = request.Name.Trim(),
            Status = "ACTIVE",
            Condition = condition,
            AcquisitionDate = acquisitionDate,
            AcquisitionCost = Math.Round(acquisitionCost, 2, MidpointRounding.AwayFromZero),
            ResidualValue = StraightLineDepreciation.CalculateResidualValue(acquisitionCost, acquisitionDate, assetType.UsefulLifeYears),
            CumulativeMaintenanceCost = 0,
            RepairCount = 0,
            QrPayload = assetCode,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        foreach (var requestValue in request.Attributes)
        {
            asset.AssetAttributeValues.Add(CreateAttributeValue(asset.Id, requestValue, now, userId));
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

        await _context.SaveChangesAsync(cancellationToken);

        return await GetAssetByIdAsync(organizationId, DepartmentScope.Unrestricted, asset.Id, cancellationToken)
            ?? throw new InvalidOperationException("Asset was created but could not be retrieved.");
    }

    // =========================================================
    // 4. UPDATE ASSET
    // =========================================================

    public async Task<AssetDetailDto?> UpdateAssetAsync(
        Guid organizationId,
        Guid assetId,
        Guid? userId,
        UpdateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var asset = await _context.Assets
            .Include(a => a.AssetAttributeValues)
            .FirstOrDefaultAsync(a => a.Id == assetId && a.OrganizationId == organizationId, cancellationToken);

        if (asset is null)
        {
            return null;
        }

        // [Required] on the DTO makes these 400 for a model-bound HTTP
        // caller before this method ever runs; the .Value access below is
        // then always safe.
        var assetTypeId = request.AssetTypeId!.Value;
        var departmentId = request.DepartmentId!.Value;
        var locationId = request.LocationId!.Value;
        var acquisitionDate = request.AcquisitionDate!.Value;
        var acquisitionCost = request.AcquisitionCost!.Value;

        var assetType = await _context.AssetTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(at => at.Id == assetTypeId && at.OrganizationId == organizationId, cancellationToken);

        if (assetType is null)
        {
            throw new ValidationException(nameof(request.AssetTypeId), "Asset type was not found for this organization.");
        }

        var departmentExists = await _context.Departments
            .AsNoTracking()
            .AnyAsync(d => d.Id == departmentId && d.OrganizationId == organizationId && d.IsActive, cancellationToken);

        if (!departmentExists)
        {
            throw new ValidationException(nameof(request.DepartmentId), "Department was not found or is inactive.");
        }

        var location = await _context.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == locationId && l.OrganizationId == organizationId && l.IsActive, cancellationToken);

        if (location is null)
        {
            throw new ValidationException(nameof(request.LocationId), "Location was not found or is inactive.");
        }

        if (location.DepartmentId != departmentId)
        {
            throw new ValidationException(nameof(request.LocationId), "Selected location does not belong to the selected department.");
        }

        var attributeDefinitions = await _context.AssetAttributeDefinitions
            .AsNoTracking()
            .Where(a => a.AssetTypeId == assetTypeId)
            .ToListAsync(cancellationToken);

        ValidateAttributes(attributeDefinitions, request.Attributes);

        var now = DateTimeOffset.UtcNow;

        // Snapshot "before" state for AssetHistory (FR-026)
        var previousFields = new AssetFieldSnapshot(
            asset.AssetTypeId, asset.DepartmentId, asset.LocationId, asset.Name,
            asset.AcquisitionDate, asset.AcquisitionCost, asset.ResidualValue);
        var previousAttributes = BuildAttributeSnapshot(asset.AssetAttributeValues);

        asset.AssetTypeId = assetTypeId;
        asset.DepartmentId = departmentId;
        asset.LocationId = locationId;
        asset.Name = request.Name.Trim();
        asset.AcquisitionDate = acquisitionDate;
        asset.AcquisitionCost = Math.Round(acquisitionCost, 2, MidpointRounding.AwayFromZero);
        asset.ResidualValue = StraightLineDepreciation.CalculateResidualValue(acquisitionCost, acquisitionDate, assetType.UsefulLifeYears);
        asset.UpdatedAt = now;
        asset.UpdatedBy = userId;

        _context.AssetAttributeValues.RemoveRange(asset.AssetAttributeValues);

        var newAttributeValues = new List<AssetAttributeValue>();
        foreach (var requestValue in request.Attributes)
        {
            var attributeValue = CreateAttributeValue(asset.Id, requestValue, now, userId);
            newAttributeValues.Add(attributeValue);
            _context.AssetAttributeValues.Add(attributeValue);
        }

        var newFields = new AssetFieldSnapshot(
            asset.AssetTypeId, asset.DepartmentId, asset.LocationId, asset.Name,
            asset.AcquisitionDate, asset.AcquisitionCost, asset.ResidualValue);
        var newAttributes = BuildAttributeSnapshot(newAttributeValues);

        var amendment = DiffAssetFields(previousFields, newFields, previousAttributes, newAttributes);

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

        await _context.SaveChangesAsync(cancellationToken);

        return await GetAssetByIdAsync(organizationId, DepartmentScope.Unrestricted, asset.Id, cancellationToken);
    }

    // =========================================================
    // 5. UPDATE CONDITION
    // =========================================================

    public async Task<bool> UpdateConditionAsync(
        Guid organizationId,
        Guid assetId,
        Guid? userId,
        UpdateAssetConditionRequest request,
        CancellationToken cancellationToken)
    {
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.OrganizationId == organizationId, cancellationToken);

        if (asset is null)
        {
            return false;
        }

        var condition = NormalizeCondition(request.Condition);
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

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    // =========================================================
    // 6. GET ASSET BY QR / ASSET CODE
    // =========================================================

    public async Task<AssetDetailDto?> GetAssetByQrCodeAsync(
        Guid organizationId,
        DepartmentScope scope,
        string code,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalizedCode = code.Trim();

        var assetId = await _context.Assets
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId && (a.AssetCode == normalizedCode || a.QrPayload == normalizedCode))
            .ApplyScope(scope, a => (Guid?)a.DepartmentId)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!assetId.HasValue)
        {
            return null;
        }

        return await GetAssetByIdAsync(organizationId, scope, assetId.Value, cancellationToken);
    }

    // =========================================================
    // ORGANIZATION CODE
    // =========================================================

    public async Task<string> GetOrganizationCodeAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

        return OrganizationCodeGenerator.Generate(organization?.Name ?? "ORG");
    }

    // =========================================================
    // 7. GET ASSET HISTORY
    // =========================================================

    public async Task<PagedResult<AssetHistoryDto>?> GetAssetHistoryAsync(
        Guid organizationId,
        DepartmentScope scope,
        Guid assetId,
        AssetHistoryQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var assetExists = await _context.Assets
            .AsNoTracking()
            .Where(a => a.Id == assetId && a.OrganizationId == organizationId)
            .ApplyScope(scope, a => (Guid?)a.DepartmentId)
            .AnyAsync(cancellationToken);

        if (!assetExists)
        {
            return null;
        }

        var query = _context.AssetHistoryEntries
            .AsNoTracking()
            .Where(h => h.AssetId == assetId && h.OrganizationId == organizationId)
            .OrderByDescending(h => h.CreatedAt);

        return await query.ToPagedResultAsync(parameters, h => new AssetHistoryDto
        {
            Id = h.Id,
            AssetId = h.AssetId,
            ActorUserId = h.ActorUserId,
            ActorEmail = h.ActorUser != null ? h.ActorUser.Email : null,
            EventType = h.EventType,
            Description = h.Description,
            PreviousValue = h.PreviousValue,
            NewValue = h.NewValue,
            CreatedAt = h.CreatedAt
        }, cancellationToken);
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

    private static Dictionary<Guid, string?> BuildAttributeSnapshot(IEnumerable<AssetAttributeValue> values) =>
        values.ToDictionary(v => v.AssetAttributeDefinitionId, AttributeValueToString);

    private static string? AttributeValueToString(AssetAttributeValue value)
    {
        if (value.ValueText is not null) return value.ValueText;
        if (value.ValueNumber.HasValue) return value.ValueNumber.Value.ToString(CultureInfo.InvariantCulture);
        if (value.ValueDate.HasValue) return value.ValueDate.Value.ToString("O", CultureInfo.InvariantCulture);
        if (value.ValueBoolean.HasValue) return value.ValueBoolean.Value.ToString();
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
            changedLabels.Add(changedAttributeCount == 1 ? "1 attribute" : $"{changedAttributeCount} attributes");
        }

        if (changedLabels.Count == 0)
        {
            return null;
        }

        return new AssetAmendment($"Updated: {string.Join(", ", changedLabels)}.", previousChanges, newChanges);
    }

    private static string NormalizeCondition(string condition)
    {
        var normalized = condition.Trim().ToUpperInvariant();

        if (!ValidConditions.Contains(normalized))
        {
            throw new ValidationException(nameof(UpdateAssetConditionRequest.Condition), $"Invalid asset condition '{condition}'.");
        }

        return normalized;
    }

    private static void ValidateAttributes(
        List<AssetAttributeDefinition> definitions,
        List<AssetAttributeValueRequest> requestValues)
    {
        // Prevent the same attribute being submitted twice.
        var duplicateAttribute = requestValues
            .GroupBy(v => v.AssetAttributeDefinitionId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateAttribute is not null)
        {
            throw new ValidationException("Attributes", "The same asset attribute cannot be submitted more than once.");
        }

        foreach (var definition in definitions.Where(d => d.IsRequired))
        {
            var suppliedValue = requestValues.FirstOrDefault(v => v.AssetAttributeDefinitionId == definition.Id);

            if (suppliedValue is null)
            {
                throw new ValidationException("Attributes", $"Required attribute '{definition.Name}' is missing.");
            }
        }

        foreach (var requestValue in requestValues)
        {
            var definition = definitions.FirstOrDefault(d => d.Id == requestValue.AssetAttributeDefinitionId);

            if (definition is null)
            {
                throw new ValidationException("Attributes", "One or more attributes do not belong to the selected asset type.");
            }

            ValidateAttributeValue(definition, requestValue);
        }
    }

    private static void ValidateAttributeValue(AssetAttributeDefinition definition, AssetAttributeValueRequest value)
    {
        var populatedValues = 0;

        if (value.ValueText is not null) populatedValues++;
        if (value.ValueNumber.HasValue) populatedValues++;
        if (value.ValueDate.HasValue) populatedValues++;
        if (value.ValueBoolean.HasValue) populatedValues++;

        if (populatedValues != 1)
        {
            throw new ValidationException("Attributes", $"Attribute '{definition.Name}' must contain exactly one value.");
        }

        switch (definition.DataType)
        {
            case "TEXT":
                if (value.ValueText is null)
                {
                    throw new ValidationException("Attributes", $"Attribute '{definition.Name}' requires a text value.");
                }
                break;

            case "SELECT":
                if (value.ValueText is null)
                {
                    throw new ValidationException("Attributes", $"Attribute '{definition.Name}' requires a selected value.");
                }
                if (definition.SelectOptions is not null && !definition.SelectOptions.Contains(value.ValueText))
                {
                    throw new ValidationException("Attributes", $"Invalid option for attribute '{definition.Name}'.");
                }
                break;

            case "NUMBER":
                if (!value.ValueNumber.HasValue)
                {
                    throw new ValidationException("Attributes", $"Attribute '{definition.Name}' requires a number.");
                }
                break;

            case "DATE":
                if (!value.ValueDate.HasValue)
                {
                    throw new ValidationException("Attributes", $"Attribute '{definition.Name}' requires a date.");
                }
                break;

            case "BOOLEAN":
                if (!value.ValueBoolean.HasValue)
                {
                    throw new ValidationException("Attributes", $"Attribute '{definition.Name}' requires a boolean value.");
                }
                break;

            default:
                throw new ValidationException("Attributes", $"Unsupported attribute type '{definition.DataType}'.");
        }

        // Enforce any additional rule stored on the definition (min/max,
        // minLength/maxLength, regex, minDate/maxDate).
        AttributeValidationRuleEngine.Enforce(definition, value);
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
            AssetAttributeDefinitionId = request.AssetAttributeDefinitionId,
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
