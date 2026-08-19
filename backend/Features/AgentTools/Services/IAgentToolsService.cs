using System;
using System.Threading;
using System.Threading.Tasks;
using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.AgentTools.Services;

public interface IAgentToolsService
{
    Task<AssetFinancialsDto?> GetAssetFinancialsAsync(Guid organizationId, Guid assetId, CancellationToken cancellationToken = default);

    Task<DepartmentBudgetSummaryDto?> GetDepartmentBudgetSummaryAsync(Guid organizationId, Guid departmentId, int fiscalYear, CancellationToken cancellationToken = default);

    ComputeDepreciationResponse ComputeDepreciation(ComputeDepreciationRequest request);
}
