namespace CoreGrid.Api.Identity;

// Abstraction over the external identity provider (SRS §4.10). Isolates the
// one piece of the setup flow that talks to ThunderID's own management API,
// so the contingency local-identity fallback in §4.10 is a swap of this
// implementation, not a rewrite of the setup/user-management endpoints.
//
// CoreGrid is self-hosted once per department (SRS §2.4, §4.2), and this
// deployment's ThunderID instance is single-tenant to match: every user is
// created in the same organisation unit. There is no cross-department
// isolation to enforce here, because no two departments share a deployment.
public interface IIdentityDirectory
{
    // Creates the ThunderID account for this deployment's first
    // Administrator, assigned the CoreGrid Administrator role (SRS §4.7).
    // Returns the ThunderID "sub" claim value to mirror onto the local User row.
    Task<string> ProvisionAdministratorAsync(
        string email,
        string givenName,
        string familyName,
        string password,
        CancellationToken cancellationToken);
}
