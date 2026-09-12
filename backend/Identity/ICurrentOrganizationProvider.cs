namespace CoreGrid.Api.Identity;

// Synchronous source of the caller's OrganizationId for CoreGridDbContext's
// global query filter (FR-006) — a query filter's predicate is evaluated as
// part of translating a LINQ query to SQL, so it cannot itself await a DB
// lookup. Reads the `organization_id` claim RoleEnrichmentMiddleware already
// rehydrates from the Users table once per request, rather than querying
// again. Returns null for requests with no such claim (unauthenticated,
// /api/setup, or the /api/agent-tools/* service-principal path that
// RoleEnrichmentMiddleware deliberately skips) — CoreGridDbContext treats
// null as "no org context yet" and does not filter, preserving those
// surfaces' existing, separately-reviewed authorization logic.
public interface ICurrentOrganizationProvider
{
    Guid? OrganizationId { get; }
}
