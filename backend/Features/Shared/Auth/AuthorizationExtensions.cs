using Microsoft.AspNetCore.Authorization;

namespace CoreGrid.Api.Features.Shared.Auth;

public static class AuthorizationExtensions
{
    // Registers every named policy from Appendix B / plan §4.4, plus the
    // fail-closed fallback (NFR-10): an endpoint with no [Authorize]
    // attribute at all still requires an authenticated caller, rather than
    // defaulting to anonymous access the way a bare AddAuthorization() does.
    public static IServiceCollection AddCoreGridAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, CoreGridPolicyHandler>();

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())

            // asset:read (Appendix B: Staff/Officer/Auditor/Admin, plus the
            // agent principal per SRS §4.6's asset:read row) — Staff's
            // own-department restriction is a service-layer filter (B14),
            // not a policy condition.
            .AddPolicy(Policies.CanReadAssets, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.ReadAssets, allowServicePrincipal: true)))
            .AddPolicy(Policies.CanManageAssets, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.ManageAssets, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanVerifyAssets, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.VerifyAssets, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanRequestMaintenance, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.RequestMaintenance, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanManageMaintenance, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.ManageMaintenance, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanRequestTransfer, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.RequestTransfer, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanApproveTransfer, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.ApproveTransfer, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanConfirmReceipt, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.ConfirmReceipt, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanRequestDisposal, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.RequestDisposal, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanApproveDisposal, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.ApproveDisposal, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanManageCampaigns, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.ManageCampaigns, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanResolveDiscrepancy, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.ResolveDiscrepancy, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanReadAuditLog, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.ReadAuditLog, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanManageConfiguration, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.ManageConfiguration, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanManageUsers, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.ManageUsers, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanInitiateWorkflow, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.InitiateWorkflow, allowServicePrincipal: false)))

            // workflow:read (SRS §4.6): every human role plus the agent
            // principal, restricted to "own run" for the latter and
            // "status only" for Staff at the service layer.
            .AddPolicy(Policies.CanReadWorkflows, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.ReadWorkflows, allowServicePrincipal: true)))
            .AddPolicy(Policies.CanApproveWorkflow, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.ApproveWorkflow, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanGenerateReports, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.GenerateReports, allowServicePrincipal: false)))
            .AddPolicy(Policies.CanReadNotifications, p => p.Requirements.Add(new CoreGridPolicyRequirement(RoleGroups.ReadNotifications, allowServicePrincipal: false)))

            // Agent tool routes only: no human role satisfies this, ever.
            .AddPolicy(Policies.AgentToolAccess, p => p.Requirements.Add(new CoreGridPolicyRequirement([], allowServicePrincipal: true)));

        return services;
    }
}
