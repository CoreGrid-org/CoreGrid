using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Shared.CurrentUser;

// The one shape every feature reads the caller through, once Phase 3 wires
// controllers onto it. Populated once per request (by RoleEnrichmentMiddleware,
// which already performs this exact `sub` lookup) instead of the three
// separate DB round-trips B15 found: CoreGridControllerBase.GetCurrentUserAsync,
// Data/Auditing/CurrentUserAccessor and MeController's own copy.
public interface ICurrentUser
{
    // False until RoleEnrichmentMiddleware has resolved a request's caller
    // — unauthenticated requests and /api/setup/* leave this false. Every
    // member below is meaningless while this is false.
    bool IsResolved { get; }

    Guid Id { get; }
    Guid OrganizationId { get; }
    CoreGridRole Role { get; }
    Guid? DepartmentId { get; }

    // True for the agent service principal (SRS §4.2's fifth actor),
    // detected by RoleEnrichmentMiddleware via ServicePrincipal.Is and set
    // here instead of the old AgentToolsAuthMiddleware's HttpContext.Items
    // flag (B3, deleted). A service principal has no Users row, so
    // Id/OrganizationId/Role/DepartmentId do not apply to it.
    bool IsServicePrincipal { get; }
}
