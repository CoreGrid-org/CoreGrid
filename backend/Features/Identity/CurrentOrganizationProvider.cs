using CoreGrid.Api.Features.Shared.CurrentUser;

namespace CoreGrid.Api.Features.Identity;

// §4.2: reads the CurrentUserContext RoleEnrichmentMiddleware already
// populates, instead of re-parsing the `organization_id` claim itself —
// same value, same timing (both are only meaningful after
// RoleEnrichmentMiddleware has run), one fewer independent source of
// truth for "what org is this request in".
public class CurrentOrganizationProvider(CurrentUserContext currentUserContext) : ICurrentOrganizationProvider
{
    public Guid? OrganizationId =>
        currentUserContext.IsResolved && !currentUserContext.IsServicePrincipal
            ? currentUserContext.OrganizationId
            : null;
}
