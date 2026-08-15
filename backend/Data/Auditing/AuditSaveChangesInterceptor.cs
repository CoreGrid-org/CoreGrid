using System.Text.Json;
using CoreGrid.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreGrid.Api.Data.Auditing;

// FR-063: writes one immutable AuditLogEntry per changed entity for every
// SaveChanges call, generically — no per-feature wiring needed. New entities
// (Maintenance, Transfer/Disposal writes, etc.) are covered automatically
// the moment they're added to CoreGridDbContext.
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public AuditSaveChangesInterceptor(ICurrentUserAccessor currentUserAccessor)
    {
        _currentUserAccessor = currentUserAccessor;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLogEntry &&
                        (e.State == EntityState.Added ||
                         e.State == EntityState.Modified ||
                         e.State == EntityState.Deleted))
            .ToList();

        if (entries.Count == 0)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var (actorUserId, actorOrganizationId) = await _currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var correlationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            var operation = entry.State switch
            {
                EntityState.Added => "Create",
                EntityState.Deleted => "Delete",
                _ => "Update"
            };

            var changes = new List<object>();
            foreach (var property in entry.Properties)
            {
                object? before = entry.State == EntityState.Added ? null : property.OriginalValue;
                object? after = entry.State == EntityState.Deleted ? null : property.CurrentValue;

                if (entry.State == EntityState.Modified && Equals(before, after))
                {
                    continue;
                }

                changes.Add(new { field = property.Metadata.Name, before, after });
            }

            var organizationId =
                entry.Properties.FirstOrDefault(p => p.Metadata.Name == "OrganizationId")?.CurrentValue as Guid?
                ?? actorOrganizationId;

            if (organizationId is null)
            {
                // No organisation to attribute this to at all (e.g. Setup's
                // unauthenticated Organization-creation step) — skip rather
                // than write a row with no meaningful home.
                continue;
            }

            var entityId = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id")?.CurrentValue as Guid?;

            context.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId.Value,
                ActorUserId = actorUserId,
                EntityType = entry.Entity.GetType().Name,
                EntityId = entityId,
                Operation = operation,
                Changes = changes.Count > 0 ? JsonSerializer.Serialize(changes) : null,
                CorrelationId = correlationId,
                CreatedAt = now
            });
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
