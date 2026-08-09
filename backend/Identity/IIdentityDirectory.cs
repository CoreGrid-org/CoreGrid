namespace CoreGrid.Api.Identity;

// Abstraction over the external identity provider (SRS §4.10). Isolates the
// one piece of the setup flow that talks to ThunderID's own management API,
// so the contingency local-identity fallback in §4.10 is a swap of this
// implementation, not a rewrite of the setup/user-management endpoints.
public interface IIdentityDirectory
{
    // Creates the ThunderID sub-organisation for a new tenant institution and
    // the first Administrator's ThunderID account inside it, assigned the
    // CoreGrid Administrator role (SRS §4.2, §4.7).
    Task<ProvisionedAdministrator> ProvisionOrganizationAdministratorAsync(
        string organizationName,
        string email,
        string givenName,
        string familyName,
        string password,
        CancellationToken cancellationToken);
}

public record ProvisionedAdministrator(string ExternalOrgId, string ExternalSubjectId);
