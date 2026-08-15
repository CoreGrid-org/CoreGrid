using CoreGrid.Api.Features.OrgConfig.DTOs;

namespace CoreGrid.Api.Features.OrgConfig.Services;

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetDepartmentsAsync(Guid organizationId);

    Task<DepartmentDto> CreateDepartmentAsync(
        Guid organizationId,
        Guid? userId,
        CreateDepartmentRequest request);

    Task<DepartmentDto?> UpdateDepartmentAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        UpdateDepartmentRequest request);

    Task<DepartmentDto?> SetDepartmentActiveAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        bool isActive);
}
