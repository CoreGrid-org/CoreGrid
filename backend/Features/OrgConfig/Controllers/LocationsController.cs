using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.OrgConfig.DTOs;
using CoreGrid.Api.Features.OrgConfig.Services;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.OrgConfig.Controllers;

// same split as DepartmentsController.
[ApiController]
[Route("api/locations")]
[Authorize]
public class LocationsController : CoreGridControllerBase
{
    private const string ReadRoles =
        $"{nameof(CoreGridRole.Staff)},{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";

    private readonly ILocationService _locationService;

    public LocationsController(
        ILocationService locationService,
        CoreGridDbContext db) : base(db)
    {
        _locationService = locationService;
    }

    // GET /api/locations?departmentId=
    [HttpGet]
    [Authorize(Roles = ReadRoles)]
    public async Task<ActionResult<PagedResult<LocationDto>>> GetLocations(
        [FromQuery] Guid? departmentId,
        [FromQuery] PagedQuery query,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var locations = await _locationService.GetLocationsAsync(
            currentUser.OrganizationId, departmentId, query, includeInactive, cancellationToken);

        return Ok(locations);
    }

    // POST /api/locations
    [HttpPost]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<LocationDto>> CreateLocation(
        [FromBody] CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var location = await _locationService.CreateLocationAsync(
            currentUser.OrganizationId, currentUser.Id, request, cancellationToken);

        return Ok(location);
    }

    // PUT /api/locations/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<LocationDto>> UpdateLocation(
        Guid id,
        [FromBody] UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var location = await _locationService.UpdateLocationAsync(
            currentUser.OrganizationId, id, currentUser.Id, request, cancellationToken);

        return location is null
            ? throw NotFoundException.For(nameof(Location), id)
            : Ok(location);
    }

    // PATCH /api/locations/{id}/deactivate
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<LocationDto>> DeactivateLocation(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await SetActive(id, false, cancellationToken);
    }

    // PATCH /api/locations/{id}/activate
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = Policies.CanManageConfiguration)]
    public async Task<ActionResult<LocationDto>> ActivateLocation(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await SetActive(id, true, cancellationToken);
    }

    private async Task<ActionResult<LocationDto>> SetActive(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var location = await _locationService.SetLocationActiveAsync(
            currentUser.OrganizationId, id, currentUser.Id, isActive, cancellationToken);

        return location is null
            ? throw NotFoundException.For(nameof(Location), id)
            : Ok(location);
    }
}
