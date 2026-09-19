using CoreGrid.Api.Features.OrgConfig.DTOs;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Paging;

namespace CoreGrid.Api.Features.OrgConfig.Services;

public interface IDepartmentService
{
    Task<PagedResult<DepartmentDto>> GetDepartmentsAsync(
        Guid organizationId,
        PagedQuery query,
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<DepartmentDto> CreateDepartmentAsync(
        Guid organizationId,
        Guid? userId,
        CreateDepartmentRequest request,
        CancellationToken cancellationToken);

    Task<DepartmentDto?> UpdateDepartmentAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken);

    Task<DepartmentDto?> SetDepartmentActiveAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        bool isActive,
        CancellationToken cancellationToken);
}
