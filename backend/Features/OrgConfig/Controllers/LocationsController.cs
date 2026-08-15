using System.Security.Claims;
using CoreGrid.Api.Data;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Assets.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Assets.Controllers;

[ApiController]
[Route("api/locations")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly CoreGridDbContext _db;

    public LocationsController(
        ILocationService locationService,
        CoreGridDbContext db)
    {
        _locationService = locationService;
        _db = db;
    }

    // GET /api/locations?departmentId=
    [HttpGet]
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

    private async Task<CoreGrid.Api.Domain.User?> GetCurrentUserAsync(
        CancellationToken cancellationToken)
    {
        var externalSubjectId =
            User.FindFirst("sub")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(externalSubjectId))
        {
            return null;
        }

        return await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                u => u.ExternalSubjectId == externalSubjectId,
                cancellationToken);
    }
}
