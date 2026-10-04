using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services.Policy;

public interface IPolicyComplianceEvaluator
{
    Task<PolicyOutcome> EvaluateAsync(
        EvaluationScope scope,
        IReadOnlyList<FailureStatisticsDto>? maintenanceByAsset,
        FinancialAssessmentResultDto? budget,
        PolicyEvaluationOptions options,
        CancellationToken cancellationToken);
}
