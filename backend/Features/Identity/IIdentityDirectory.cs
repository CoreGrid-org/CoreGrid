using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Identity;

// Provides access to identity management operations.

public interface IIdentityDirectory
{
  // Provisions a user with the specified role.
    Task<string> ProvisionUserAsync(
        string email,
        string givenName,
        string familyName,
        string password,
        CoreGridRole role,
        CancellationToken cancellationToken);
}
