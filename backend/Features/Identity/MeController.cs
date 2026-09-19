using CoreGrid.Api.Data;
using CoreGrid.Api.Features.Shared.CurrentUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Identity;

// Role resolution for the currently signed-in user (SRS §16.1's `/api/me`).
// Deliberately reads CoreGrid's own `Users.Role` column, not a `roles` claim
// off the token — ThunderID authenticates who the caller is, but CoreGrid's
// own mirror is authoritative for what they're allowed to do. This is also
// what makes role checks immune to which token (ID vs access) a given
// ThunderID application happens to be configured to put `roles` claims on.
[ApiController]
[Route("api/me")]
[Authorize]
public class MeController(CoreGridDbContext db, CurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken)
    {
        // Defensive only: RoleEnrichmentMiddleware already resolves and
        // validates the caller's User row for every non-agent-tools route
        // before any controller runs, returning 401 itself otherwise —
        // this route can never actually be reached with IsResolved false.
        if (!currentUser.IsResolved || currentUser.IsServicePrincipal)
        {
            return Unauthorized();
        }

        // Email/GivenName/FamilyName aren't part of CurrentUserContext (it
        // only carries what authorization decisions need), so this is
        // still one query — reading by the already-resolved Id instead of
        // re-parsing the `sub` claim and looking up by ExternalSubjectId.
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == currentUser.Id, cancellationToken);

        return Ok(new MeResponse(user.Id, user.Email, user.GivenName, user.FamilyName, user.Role, user.IsActive, user.OrganizationId));
    }
}
