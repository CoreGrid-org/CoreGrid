using System.Security.Claims;
using CoreGrid.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Identity;

// Rehydrates the JWT's `roles` claim from CoreGrid's own Users table on
// every authenticated request, and rejects deactivated users (FR-009).
// ThunderID authenticates identity, but CoreGrid's local Users mirror is
// authoritative for role/authorization (see Domain/Identity/User.cs). Without
// this, an Administrator editing a user's role via UsersController.Update
// would not actually change what [Authorize(Roles = ...)] enforces until the
// caller's token happened to be reissued with a fresh `roles` claim from
// ThunderID — which nothing currently triggers.
public class RoleEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, CoreGridDbContext db)
    {
        if (context.User.Identity is ClaimsIdentity { IsAuthenticated: true } identity)
        {
            var externalSubjectId =
                identity.FindFirst("sub")?.Value ??
                identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var user = externalSubjectId is null
                ? null
                : await db.Users.AsNoTracking().SingleOrDefaultAsync(
                    u => u.ExternalSubjectId == externalSubjectId,
                    context.RequestAborted);

            if (user is null || !user.IsActive)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            foreach (var roleClaim in identity.FindAll("roles").ToList())
            {
                identity.RemoveClaim(roleClaim);
            }

            identity.AddClaim(new Claim("roles", user.Role.ToString()));
        }

        await next(context);
    }
}
