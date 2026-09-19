using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Shared.Auth;

// The role membership behind each named policy in Policies.cs, read
// straight off SRS §4.6's permission matrix and Appendix B — one place
// instead of the 16 duplicated per-controller `ReadRoles`/`ManageRoles`/
// etc. string constants (B17) Phase 3 replaces as each controller moves
// onto these policies.
public static class RoleGroups
{
    public static readonly CoreGridRole[] All =
        [CoreGridRole.Staff, CoreGridRole.InventoryOfficer, CoreGridRole.Auditor, CoreGridRole.Administrator];

    public static readonly CoreGridRole[] ReadAssets = All;
    public static readonly CoreGridRole[] ManageAssets = [CoreGridRole.InventoryOfficer, CoreGridRole.Administrator];

    // Deviation from Appendix B kept and recorded (plan §4.4): Administrator
    // already passes every verification-related check today (canActOnAnyTask),
    // so Administrator is included alongside Officer/Auditor rather than
    // narrowing existing behaviour.
    public static readonly CoreGridRole[] VerifyAssets = [CoreGridRole.InventoryOfficer, CoreGridRole.Auditor, CoreGridRole.Administrator];

    public static readonly CoreGridRole[] RequestMaintenance = [CoreGridRole.Staff, CoreGridRole.InventoryOfficer, CoreGridRole.Administrator];
    public static readonly CoreGridRole[] ManageMaintenance = [CoreGridRole.InventoryOfficer, CoreGridRole.Administrator];

    public static readonly CoreGridRole[] RequestTransfer = [CoreGridRole.InventoryOfficer, CoreGridRole.Administrator];
    public static readonly CoreGridRole[] ApproveTransfer = [CoreGridRole.Administrator];

    // Deviation from Appendix B kept and recorded (plan §4.4): Administrator
    // already passes this check today too.
    public static readonly CoreGridRole[] ConfirmReceipt = [CoreGridRole.InventoryOfficer, CoreGridRole.Administrator];

    public static readonly CoreGridRole[] RequestDisposal = [CoreGridRole.InventoryOfficer, CoreGridRole.Administrator];
    public static readonly CoreGridRole[] ApproveDisposal = [CoreGridRole.Administrator];

    public static readonly CoreGridRole[] ManageCampaigns = [CoreGridRole.Auditor, CoreGridRole.Administrator];
    public static readonly CoreGridRole[] ResolveDiscrepancy = [CoreGridRole.Auditor, CoreGridRole.Administrator];
    public static readonly CoreGridRole[] ReadAuditLog = [CoreGridRole.Auditor, CoreGridRole.Administrator];

    public static readonly CoreGridRole[] ManageConfiguration = [CoreGridRole.Administrator];

    // Appendix B / SRS §4.6 makes user:manage Administrator-only.
    // GET /api/users additionally stays readable by InventoryOfficer today
    // (assignee picker) — a deliberate deviation recorded in the plan
    // (§12.4), applied directly on that one action in Phase 3 rather than
    // widened here, so CanManageUsers itself stays exactly what the SRS says.
    public static readonly CoreGridRole[] ManageUsers = [CoreGridRole.Administrator];

    public static readonly CoreGridRole[] InitiateWorkflow = [CoreGridRole.InventoryOfficer, CoreGridRole.Administrator];

    // "Status only" for Staff and "own run" for the agent principal are
    // resource-level restrictions applied in the service layer, not by the
    // policy — every human role can reach the route (SRS §4.6's
    // workflow:read row).
    public static readonly CoreGridRole[] ReadWorkflows = All;

    public static readonly CoreGridRole[] ApproveWorkflow = [CoreGridRole.Administrator];

    public static readonly CoreGridRole[] GenerateReports = [CoreGridRole.InventoryOfficer, CoreGridRole.Auditor, CoreGridRole.Administrator];

    // Personal data (each user's own notifications) — any authenticated
    // human role.
    public static readonly CoreGridRole[] ReadNotifications = All;
}
