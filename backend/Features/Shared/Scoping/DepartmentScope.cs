using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared.CurrentUser;

namespace CoreGrid.Api.Features.Shared.Scoping;

// FR-086 / Appendix B: "Staff are restricted to their own department by a
// service-layer filter." Administrator and Auditor are org-wide roles;
// Staff and InventoryOfficer are restricted to their own department —
// including the case where they have none assigned, which must show
// nothing rather than everything. Moved out of Features/Dashboard (its
// original, narrower home) so Phase 3 can apply the same rule to the
// Assets/Maintenance/Transfers/Disposals lists Appendix B also names (B14).
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
