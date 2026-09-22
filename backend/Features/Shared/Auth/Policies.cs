namespace CoreGrid.Api.Features.Shared.Auth;

// Defines the authorization policy names used by the application.
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

    // Defines the policy for agent tool access.
    public const string AgentToolAccess = nameof(AgentToolAccess);
}
