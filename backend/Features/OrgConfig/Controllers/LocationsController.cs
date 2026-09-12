using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.OrgConfig.DTOs;
using CoreGrid.Api.Features.OrgConfig.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using CoreGrid.Api.Features.Shared;

namespace CoreGrid.Api.Features.OrgConfig.Controllers;

// FR-005 / SRS §4.6: same split as DepartmentsController.
[ApiController]
[Route("api/locations")]
[Authorize]
public class LocationsController : CoreGridControllerBase
{
    private const string ReadRoles =
        $"{nameof(CoreGridRole.Staff)},{nameof(CoreGridRole.InventoryOfficer)},{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}";
    private const string ManageRoles = nameof(CoreGridRole.Administrator);

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
    public async Task<ActionResult<List<LocationDto>>> GetLocations(
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var locations =
            await _locationService.GetLocationsAsync(
                currentUser.OrganizationId,
                departmentId);

        return Ok(locations);
    }

    // POST /api/locations
    [HttpPost]
    [Authorize(Roles = ManageRoles)]
    public async Task<ActionResult<LocationDto>> CreateLocation(
        [FromBody] CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var location = await _locationService.CreateLocationAsync(
                currentUser.OrganizationId,
                currentUser.Id,
                request);

            return Ok(location);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Location could not be created because of a database conflict."
            });
        }
    }

    // PUT /api/locations/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = ManageRoles)]
    public async Task<ActionResult<LocationDto>> UpdateLocation(
        Guid id,
        [FromBody] UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var location = await _locationService.UpdateLocationAsync(
                currentUser.OrganizationId,
                id,
                currentUser.Id,
                request);

            if (location is null)
            {
                return NotFound(new
                {
                    message = "Location not found."
                });
            }

            return Ok(location);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Location could not be updated because of a database conflict."
            });
        }
    }

    // PATCH /api/locations/{id}/deactivate
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = ManageRoles)]
    public async Task<ActionResult<LocationDto>> DeactivateLocation(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await SetActive(id, false, cancellationToken);
    }

    // PATCH /api/locations/{id}/activate
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Roles = ManageRoles)]
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

        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            var location = await _locationService.SetLocationActiveAsync(
                currentUser.OrganizationId,
                id,
                currentUser.Id,
                isActive);

            if (location is null)
            {
                return NotFound(new
                {
                    message = "Location not found."
                });
            }

            return Ok(location);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}
