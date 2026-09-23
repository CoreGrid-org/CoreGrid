using System.Linq.Expressions;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.OrgConfig.DTOs;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.OrgConfig.Services;

public class LocationService : ILocationService
{
    private static readonly Expression<Func<Location, LocationDto>> ToDtoExpression = l => new LocationDto
    {
        Id = l.Id,
        Name = l.Name,
        Type = l.Type,
        DepartmentId = l.DepartmentId,
        DepartmentName = l.Department != null ? l.Department.Name : string.Empty,
        IsActive = l.IsActive
    };

    private static readonly Func<Location, LocationDto> ToDto = ToDtoExpression.Compile();

    private static readonly IReadOnlyDictionary<string, Expression<Func<Location, object?>>> SortMap =
        new Dictionary<string, Expression<Func<Location, object?>>>
        {
            ["name"] = l => l.Name,
            ["type"] = l => l.Type,
        };

    private readonly CoreGridDbContext _context;

    public LocationService(CoreGridDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LocationDto>> GetLocationsAsync(
        Guid organizationId,
        Guid? departmentId,
        PagedQuery query,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var locations = _context.Locations
            .AsNoTracking()
            .Where(l => l.OrganizationId == organizationId);

        if (!includeInactive)
        {
            locations = locations.Where(l => l.IsActive);
        }

        if (departmentId.HasValue)
        {
            locations = locations.Where(l => l.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            locations = locations.Where(l => EF.Functions.ILike(l.Name, pattern) || EF.Functions.ILike(l.Type, pattern));
        }

        var sorted = locations.ApplySort(query, SortMap, defaultSortKey: "name");
        return await sorted.ToPagedResultAsync(query, ToDtoExpression, cancellationToken);
    }

    public async Task<LocationDto> CreateLocationAsync(
        Guid organizationId,
        Guid? userId,
        CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var departmentId = request.DepartmentId!.Value;

        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == departmentId && d.OrganizationId == organizationId && d.IsActive, cancellationToken);

        if (department is null)
        {
            throw new ValidationException(nameof(request.DepartmentId), "Department was not found or is inactive.");
        }

        var now = DateTimeOffset.UtcNow;

        var location = new Location
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            DepartmentId = departmentId,
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        _context.Locations.Add(location);
        await _context.SaveChangesAsync(cancellationToken);

        return new LocationDto
        {
            Id = location.Id,
            Name = location.Name,
            Type = location.Type,
            DepartmentId = location.DepartmentId,
            DepartmentName = department.Name,
            IsActive = location.IsActive
        };
    }

    public async Task<LocationDto?> UpdateLocationAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == id && l.OrganizationId == organizationId, cancellationToken);

        if (location is null)
        {
            return null;
        }

        var departmentId = request.DepartmentId!.Value;

        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == departmentId && d.OrganizationId == organizationId && d.IsActive, cancellationToken);

        if (department is null)
        {
            throw new ValidationException(nameof(request.DepartmentId), "Department was not found or is inactive.");
        }

        location.Name = request.Name.Trim();
        location.Type = request.Type.Trim();
        location.DepartmentId = departmentId;
        location.UpdatedAt = DateTimeOffset.UtcNow;
        location.UpdatedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return new LocationDto
        {
            Id = location.Id,
            Name = location.Name,
            Type = location.Type,
            DepartmentId = location.DepartmentId,
            DepartmentName = department.Name,
            IsActive = location.IsActive
        };
    }

    public async Task<LocationDto?> SetLocationActiveAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        // loads Department alongside the location so the response
        // below reuses it instead of re-querying the department name
        // afterwards.
        var location = await _context.Locations
            .Include(l => l.Department)
            .FirstOrDefaultAsync(l => l.Id == id && l.OrganizationId == organizationId, cancellationToken);

        if (location is null)
        {
            return null;
        }

        if (!isActive && location.IsActive)
        {
            // FR-012: same "active" definition as the department guard.
            var hasActiveAssets = await _context.Assets
                .AsNoTracking()
                .AnyAsync(a => a.LocationId == id && a.Status != AssetStatuses.Disposed, cancellationToken);

            if (hasActiveAssets)
            {
                throw new BusinessRuleException(
                    "This location cannot be deactivated while active assets are assigned to it.",
                    "location_has_active_assets");
            }
        }

        location.IsActive = isActive;
        location.UpdatedAt = DateTimeOffset.UtcNow;
        location.UpdatedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return ToDto(location);
    }
}
