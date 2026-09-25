using CoreGrid.Api.Data;
using CoreGrid.Api.Features.Shared.CurrentUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Identity;

// Provides the current user's identity information.
[ApiController]
[Route("api/me")]
[Authorize]
public class MeController(CoreGridDbContext db, CurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken)
    {
        // Ensures the current user context is valid.
        if (!currentUser.IsResolved || currentUser.IsServicePrincipal)
        {
            return Unauthorized();
        }

        // Loads the user's profile and organisation name for the account screen.
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Organization)
            .SingleAsync(u => u.Id == currentUser.Id, cancellationToken);

        return Ok(new MeResponse(
            user.Id,
            user.Email,
            user.GivenName,
            user.FamilyName,
            user.Role,
            user.IsActive,
            user.OrganizationId,
            user.Organization?.Name ?? "organisation"));
    }
}
