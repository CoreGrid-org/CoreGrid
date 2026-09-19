using CoreGrid.Api.Domain;
using Microsoft.AspNetCore.Authorization;

namespace CoreGrid.Api.Features.Shared.Auth;

// A policy is satisfied by any one of a fixed set of human roles, or by
// the agent service principal when explicitly allowed — never both a role
// AND service-principal check layered separately, so every policy's
// membership is legible in one place (Policies.cs) rather than split
// across [Authorize(Roles=...)] plus an ad hoc middleware flag (B3-B5).
public class CoreGridPolicyRequirement(IReadOnlyCollection<CoreGridRole> allowedRoles, bool allowServicePrincipal) : IAuthorizationRequirement
{
    public IReadOnlyCollection<CoreGridRole> AllowedRoles { get; } = allowedRoles;
    public bool AllowServicePrincipal { get; } = allowServicePrincipal;
}

public class CoreGridPolicyHandler : AuthorizationHandler<CoreGridPolicyRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CoreGridPolicyRequirement requirement)
    {
        // SEC-ID-10 / AI-28: a write policy is built with
        // allowServicePrincipal: false (see Policies.cs) — there is no
        // branch here that can accidentally grant a service principal a
        // permission its policy didn't explicitly allow.
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
