using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Assets.Services;

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
                    : string.Empty
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
            DepartmentName = department.Name
        };
    }
}
