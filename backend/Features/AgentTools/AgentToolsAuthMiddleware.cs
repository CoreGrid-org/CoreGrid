using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CoreGrid.Api.Features.AgentTools;

/// <summary>
/// Scoped middleware applied exclusively to /api/agent-tools/* routes.
/// Allows ThunderID machine-to-machine service principal tokens to pass through
/// without requiring a human user record in CoreGrid's Users table mirror,
/// keeping the global RoleEnrichmentMiddleware untouched for all other controllers.
/// </summary>
public class AgentToolsAuthMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/agent-tools", StringComparison.OrdinalIgnoreCase))
        {
            if (context.User.Identity is ClaimsIdentity { IsAuthenticated: true } identity)
            {
                var clientId = identity.FindFirst("client_id")?.Value ?? identity.FindFirst("azp")?.Value;
                var sub = identity.FindFirst("sub")?.Value;
                var gty = identity.FindFirst("gty")?.Value;

                var isServicePrincipal = gty == "client-credentials" ||
                                         (clientId is not null && sub == clientId) ||
                                         identity.HasClaim(c => c.Type == "roles" && c.Value == "AgentServicePrincipal") ||
                                         identity.HasClaim(c => c.Type == "scope" && c.Value.Contains("agent:tools"));

                if (isServicePrincipal)
                {
                    // Tag request so downstream handlers know it's a valid service principal
                    context.Items["IsAgentServicePrincipal"] = true;
                }
            }
        }

        await next(context);
    }
}
