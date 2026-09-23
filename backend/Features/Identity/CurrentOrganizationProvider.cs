using CoreGrid.Api.Features.Shared.CurrentUser;

namespace CoreGrid.Api.Features.Identity;

// Provides the organization ID for the current user.
public class CurrentOrganizationProvider(CurrentUserContext currentUserContext) : ICurrentOrganizationProvider
{
    public Guid? OrganizationId =>
        currentUserContext.IsResolved && !currentUserContext.IsServicePrincipal
            ? currentUserContext.OrganizationId
            : null;
}
