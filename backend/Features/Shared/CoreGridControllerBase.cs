using CoreGrid.Api.Data;
using CoreGrid.Api.Features.Shared.CurrentUser;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Shared;

// Provides shared current-user access for feature controllers.
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
