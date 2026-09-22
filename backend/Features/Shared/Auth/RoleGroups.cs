using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Shared.Auth;

// Defines the roles allowed for each authorization policy.
public static class RoleGroups
{
    public static readonly CoreGridRole[] All =
        [CoreGridRole.Staff, CoreGridRole.InventoryOfficer, CoreGridRole.Auditor, CoreGridRole.Administrator];

    public static readonly CoreGridRole[] ReadAssets = All;
    public static readonly CoreGridRole[] ManageAssets = [CoreGridRole.InventoryOfficer, CoreGridRole.Administrator];

   // Roles allowed to verify assets.
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

    // Roles allowed to manage users.
    public static readonly CoreGridRole[] ManageUsers = [CoreGridRole.Administrator];

    public static readonly CoreGridRole[] InitiateWorkflow = [CoreGridRole.InventoryOfficer, CoreGridRole.Administrator];

     // Roles allowed to read workflow information.
    public static readonly CoreGridRole[] ReadWorkflows = All;

    public static readonly CoreGridRole[] ApproveWorkflow = [CoreGridRole.Administrator];

    public static readonly CoreGridRole[] GenerateReports = [CoreGridRole.InventoryOfficer, CoreGridRole.Auditor, CoreGridRole.Administrator];

    // Personal data (each user's own notifications) — any authenticated
    // human role.
    public static readonly CoreGridRole[] ReadNotifications = All;
}
