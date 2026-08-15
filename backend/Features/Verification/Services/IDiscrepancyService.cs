using CoreGrid.Api.Features.Verification.DTOs;

namespace CoreGrid.Api.Features.Verification.Services;

public interface IDiscrepancyService
{
    Task<List<DiscrepancyDto>> GetDiscrepanciesAsync(
        Guid organizationId,
        Guid? campaignId,
        bool onlyOpen);

    Task<DiscrepancyDto?> RaiseManualAsync(
        Guid organizationId,
        Guid taskId,
        Guid currentUserId,
        RaiseDiscrepancyRequest request);

    Task<DiscrepancyDto?> ResolveAsync(
        Guid organizationId,
        Guid discrepancyId,
        Guid currentUserId,
        ResolveDiscrepancyRequest request);
}
