using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.AgentTools.Services;

public interface IPolicyTools
{
    Task<OrganizationPolicyFactsDto?> GetOrganizationPoliciesAsync(Guid organizationId, Guid? assetTypeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssetComplianceStateDto>> GetComplianceStateAsync(AssetSelection selection, CancellationToken cancellationToken = default);
}
