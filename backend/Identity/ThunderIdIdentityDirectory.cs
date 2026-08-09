namespace CoreGrid.Api.Identity;

// Real implementation of IIdentityDirectory against ThunderID's management
// API. Left unimplemented: the exact request/response contract for creating
// a sub-organisation and a user inside it hasn't been verified against a
// running ThunderID instance yet (see the notes in doc/setup/ThunderID.md).
// Wire this up against ThunderID__ScimClientId / ThunderID__ScimClientSecret
// once that contract is confirmed, rather than guessing it here.
public class ThunderIdIdentityDirectory : IIdentityDirectory
{
    public Task<ProvisionedAdministrator> ProvisionOrganizationAdministratorAsync(
        string organizationName,
        string email,
        string givenName,
        string familyName,
        string password,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException(
            "ThunderID organisation/user provisioning is not wired up yet — see doc/setup/ThunderID.md.");
    }
}
