using System;
using System.Threading;
using System.Threading.Tasks;
using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.AgentTools.Services;

public interface IAgentToolsService
{
    Task<AssetSummaryDto?> GetAssetSummaryAsync(Guid organizationId, Guid assetId, CancellationToken cancellationToken = default);

    Task<AssetFinancialsDto?> GetAssetFinancialsAsync(Guid organizationId, Guid assetId, CancellationToken cancellationToken = default);

    Task<DepartmentBudgetSummaryDto?> GetDepartmentBudgetSummaryAsync(Guid organizationId, Guid departmentId, int fiscalYear, CancellationToken cancellationToken = default);

    ComputeDepreciationResponse ComputeDepreciation(ComputeDepreciationRequest request);

    Task<OrganizationPolicyFactsDto?> GetOrganizationPoliciesAsync(Guid organizationId, Guid? assetTypeId, CancellationToken cancellationToken = default);

    Task<AssetComplianceStateDto?> GetAssetComplianceStateAsync(Guid organizationId, Guid assetId, CancellationToken cancellationToken = default);

    // Fleet variants: every active (non-disposed) asset of the type, or just
    // assetId when given — a fixed number of queries however large the fleet.
    Task<AssetTypeSummaryDto?> GetAssetTypeSummaryAsync(Guid organizationId, Guid assetTypeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssetFinancialsDto>> GetFleetFinancialsAsync(Guid organizationId, Guid assetTypeId, Guid? assetId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssetComplianceStateDto>> GetFleetComplianceStateAsync(Guid organizationId, Guid assetTypeId, Guid? assetId, CancellationToken cancellationToken = default);
}
