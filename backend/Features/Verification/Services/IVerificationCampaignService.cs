using CoreGrid.Api.Features.Verification.DTOs;

namespace CoreGrid.Api.Features.Verification.Services;

public interface IVerificationCampaignService
{
    Task<List<CampaignDto>> GetCampaignsAsync(Guid organizationId);

    Task<CampaignDto?> GetCampaignByIdAsync(Guid organizationId, Guid id);

    Task<CampaignDto> CreateCampaignAsync(
        Guid organizationId,
        Guid userId,
        CreateCampaignRequest request);

    Task<CampaignDto?> UpdateCampaignAsync(
        Guid organizationId,
        Guid id,
        UpdateCampaignRequest request);

    Task<bool> DeleteCampaignAsync(Guid organizationId, Guid id);
}
