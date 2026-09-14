using System.Security.Claims;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
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
//
// Also rehydrates an `organization_id` claim the same way (FR-003) — this is
// the one place in the pipeline that already looks the caller's User row up
// by `sub` on every request, so it's the cheapest place to make OrganizationId
// available downstream without a second lookup. CoreGridDbContext's global
// query filter (FR-006, see CurrentOrganizationProvider) reads this claim.
//
// FR-004: also creates or refreshes the local mirror from token claims on
// the first authenticated request of a session. M0 has no self-service
// signup (SRS §17 is M1) and this deployment's ThunderID instance is
// single-tenant (SRS §4.2) — every account able to authenticate here was
// already provisioned by Setup or an Administrator's invite
// (UsersController.Create), which also assigned the ThunderID-side `roles`
// claim this reads at create time. This "create" branch exists to self-heal
// the case where that provisioning call's own DB write never landed (e.g. a
// crash between ThunderID provisioning and `db.SaveChangesAsync()`), not to
// open self-registration — it refuses to invent a row (and falls back to the
// existing 401) whenever the token doesn't carry everything needed to mirror
// it safely. The "refresh" branch only ever touches profile fields
// (Email/GivenName/FamilyName) — never Role, which stays authoritative from
// CoreGrid's own mirror exactly as the comment above already established.
public class RoleEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, CoreGridDbContext db)
    {
        if (context.User.Identity is ClaimsIdentity { IsAuthenticated: true } identity)
        {
            var externalSubjectId =
                identity.FindFirst("sub")?.Value ??
                identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            User? user = null;
            var mirrorChanged = false;

            if (externalSubjectId is not null)
            {
                user = await db.Users.SingleOrDefaultAsync(
                    u => u.ExternalSubjectId == externalSubjectId,
                    context.RequestAborted);

                if (user is null)
                {
                    user = await TryCreateFromClaimsAsync(db, identity, externalSubjectId, context.RequestAborted);
                    if (user is not null)
                    {
                        db.Users.Add(user);
                        mirrorChanged = true;
                    }
                }
                else if (RefreshProfileFromClaims(user, identity))
                {
                    mirrorChanged = true;
                }
            }

            if (mirrorChanged)
            {
                await db.SaveChangesAsync(context.RequestAborted);
            }

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

            foreach (var orgClaim in identity.FindAll("organization_id").ToList())
            {
                identity.RemoveClaim(orgClaim);
            }

            identity.AddClaim(new Claim("organization_id", user.OrganizationId.ToString()));
        }

        await next(context);
    }

    private static async Task<User?> TryCreateFromClaimsAsync(
        CoreGridDbContext db,
        ClaimsIdentity identity,
        string externalSubjectId,
        CancellationToken cancellationToken)
    {
        var email = identity.FindFirst("email")?.Value ?? identity.FindFirst(ClaimTypes.Email)?.Value;
        var givenName = identity.FindFirst("given_name")?.Value ?? identity.FindFirst(ClaimTypes.GivenName)?.Value;
        var familyName = identity.FindFirst("family_name")?.Value ?? identity.FindFirst(ClaimTypes.Surname)?.Value;
        var roleClaim = identity.FindFirst("roles")?.Value;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(givenName) ||
            string.IsNullOrWhiteSpace(familyName) ||
            !Enum.TryParse<CoreGridRole>(roleClaim, out var role))
        {
            // Not enough on the token to safely mirror this identity —
            // fail closed (401), same as before this method existed.
            return null;
        }

        // M0 is one Organization per deployment (SRS §4.2) — there is
        // nowhere else a first-ever authenticated user could belong.
        var organization = await db.Organizations.SingleOrDefaultAsync(cancellationToken);
        if (organization is null)
        {
            return null;
        }

        return new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            ExternalSubjectId = externalSubjectId,
            Email = email,
            GivenName = givenName,
            FamilyName = familyName,
            Role = role,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    private static bool RefreshProfileFromClaims(User user, ClaimsIdentity identity)
    {
        var email = identity.FindFirst("email")?.Value ?? identity.FindFirst(ClaimTypes.Email)?.Value;
        var givenName = identity.FindFirst("given_name")?.Value ?? identity.FindFirst(ClaimTypes.GivenName)?.Value;
        var familyName = identity.FindFirst("family_name")?.Value ?? identity.FindFirst(ClaimTypes.Surname)?.Value;

        var changed = false;

        if (!string.IsNullOrWhiteSpace(email) && email != user.Email)
        {
            user.Email = email;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(givenName) && givenName != user.GivenName)
        {
            user.GivenName = givenName;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(familyName) && familyName != user.FamilyName)
        {
            user.FamilyName = familyName;
            changed = true;
        }

        return changed;
    }
}
