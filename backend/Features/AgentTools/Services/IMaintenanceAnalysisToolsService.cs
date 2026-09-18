using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.AgentTools.Services;

public interface IMaintenanceAnalysisToolsService
{
    Task<MaintenanceHistoryDto?> GetMaintenanceHistoryAsync(Guid organizationId, Guid assetId, CancellationToken cancellationToken);

    Task<FailureStatisticsDto?> ComputeFailureStatisticsAsync(Guid organizationId, Guid assetId, CancellationToken cancellationToken);
}
