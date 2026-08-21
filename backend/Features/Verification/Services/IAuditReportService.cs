using CoreGrid.Api.Features.Verification.DTOs;

namespace CoreGrid.Api.Features.Verification.Services;

public interface IAuditReportService
{
    Task<AuditReportDto> GetReportAsync(Guid organizationId, AuditReportFilter filter, CancellationToken cancellationToken);

    byte[] BuildCsv(AuditReportDto report);

    byte[] BuildPdf(AuditReportDto report);
}
