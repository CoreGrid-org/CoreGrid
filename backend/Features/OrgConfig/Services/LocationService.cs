using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.OrgConfig.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.OrgConfig.Services;

public class LocationService : ILocationService
{
    private readonly CoreGridDbContext _context;

    public LocationService(CoreGridDbContext context)
    {
        _context = context;
    }

    public async Task<List<LocationDto>> GetLocationsAsync(
        Guid organizationId,
        Guid? departmentId)
    {
        var query = _context.Locations
            .AsNoTracking()
            .Where(l =>
                l.OrganizationId == organizationId &&
                l.IsActive);

        if (departmentId.HasValue)
        {
            query = query.Where(l =>
                l.DepartmentId == departmentId.Value);
        }

        return await query
            .OrderBy(l => l.Name)
            .Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                Type = l.Type,
                DepartmentId = l.DepartmentId,
                DepartmentName = l.Department != null
                    ? l.Department.Name
                    : string.Empty,
                IsActive = l.IsActive
            })
            .ToListAsync();
    }

    public async Task<LocationDto> CreateLocationAsync(
        Guid organizationId,
        Guid? userId,
        CreateLocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(
                "Location name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            throw new InvalidOperationException(
                "Location type is required.");
        }

        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d =>
                d.Id == request.DepartmentId &&
                d.OrganizationId == organizationId &&
                d.IsActive);

        if (department is null)
        {
            throw new InvalidOperationException(
                "Department was not found or is inactive.");
        }

        var now = DateTimeOffset.UtcNow;

        var location = new Location
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            DepartmentId = request.DepartmentId,
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        _context.Locations.Add(location);

        await _context.SaveChangesAsync();

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
        UpdateLocationRequest request)
    {
        var location = await _context.Locations
            .FirstOrDefaultAsync(l =>
                l.Id == id &&
                l.OrganizationId == organizationId);

        if (location is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(
                "Location name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            throw new InvalidOperationException(
                "Location type is required.");
        }

        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d =>
                d.Id == request.DepartmentId &&
                d.OrganizationId == organizationId &&
                d.IsActive);

        if (department is null)
        {
            throw new InvalidOperationException(
                "Department was not found or is inactive.");
        }

        location.Name = request.Name.Trim();
        location.Type = request.Type.Trim();
        location.DepartmentId = request.DepartmentId;
        location.UpdatedAt = DateTimeOffset.UtcNow;
        location.UpdatedBy = userId;

        await _context.SaveChangesAsync();

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
        bool isActive)
    {
        var location = await _context.Locations
            .FirstOrDefaultAsync(l =>
                l.Id == id &&
                l.OrganizationId == organizationId);

        if (location is null)
        {
            return null;
        }

        if (!isActive && location.IsActive)
        {
            // FR-012: same "active" definition as the department guard.
            var hasActiveAssets = await _context.Assets
                .AsNoTracking()
                .AnyAsync(a =>
                    a.LocationId == id &&
                    a.Status != "DISPOSED");

            if (hasActiveAssets)
            {
                throw new InvalidOperationException(
                    "This location cannot be deactivated while active assets are assigned to it.");
            }
        }

        location.IsActive = isActive;
        location.UpdatedAt = DateTimeOffset.UtcNow;
        location.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        var departmentName = await _context.Departments
            .AsNoTracking()
            .Where(d => d.Id == location.DepartmentId)
            .Select(d => d.Name)
            .FirstOrDefaultAsync() ?? string.Empty;

        return new LocationDto
        {
            Id = location.Id,
            Name = location.Name,
            Type = location.Type,
            DepartmentId = location.DepartmentId,
            DepartmentName = departmentName,
            IsActive = location.IsActive
        };
    }
}
