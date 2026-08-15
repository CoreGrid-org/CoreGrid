# CoreGrid — notes for AI-assisted sessions

CoreGrid is an asset-lifecycle management system: .NET 10 / EF Core / PostgreSQL backend, React/TypeScript (Vite, Carbon Design System) frontend, ThunderID for auth. Full requirements live in [`doc/SRS/`](./doc/SRS/00-front-matter.md); what's actually built (vs. still planned) is tracked in [`doc/PROGRESS.md`](./doc/PROGRESS.md) — treat that file as more current than your own assumptions about the codebase.

**Backend file structure — see [`CONTRIBUTING.md` § Project Structure](./CONTRIBUTING.md#project-structure).** That's the single source of truth for where things go; don't duplicate it here or let this file drift out of sync with it. The short version: one `Features/<Name>/` folder per SRS component/owner (not per technically-related entity group), flat for a small feature or `Controllers/`/`Services/`/`DTOs/` subfolders once it outgrows that; `Domain/` entities are grouped into matching subfolders but all share one flat `CoreGrid.Api.Domain` namespace, so moving a domain file between subfolders never requires a `using` change anywhere else.

## Before finishing backend work

- `dotnet build` from `backend/` — zero warnings, zero errors.
- If you touched `Domain/` or `Data/CoreGridDbContext.cs`: run `dotnet ef migrations add <Name>`, then sanity-check the generated `Up()`/`Down()` are the change you intended (not a much bigger diff — that usually means `CoreGridDbContextModelSnapshot.cs` was already out of sync with migration history before you started; verify with an empty throwaway migration first if unsure).
- Regenerate `backend/db/schema.sql` and add a numbered `backend/db/migrations/NNNN_*.sql` export — see [`backend/db/README.md`](./backend/db/README.md). These are generated, never hand-edited.
- `dotnet ef database update` against the local dev Postgres (docker-compose, not shared) if you want the running app to actually reflect the new schema — a pending migration that's never applied causes exactly the DB-column-mismatch 500s that FR-endpoints touching that table start throwing.

## Conventions worth knowing

- Every controller resolves the caller's CoreGrid user via `CoreGridControllerBase.GetCurrentUserAsync()` (`Features/Shared/`) — the ThunderID token's `sub` claim looked up against `Users.ExternalSubjectId`. Don't re-implement this per controller.
- `AuditSaveChangesInterceptor` (`Data/Auditing/`) logs every entity mutation generically — new entities are covered automatically the moment they're added to `CoreGridDbContext`. Nothing to wire up per-feature for FR-063.
- Multi-tenancy is enforced manually per-query (`.Where(x => x.OrganizationId == currentUser.OrganizationId)`), not via an EF global query filter (`FR-006` is tracked `❌` in `doc/PROGRESS.md` for exactly this reason) — every new query needs that filter explicitly.
