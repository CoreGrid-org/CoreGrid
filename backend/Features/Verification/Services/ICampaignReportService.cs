using CoreGrid.Api.Features.Verification.DTOs;

namespace CoreGrid.Api.Features.Verification.Services;

public interface ICampaignReportService
{
    Task<CampaignReportDto?> GetReportAsync(Guid organizationId, Guid campaignId, CancellationToken cancellationToken);

    byte[] BuildCsv(CampaignReportDto report);

    byte[] BuildPdf(CampaignReportDto report);
}
