namespace CoreGrid.Api.Data.Auditing;

// Resolves the CoreGrid user mirroring the current request's caller, the
// same "by sub claim" lookup every controller in this codebase already does
// individually (see MeController, CoreGridControllerBase) — centralised here
// so the audit interceptor doesn't have to re-run it, and so it only runs
// once per request regardless of how many entities a request mutates.
public interface ICurrentUserAccessor
{
    Task<(Guid? UserId, Guid? OrganizationId)> GetCurrentUserAsync(CancellationToken cancellationToken);
}
