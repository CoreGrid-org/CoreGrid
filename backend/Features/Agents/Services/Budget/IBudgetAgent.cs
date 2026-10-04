using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services.Budget;

public interface IBudgetAgent
{
    Task<FinancialAssessmentResultDto> RunAssessmentAsync(
        EvaluationScope scope,
        IReadOnlyList<FailureStatisticsDto> maintenanceByAsset,
        CancellationToken cancellationToken = default);
}
