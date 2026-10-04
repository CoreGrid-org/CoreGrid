using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.AgentTools.Services;

public interface IMaintenanceAnalysisToolsService
{
    Task<MaintenanceHistoryDto?> GetMaintenanceHistoryAsync(Guid organizationId, Guid assetId, CancellationToken cancellationToken);

    Task<FailureStatisticsDto?> ComputeFailureStatisticsAsync(Guid organizationId, Guid assetId, CancellationToken cancellationToken);

    // Per-asset statistics for every active asset of the type (or just assetId), in two queries.
    Task<IReadOnlyList<FailureStatisticsDto>> ComputeFleetFailureStatisticsAsync(Guid organizationId, Guid assetTypeId, Guid? assetId, CancellationToken cancellationToken);
}
