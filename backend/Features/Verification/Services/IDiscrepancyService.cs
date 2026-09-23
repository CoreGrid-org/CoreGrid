using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Verification.DTOs;

namespace CoreGrid.Api.Features.Verification.Services;

public interface IDiscrepancyService
{
    Task<PagedResult<DiscrepancyDto>> GetDiscrepanciesAsync(
        Guid organizationId,
        DiscrepancyQueryParameters query,
        CancellationToken cancellationToken);

    // B18: a real single-row query, not GetDiscrepanciesAsync(...).FirstOrDefault(...).
    Task<DiscrepancyDto?> RaiseManualAsync(
        Guid organizationId,
        Guid taskId,
        Guid currentUserId,
        RaiseDiscrepancyRequest request,
        CancellationToken cancellationToken);

    Task<DiscrepancyDto?> ResolveAsync(
        Guid organizationId,
        Guid discrepancyId,
        Guid currentUserId,
        ResolveDiscrepancyRequest request,
        CancellationToken cancellationToken);
}
