using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Shared.CurrentUser;

// Scoped, request-lifetime implementation of ICurrentUser. Registered in
// DI (Program.cs) so it can be resolved by CoreGridControllerBase, the
// audit interceptor and the FR-006 org-filter provider once Phase 3 (§4.2)
// switches them onto it — until then this is populated but unread, sitting
// alongside the three still-live lookups it will replace.
public class CurrentUserContext : ICurrentUser
{
    public bool IsResolved { get; private set; }
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public CoreGridRole Role { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public bool IsServicePrincipal { get; private set; }

    public void SetUser(User user)
    {
        IsResolved = true;
        Id = user.Id;
        OrganizationId = user.OrganizationId;
        Role = user.Role;
        DepartmentId = user.DepartmentId;
        IsServicePrincipal = false;
    }

    public void SetServicePrincipal()
    {
        IsResolved = true;
        IsServicePrincipal = true;
    }
}
