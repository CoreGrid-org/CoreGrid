using CoreGrid.Api.Data;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreGrid.Api.Features.Audit;

// FR-064: read-only, deliberately — no POST/PUT/PATCH/DELETE route exists
// anywhere for AuditLogEntry, and the database itself revokes UPDATE/DELETE
// from the app role (AddAuditLog migration), so immutability holds even if
// a future change accidentally exposes db.AuditLogEntries for editing.
[ApiController]
[Route("api/audit-log")]
[Authorize(Policy = Policies.CanReadAuditLog)]
public class AuditLogController : CoreGridControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService, CoreGridDbContext db) : base(db)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogEntryDto>>> GetEntries(
        [FromQuery] AuditLogQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null) return Unauthorized();

        var result = await _auditLogService.GetEntriesAsync(currentUser.OrganizationId, parameters, cancellationToken);
        return Ok(result);
    }
}
