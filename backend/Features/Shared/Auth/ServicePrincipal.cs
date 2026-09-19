using System.Security.Claims;

namespace CoreGrid.Api.Features.Shared.Auth;

// The agent service-principal detection logic that used to live only in
// AgentToolsAuthMiddleware (B3: registered nowhere, so it never actually
// ran — deleted, §6.2). Now the single source of truth for "is this caller
// the agent service principal", read by both CoreGridPolicyHandler (every
// AllowServicePrincipal policy) and RoleEnrichmentMiddleware (to skip the
// Users-row lookup/401 for it while still rehydrating a human caller's
// `roles` claim on the same routes).
public static class ServicePrincipal
{
    public static bool Is(ClaimsPrincipal user)
    {
        if (user.Identity is not ClaimsIdentity { IsAuthenticated: true } identity)
        {
            return false;
        }

        var clientId = identity.FindFirst("client_id")?.Value ?? identity.FindFirst("azp")?.Value;
        var sub = identity.FindFirst("sub")?.Value;
        var gty = identity.FindFirst("gty")?.Value;

        return gty == "client-credentials" ||
               (clientId is not null && sub == clientId) ||
               identity.HasClaim(c => c.Type == "roles" && c.Value == "AgentServicePrincipal") ||
               identity.HasClaim(c => c.Type == "scope" && c.Value.Contains("agent:tools"));
    }
}
