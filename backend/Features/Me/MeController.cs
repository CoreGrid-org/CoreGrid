using System.Security.Claims;
using CoreGrid.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Me;

// Role resolution for the currently signed-in user (SRS §16.1's `/api/me`).
// Deliberately reads CoreGrid's own `Users.Role` column, not a `roles` claim
// off the token — ThunderID authenticates who the caller is, but CoreGrid's
// own mirror is authoritative for what they're allowed to do. This is also
// what makes role checks immune to which token (ID vs access) a given
// ThunderID application happens to be configured to put `roles` claims on.
[ApiController]
[Route("api/me")]
[Authorize]
public class MeController(CoreGridDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken)
    {
        var externalSubjectId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalSubjectId))
        {
            return Unauthorized();
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.ExternalSubjectId == externalSubjectId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new MeResponse(user.Id, user.Email, user.GivenName, user.FamilyName, user.Role, user.IsActive));
    }
}
