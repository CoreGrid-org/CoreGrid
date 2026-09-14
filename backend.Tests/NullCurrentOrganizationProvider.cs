using CoreGrid.Api.Identity;

namespace backend.Tests;

// Test double for CoreGridDbContext's global query filter (FR-006). These
// tests construct CoreGridDbContext directly, outside the HTTP pipeline that
// would otherwise populate the `organization_id` claim, and exercise each
// service's own explicit organizationId parameter instead — so the filter
// should always bypass (null = "no org context yet"), leaving org scoping
// entirely up to the service code under test, same as before this filter existed.
public class NullCurrentOrganizationProvider : ICurrentOrganizationProvider
{
    public Guid? OrganizationId => null;
}
