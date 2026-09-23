using CoreGrid.Api.Features.Shared;

namespace CoreGrid.Api.Features.Audit;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogEntryDto>> GetEntriesAsync(Guid organizationId, AuditLogQueryParameters parameters, CancellationToken cancellationToken);
}
