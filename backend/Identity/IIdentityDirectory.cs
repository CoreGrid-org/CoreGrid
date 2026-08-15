using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Identity;

// Abstraction over the external identity provider (SRS §4.10). Isolates the
// one piece of the setup/user-management flows that talks to ThunderID's own
// management API, so the contingency local-identity fallback in §4.10 is a
// swap of this implementation, not a rewrite of the calling endpoints.
//
// In M0, CoreGrid is self-hosted once per customer organisation (SRS §2.4,
// §4.2), and this deployment's ThunderID instance is single-tenant to
// match: every user is created in the same organisation unit. There is no
// cross-customer isolation to enforce here, because no two customers share
// a deployment.
public interface IIdentityDirectory
{
    // Creates a ThunderID account and assigns it the given CoreGrid role
    // (SRS §4.7) — used both for the first Administrator during Setup and
    // for every user an Administrator creates afterwards from the dashboard.
    // Returns the ThunderID "sub" claim value to mirror onto the local User row.
    Task<string> ProvisionUserAsync(
        string email,
        string givenName,
        string familyName,
        string password,
        CoreGridRole role,
        CancellationToken cancellationToken);
}
