using CoreGrid.Api.Features.OrgConfig.DTOs;

namespace CoreGrid.Api.Features.OrgConfig.Services;

public interface IOrganizationPolicyService
{
    Task<List<OrganizationPolicyDto>> GetPoliciesAsync(Guid organizationId);

    Task<OrganizationPolicyDto?> GetPolicyByIdAsync(Guid organizationId, Guid id);

    Task<OrganizationPolicyDto> CreatePolicyAsync(
        Guid organizationId,
        Guid? userId,
        SaveOrganizationPolicyRequest request);

    Task<OrganizationPolicyDto?> UpdatePolicyAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        SaveOrganizationPolicyRequest request);
}
