using CoreGrid.Api.Features.OrgConfig.DTOs;

namespace CoreGrid.Api.Features.OrgConfig.Services;

public interface ILocationService
{
    Task<List<LocationDto>> GetLocationsAsync(
        Guid organizationId,
        Guid? departmentId);

    Task<LocationDto> CreateLocationAsync(
        Guid organizationId,
        Guid? userId,
        CreateLocationRequest request);

    Task<LocationDto?> UpdateLocationAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        UpdateLocationRequest request);

    Task<LocationDto?> SetLocationActiveAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        bool isActive);
}
