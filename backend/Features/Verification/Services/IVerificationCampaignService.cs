using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Verification.DTOs;

namespace CoreGrid.Api.Features.Verification.Services;

public interface IVerificationCampaignService
{
    Task<PagedResult<CampaignDto>> GetCampaignsAsync(Guid organizationId, CampaignQueryParameters query, CancellationToken cancellationToken);

    // B18: a real single-row query, not GetCampaignsAsync(...).FirstOrDefault(...).
    Task<CampaignDto?> GetCampaignByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<CampaignDto> CreateCampaignAsync(
        Guid organizationId,
        Guid userId,
        CreateCampaignRequest request,
        CancellationToken cancellationToken);

    Task<CampaignDto?> UpdateCampaignAsync(
        Guid organizationId,
        Guid id,
        UpdateCampaignRequest request,
        CancellationToken cancellationToken);

    Task<bool> DeleteCampaignAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);
}
