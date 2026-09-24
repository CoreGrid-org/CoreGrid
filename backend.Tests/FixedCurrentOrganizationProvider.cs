using CoreGrid.Api.Features.Identity;

namespace backend.Tests;

// Unlike NullCurrentOrganizationProvider (which always bypasses the FR-006
// query filter), this returns a fixed, real OrganizationId — for the one
// class of test that needs the filter to actually apply, not bypass:
// proving a query against an entity with no OrganizationId column of its
// own (AssetAttributeDefinition, AssetAttributeValue, AgentExecutionStep,
// AgentApproval) is still org-scoped through its required parent navigation.
public class FixedCurrentOrganizationProvider(Guid organizationId) : ICurrentOrganizationProvider
{
    public Guid? OrganizationId => organizationId;
}
