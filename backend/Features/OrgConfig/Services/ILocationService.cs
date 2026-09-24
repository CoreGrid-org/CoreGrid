using CoreGrid.Api.Features.OrgConfig.DTOs;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Paging;

namespace CoreGrid.Api.Features.OrgConfig.Services;

public interface ILocationService
{
    Task<PagedResult<LocationDto>> GetLocationsAsync(
        Guid organizationId,
        Guid? departmentId,
        PagedQuery query,
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<LocationDto> CreateLocationAsync(
        Guid organizationId,
        Guid? userId,
        CreateLocationRequest request,
        CancellationToken cancellationToken);

    Task<LocationDto?> UpdateLocationAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        UpdateLocationRequest request,
        CancellationToken cancellationToken);

    Task<LocationDto?> SetLocationActiveAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        bool isActive,
        CancellationToken cancellationToken);
}
