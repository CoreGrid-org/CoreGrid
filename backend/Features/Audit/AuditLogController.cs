using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Audit;

// FR-064: read-only, deliberately — no POST/PUT/PATCH/DELETE route exists
// anywhere for AuditLogEntry, and the database itself revokes UPDATE/DELETE
// from the app role (AddAuditLog migration), so immutability holds even if
// a future change accidentally exposes db.AuditLogEntries for editing.
[ApiController]
[Route("api/audit-log")]
[Authorize(Roles = $"{nameof(CoreGridRole.Auditor)},{nameof(CoreGridRole.Administrator)}")]
public class AuditLogController : CoreGridControllerBase
{
    private readonly CoreGridDbContext _db;

    public AuditLogController(CoreGridDbContext db) : base(db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogEntryDto>>> GetEntries(
        [FromQuery] AuditLogQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);

        if (currentUser is null)
        {
            return Unauthorized();
        }

        var query = _db.AuditLogEntries
            .AsNoTracking()
            .Where(a => a.OrganizationId == currentUser.OrganizationId);

        if (!string.IsNullOrWhiteSpace(parameters.EntityType))
        {
            query = query.Where(a => a.EntityType == parameters.EntityType);
        }

        if (parameters.ActorUserId.HasValue)
        {
            query = query.Where(a => a.ActorUserId == parameters.ActorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Operation))
        {
            query = query.Where(a => a.Operation == parameters.Operation);
        }

        if (parameters.From.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= parameters.From.Value);
        }

        if (parameters.To.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= parameters.To.Value);
        }

        var page = parameters.Page < 1 ? 1 : parameters.Page;
        var pageSize = parameters.PageSize is < 1 or > 200 ? 50 : parameters.PageSize;

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogEntryDto
            {
                Id = a.Id,
                ActorUserId = a.ActorUserId,
                ActorEmail = a.ActorUser != null ? a.ActorUser.Email : null,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Operation = a.Operation,
                Changes = a.Changes,
                CorrelationId = a.CorrelationId,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<AuditLogEntryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }
}
