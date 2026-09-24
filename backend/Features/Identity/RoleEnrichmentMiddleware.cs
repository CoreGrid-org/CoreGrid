using System.Security.Claims;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared.Auth;
using CoreGrid.Api.Features.Shared.CurrentUser;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Identity;

// Enriches authenticated requests with the local user role and context.
public class RoleEnrichmentMiddleware(RequestDelegate next)
{
// Populates the shared user context for the current request.
    public async Task InvokeAsync(HttpContext context, CoreGridDbContext db, CurrentUserContext currentUserContext, ILogger<RoleEnrichmentMiddleware> logger)
    {
        if (context.User.Identity is ClaimsIdentity { IsAuthenticated: true } identity)
        {
           // Service principals use their token claims and do not require a local user record.
            if (ServicePrincipal.Is(context.User))
            {
                currentUserContext.SetServicePrincipal();
                await next(context);
                return;
            }

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
                // Rejects requests without an active local user.
                logger.LogWarning(
                    "Authorization outcome {StatusCode} for {Subject} (no active CoreGrid user) on {Method} {Path} at {Timestamp:o}",
                    StatusCodes.Status401Unauthorized, externalSubjectId ?? "unknown", context.Request.Method, context.Request.Path, DateTimeOffset.UtcNow);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            currentUserContext.SetUser(user);

            foreach (var roleClaim in identity.FindAll("roles").ToList())
            {
                identity.RemoveClaim(roleClaim);
            }

            identity.AddClaim(new Claim("roles", user.Role.ToString()));
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
