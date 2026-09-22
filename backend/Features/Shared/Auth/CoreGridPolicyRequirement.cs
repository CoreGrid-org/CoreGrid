using CoreGrid.Api.Domain;
using Microsoft.AspNetCore.Authorization;

namespace CoreGrid.Api.Features.Shared.Auth;

// Defines the roles and service-principal access allowed by a policy.
public class CoreGridPolicyRequirement(IReadOnlyCollection<CoreGridRole> allowedRoles, bool allowServicePrincipal) : IAuthorizationRequirement
{
    public IReadOnlyCollection<CoreGridRole> AllowedRoles { get; } = allowedRoles;
    public bool AllowServicePrincipal { get; } = allowServicePrincipal;
}

public class CoreGridPolicyHandler : AuthorizationHandler<CoreGridPolicyRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CoreGridPolicyRequirement requirement)
    {
       // Grants access to an authorized service principal or role.
        if (requirement.AllowServicePrincipal && ServicePrincipal.Is(context.User))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        foreach (var role in requirement.AllowedRoles)
        {
            if (context.User.IsInRole(role.ToString()))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
}
