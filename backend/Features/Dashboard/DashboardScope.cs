using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Dashboard;

// FR-086: "every dashboard figure and report shall be computed within the
// caller's organisation and restricted to the departments their role
// permits them to see." Administrator and Auditor are org-wide roles (audit
// and administration both need to see every department); Staff and
// InventoryOfficer are restricted to their own — including the case where
// they have none assigned, which must show nothing rather than everything.
public readonly record struct DepartmentScope(bool IsRestricted, Guid? DepartmentId)
{
    public static readonly DepartmentScope Unrestricted = new(false, null);

    public static DepartmentScope Restricted(Guid? departmentId) => new(true, departmentId);
}

public static class DashboardScope
{
    public static DepartmentScope Resolve(User user) =>
        user.Role is CoreGridRole.Administrator or CoreGridRole.Auditor
            ? DepartmentScope.Unrestricted
            : DepartmentScope.Restricted(user.DepartmentId);
}
