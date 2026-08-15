using CoreGrid.Api.Features.Assets.DTOs;

namespace CoreGrid.Api.Features.Assets.Services;

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetDepartmentsAsync(Guid organizationId);

    Task<DepartmentDto> CreateDepartmentAsync(
        Guid organizationId,
        Guid? userId,
        CreateDepartmentRequest request);
}
