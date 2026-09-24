using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Shared.CurrentUser;

// Stores the current user's request-scoped context.
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
