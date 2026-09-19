using CoreGrid.Api.Data;
using CoreGrid.Api.Features.Shared.CurrentUser;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Shared;

// Shared current-user resolution used across feature controllers. Resolved
// via CurrentUserContext (Features/Shared/CurrentUser) — the `sub`-claim
// lookup RoleEnrichmentMiddleware already performs once per request — not
// a second DB query the way this used to work (B15: this exact lookup was
// previously re-run per controller, per audit save and again in
// MeController, three round-trips for one piece of information).
// HttpContext.RequestServices is used instead of constructor injection so
// this stays a same-file change: none of this base class's ~15 subclasses'
// constructors need to change to pick it up.
public abstract class CoreGridControllerBase(CoreGridDbContext db) : ControllerBase
{
    protected readonly CoreGridDbContext Db = db;

    protected Task<ICurrentUser?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var currentUser = HttpContext.RequestServices.GetRequiredService<CurrentUserContext>();

        return Task.FromResult(
            currentUser.IsResolved && !currentUser.IsServicePrincipal
                ? (ICurrentUser?)currentUser
                : null);
    }
}
