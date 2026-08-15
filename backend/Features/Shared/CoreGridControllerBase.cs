using System.Security.Claims;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Shared;

// Shared current-user resolution used across feature controllers. Same
// pattern as MeController: resolves the caller's CoreGrid user by the
// token's `sub` claim, since ThunderID authenticates identity but CoreGrid's
// own Users mirror is authoritative for OrganizationId/role scoping.
public abstract class CoreGridControllerBase(CoreGridDbContext db) : ControllerBase
{
    protected async Task<User?> GetCurrentUserAsync(
        CancellationToken cancellationToken)
    {
        var externalSubjectId =
            User.FindFirst("sub")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(externalSubjectId))
        {
            return null;
        }

        return await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                u => u.ExternalSubjectId == externalSubjectId,
                cancellationToken);
    }
}
