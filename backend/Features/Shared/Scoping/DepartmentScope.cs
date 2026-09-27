using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared.CurrentUser;

namespace CoreGrid.Api.Features.Shared.Scoping;

// Defines the department scope for role-based data access. SRS §4.6 /
// Appendix B: only Staff are restricted to their own department — Inventory
// Officer, Auditor and Administrator all hold organisation-wide asset:read.
// (Officer used to be restricted too, which hid an incoming transfer from
// the destination officer who has to confirm its receipt, since the asset
// still sits in the source department until they do.)
public readonly record struct DepartmentScope(bool IsRestricted, Guid? DepartmentId)
{
    public static readonly DepartmentScope Unrestricted = new(false, null);

    public static DepartmentScope Restricted(Guid? departmentId) => new(true, departmentId);

    public static DepartmentScope For(User user) =>
        user.Role is CoreGridRole.Staff
            ? Restricted(user.DepartmentId)
            : Unrestricted;

    public static DepartmentScope For(ICurrentUser currentUser) =>
        currentUser.Role is CoreGridRole.Staff
            ? Restricted(currentUser.DepartmentId)
            : Unrestricted;
}
