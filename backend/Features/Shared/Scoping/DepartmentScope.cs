using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared.CurrentUser;

namespace CoreGrid.Api.Features.Shared.Scoping;

// Defines the department scope for role-based data access.
public readonly record struct DepartmentScope(bool IsRestricted, Guid? DepartmentId)
{
    public static readonly DepartmentScope Unrestricted = new(false, null);

    public static DepartmentScope Restricted(Guid? departmentId) => new(true, departmentId);

    public static DepartmentScope For(User user) =>
        user.Role is CoreGridRole.Administrator or CoreGridRole.Auditor
            ? Unrestricted
            : Restricted(user.DepartmentId);

    public static DepartmentScope For(ICurrentUser currentUser) =>
        currentUser.Role is CoreGridRole.Administrator or CoreGridRole.Auditor
            ? Unrestricted
            : Restricted(currentUser.DepartmentId);
}
