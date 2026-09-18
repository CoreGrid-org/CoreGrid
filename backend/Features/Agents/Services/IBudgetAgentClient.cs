using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

/// <summary>
/// Client contract for the Budget Analysis Agent (SRS §7.3, Node 3).
/// Evaluates lifecycle options (REPAIR, REPLACE, TRANSFER, DISPOSE) against
/// asset financials, department budget constraints, and maintenance statistics.
/// </summary>
public interface IBudgetAgentClient
{
    Task<FinancialAssessmentResultDto> RunAssessmentAsync(
        Guid organizationId,
        Guid assetId,
        Guid? departmentId,
        int? fiscalYear,
        FailureStatisticsDto maintenanceAnalysis,
        CancellationToken cancellationToken = default);
}
