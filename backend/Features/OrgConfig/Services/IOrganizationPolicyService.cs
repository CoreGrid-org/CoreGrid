using CoreGrid.Api.Features.OrgConfig.DTOs;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Paging;

namespace CoreGrid.Api.Features.OrgConfig.Services;

public interface IOrganizationPolicyService
{
    Task<PagedResult<OrganizationPolicyDto>> GetPoliciesAsync(Guid organizationId, PagedQuery query, CancellationToken cancellationToken);

    Task<OrganizationPolicyDto?> GetPolicyByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<OrganizationPolicyDto> CreatePolicyAsync(
        Guid organizationId,
        Guid? userId,
        SaveOrganizationPolicyRequest request,
        CancellationToken cancellationToken);

    Task<OrganizationPolicyDto?> UpdatePolicyAsync(
        Guid organizationId,
        Guid id,
        Guid? userId,
        SaveOrganizationPolicyRequest request,
        CancellationToken cancellationToken);
}
