using CoreGrid.Api.Features.AgentTools.DTOs;

namespace CoreGrid.Api.Features.AgentTools.Services;

public interface IMaintenanceTools
{
    Task<MaintenanceHistoryDto?> GetMaintenanceHistoryAsync(Guid organizationId, Guid assetId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FailureStatisticsDto>> GetFailureStatisticsAsync(AssetSelection selection, CancellationToken cancellationToken = default);
}
