using System.Security.Claims;

namespace CoreGrid.Api.Features.Shared.Auth;

// Identifies whether the authenticated caller is the agent service principal.
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
