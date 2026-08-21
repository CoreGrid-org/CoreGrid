using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace backend.Tests.Features.Authorization;

// Replaces real ThunderID JWT validation inside the test host. A request
// carrying the "Test-Sub" header authenticates as that `sub` — real
// authorization then flows through the real pipeline (RoleEnrichmentMiddleware
// re-derives the role from CoreGrid's own Users table, exactly like
// production), so this only fakes *identity*, never *authorization*. A
// request carrying "Test-ServicePrincipal: true" instead gets the same
// client-credentials-shaped claims AgentToolsAuthMiddleware looks for
// (client_id, gty), with no `sub` matching any Users row — proving the
// agent service principal path doesn't require one (AI-28).
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>();

        if (Request.Headers.TryGetValue("Test-ServicePrincipal", out var isServicePrincipal) && isServicePrincipal == "true")
        {
            claims.Add(new Claim("client_id", "test-agent-service"));
            claims.Add(new Claim("azp", "test-agent-service"));
            claims.Add(new Claim("gty", "client-credentials"));
        }
        else if (Request.Headers.TryGetValue("Test-Sub", out var sub))
        {
            claims.Add(new Claim("sub", sub.ToString()));
        }
        else
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // RoleClaimType must match Program.cs's JwtBearer config ("roles") —
        // otherwise [Authorize(Roles = ...)] never finds the role claim
        // RoleEnrichmentMiddleware adds, and every role check fails closed.
        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, "roles");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
