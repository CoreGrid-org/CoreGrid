using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.OrgConfig.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.OrgConfig.Services;

public class DepartmentService : IDepartmentService
{
    private readonly CoreGridDbContext _context;

    public DepartmentService(CoreGridDbContext context)
    {
        _context = context;
    }

    public async Task<List<DepartmentDto>> GetDepartmentsAsync(
        Guid organizationId)
    {
        return await _context.Departments
            .AsNoTracking()
            .Where(d =>
                d.OrganizationId == organizationId &&
                d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                IsActive = d.IsActive
            })
            .ToListAsync();
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(
        Guid organizationId,
        Guid? userId,
        CreateDepartmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new InvalidOperationException(
                "Department code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(
                "Department name is required.");
        }

        var code = request.Code.Trim().ToUpperInvariant();

        if (code.Length > 20)
        {
            throw new InvalidOperationException(
                "Department code cannot be longer than 20 characters.");
        }

        var codeInUse = await _context.Departments
            .AsNoTracking()
            .AnyAsync(d =>
                d.OrganizationId == organizationId &&
                d.Code == code);

        if (codeInUse)
        {
            throw new InvalidOperationException(
                $"A department with code '{code}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;

        var department = new Department
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

        _context.Departments.Add(department);

        await _context.SaveChangesAsync();

        return new DepartmentDto
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            IsActive = department.IsActive
        };
    }

    public async Task<DepartmentDto?> UpdateDepartmentAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        UpdateDepartmentRequest request)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d =>
                d.Id == id &&
                d.OrganizationId == organizationId);

        if (department is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new InvalidOperationException(
                "Department code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(
                "Department name is required.");
        }

        var code = request.Code.Trim().ToUpperInvariant();

        if (code.Length > 20)
        {
            throw new InvalidOperationException(
                "Department code cannot be longer than 20 characters.");
        }

        var codeInUse = await _context.Departments
            .AsNoTracking()
            .AnyAsync(d =>
                d.OrganizationId == organizationId &&
                d.Code == code &&
                d.Id != id);

        if (codeInUse)
        {
            throw new InvalidOperationException(
                $"A department with code '{code}' already exists.");
        }

        department.Code = code;
        department.Name = request.Name.Trim();
        department.UpdatedAt = DateTimeOffset.UtcNow;
        department.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        return new DepartmentDto
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            IsActive = department.IsActive
        };
    }

    public async Task<DepartmentDto?> SetDepartmentActiveAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        bool isActive)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d =>
                d.Id == id &&
                d.OrganizationId == organizationId);

        if (department is null)
        {
            return null;
        }

        if (!isActive && department.IsActive)
        {
            // FR-012: a department referenced by an active asset may not be
            // deactivated. "Active" here means not yet disposed — every
            // other Asset.Status value still represents a live, in-service
            // asset that legitimately belongs to this department.
            var hasActiveAssets = await _context.Assets
                .AsNoTracking()
                .AnyAsync(a =>
                    a.DepartmentId == id &&
                    a.Status != "DISPOSED");

            if (hasActiveAssets)
            {
                throw new InvalidOperationException(
                    "This department cannot be deactivated while active assets are assigned to it.");
            }
        }

        department.IsActive = isActive;
        department.UpdatedAt = DateTimeOffset.UtcNow;
        department.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        return new DepartmentDto
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            IsActive = department.IsActive
        };
    }
}
