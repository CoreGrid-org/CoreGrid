using CoreGrid.Api.Features.Assets.DTOs;

namespace CoreGrid.Api.Features.Assets.Services;

public interface ILocationService
{
    Task<List<LocationDto>> GetLocationsAsync(
        Guid organizationId,
        Guid? departmentId);

    Task<LocationDto> CreateLocationAsync(
        Guid organizationId,
        Guid? userId,
        CreateLocationRequest request);
}
