using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.AgentTools.Services;

public interface IBudgetTools
{
    Task<IReadOnlyList<AssetFinancialsDto>> GetFinancialsAsync(AssetSelection selection, CancellationToken cancellationToken = default);

    Task<DepartmentBudgetSummaryDto?> GetDepartmentBudgetSummaryAsync(Guid organizationId, Guid departmentId, int fiscalYear, CancellationToken cancellationToken = default);

    ComputeDepreciationResponse ComputeDepreciation(ComputeDepreciationRequest request);
}
