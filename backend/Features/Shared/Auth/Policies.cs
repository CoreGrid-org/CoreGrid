namespace CoreGrid.Api.Features.Shared.Auth;

// The named policies from SRS Appendix B / plan §4.4. Defined and
// registered here (AuthorizationExtensions) starting Phase 2, but not yet
// referenced by any controller's [Authorize(Policy = ...)] — that migration,
// controller by controller, is Phase 3 (§5). Until then every route keeps
// enforcing exactly what its existing [Authorize(Roles = "...")] says.
public static class Policies
{
    public const string CanReadAssets = nameof(CanReadAssets);
    public const string CanManageAssets = nameof(CanManageAssets);
    public const string CanVerifyAssets = nameof(CanVerifyAssets);
    public const string CanRequestMaintenance = nameof(CanRequestMaintenance);
    public const string CanManageMaintenance = nameof(CanManageMaintenance);
    public const string CanRequestTransfer = nameof(CanRequestTransfer);
    public const string CanApproveTransfer = nameof(CanApproveTransfer);
    public const string CanConfirmReceipt = nameof(CanConfirmReceipt);
    public const string CanRequestDisposal = nameof(CanRequestDisposal);
    public const string CanApproveDisposal = nameof(CanApproveDisposal);
    public const string CanManageCampaigns = nameof(CanManageCampaigns);
    public const string CanResolveDiscrepancy = nameof(CanResolveDiscrepancy);
    public const string CanReadAuditLog = nameof(CanReadAuditLog);
    public const string CanManageConfiguration = nameof(CanManageConfiguration);
    public const string CanManageUsers = nameof(CanManageUsers);
    public const string CanInitiateWorkflow = nameof(CanInitiateWorkflow);
    public const string CanReadWorkflows = nameof(CanReadWorkflows);
    public const string CanApproveWorkflow = nameof(CanApproveWorkflow);
    public const string CanGenerateReports = nameof(CanGenerateReports);
    public const string CanReadNotifications = nameof(CanReadNotifications);

    // Read-only tool routes only; every write policy above is built with
    // allowServicePrincipal: false, so the agent principal is denied all of
    // them regardless of any role claim it might present (SEC-ID-10, AI-28).
    public const string AgentToolAccess = nameof(AgentToolAccess);
}
