using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

/// <summary>
/// Client contract for the Budget Analysis Agent (SRS §7.3, Node 3).
/// Triages every asset in scope financially (deterministic), then ranks the
/// lifecycle options (REPAIR, REPLACE, TRANSFER, DISPOSE, RETAIN) for the scope.
/// </summary>
public interface IBudgetAgentClient
{
    Task<FinancialAssessmentResultDto> RunAssessmentAsync(
        EvaluationScope scope,
        IReadOnlyList<FailureStatisticsDto> maintenanceByAsset,
        CancellationToken cancellationToken = default);
}
