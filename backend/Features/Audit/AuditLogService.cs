using System.Linq.Expressions;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared;
using CoreGrid.Api.Features.Shared.Paging;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Audit;

public class AuditLogService : IAuditLogService
{
    private static readonly Expression<Func<AuditLogEntry, AuditLogEntryDto>> ToDtoExpression = a => new AuditLogEntryDto
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
    };

    private static readonly IReadOnlyDictionary<string, Expression<Func<AuditLogEntry, object?>>> SortMap =
        new Dictionary<string, Expression<Func<AuditLogEntry, object?>>>
        {
            ["createdat"] = a => a.CreatedAt,
            ["entitytype"] = a => a.EntityType,
            ["operation"] = a => a.Operation,
        };

    private readonly CoreGridDbContext _db;

    public AuditLogService(CoreGridDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditLogEntryDto>> GetEntriesAsync(
        Guid organizationId, AuditLogQueryParameters parameters, CancellationToken cancellationToken)
    {
        var query = _db.AuditLogEntries
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId);

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

        var sorted = query.ApplySort(parameters, SortMap, defaultSortKey: "createdat");
        return await sorted.ToPagedResultAsync(parameters, ToDtoExpression, cancellationToken);
    }
}
