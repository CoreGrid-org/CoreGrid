using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Assets.Services;

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
                Name = d.Name
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
            Name = department.Name
        };
    }
}
