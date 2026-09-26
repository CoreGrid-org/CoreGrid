# Contributing to CoreGrid

Thank you for your interest in contributing to CoreGrid. This guide walks you through setting up the full development environment from scratch.

---

## Prerequisites

Install the following before starting:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/) with npm
- [Docker](https://docs.docker.com/get-docker/) and Docker Compose

Install the EF Core CLI tool (used for migrations):

```bash
dotnet tool install --global dotnet-ef
```

Make sure `~/.dotnet/tools` is on your `PATH` — the installer tells you if it isn't.

There's no separate codegen step for API docs or database queries: Swagger is generated automatically from the ASP.NET Core controllers at runtime, and there's no SQL query-builder codegen — CoreGrid uses EF Core directly.

---

## 1. Clone the Repository

```bash
git clone https://github.com/CoreGrid-org/CoreGrid.git
cd CoreGrid
```

---

## 2. Start Infrastructure (ThunderID + PostgreSQL)

**On a machine that's never run this project's containers before**, bootstrap ThunderID on its own first:

```bash
docker compose -f oci://ghcr.io/thunder-id/thunderid-quick-start:latest -p coregrid up -d
```

Then, from the repo root, bring up CoreGrid's own database alongside it:

```bash
docker compose up -d
```

Together these give you two running containers: `coregrid-thunderid-1` (identity, `https://localhost:8090`) and `coregrid-postgres` (CoreGrid's own application database, host port `5433` — not the Postgres default `5432`, to avoid colliding with another local project's Postgres). See [`docs/setup/thunderid.md`](./docs/setup/thunderid.md#start-thunderid-and-postgresql) for why it's two commands the first time, not one.

**The first `docker compose -f oci://...` run pulls the ThunderID image, which is large and can take several minutes** — this is normal, not a hang. After that first bootstrap, everything is local: to stop without losing data, `docker compose stop`; to restart, `docker compose start` (don't re-run either `up -d` command against an existing volume unless you mean to — see the troubleshooting table in [`docs/setup/thunderid.md`](./docs/setup/thunderid.md)).

---

## 3. Set Up ThunderID

Follow [`docs/setup/thunderid.md`](./docs/setup/thunderid.md) for the full one-time console setup — creating the `CoreGridUser` type and the four CoreGrid roles, registering the frontend application, allowing the frontend's CORS origin, and creating the backend's service credential. Come back here once that's done.

---

## 4. Backend Setup

```bash
cd backend
dotnet restore
```

### Apply Database Migrations

```bash
dotnet ef database update
```

Schema is managed exclusively by these EF Core migrations (SRS §2.3, C-02) — see [`backend/db/README.md`](./backend/db/README.md) if you also want the plain numbered `.sql` files that get generated from them for review.

### Configure ThunderID Credentials

`backend/appsettings.Development.json` already has the local Postgres connection string, the frontend's CORS origin, and every non-secret ThunderID value filled in (`Issuer`, `Resource`, `OuId`, `UserType`, `RoleIds`, `ScimClientId`) — nothing to fill in there yourself unless you set up a fresh ThunderID instance. The one secret, `ScimClientSecret`, is never committed — set it with `dotnet user-secrets`, matching SRS §14.2's naming:

```bash
cd backend
dotnet user-secrets set "ThunderID:ScimClientSecret" "<Backend Service Client Secret from docs/setup/thunderid.md>"
```

See [`docs/setup/thunderid.md`](./docs/setup/thunderid.md) for what each of these values is and where it comes from in the console — including the `username` attribute `CoreGridUser` needs for sign-in to resolve at all, a common first-time gotcha.

### Start the Backend

```bash
dotnet run
```

The backend runs on `http://localhost:5083`. Swagger UI is available at `http://localhost:5083/swagger`.

---

## 5. Frontend Setup

```bash
cd frontend
npm install
cp .env.example .env
```

Fill in `.env` with the values from your ThunderID frontend application (see [`docs/setup/thunderid.md`](./docs/setup/thunderid.md)) — `VITE_API_URL` is already correct as-is for a default local backend:

```env
VITE_API_URL=http://localhost:5083/api

VITE_THUNDERID_CLIENT_ID=<frontend Client ID>
VITE_THUNDERID_BASE_URL=https://localhost:8090
VITE_THUNDERID_AFTER_SIGN_IN_URL=http://localhost:5173
VITE_THUNDERID_AFTER_SIGN_OUT_URL=http://localhost:5173
```

These are read by `ThunderIDProvider` in `frontend/src/main.tsx`. `.env` is gitignored, same as `.env.local` would be — either name works with Vite, but `.env` is what the rest of the team actually uses, so stick with it for consistency.

Start the frontend:

```bash
npm run dev
```

The frontend runs on `http://localhost:5173`.

---

## 6. Verify Everything is Running

| Service | URL |
|---|---|
| Frontend | http://localhost:5173 |
| Backend | http://localhost:5083 |
| Swagger UI | http://localhost:5083/swagger |
| ThunderID Console | https://localhost:8090/console |
| PostgreSQL | localhost:5433 |

---

## 7. Try It Out

1. Go to `http://localhost:5173`.
2. On a fresh database, you'll be redirected through sign-in straight to `/setup` — `GET /api/setup/status` genuinely checks whether any organisation exists yet.
3. Fill in the admin account and organisation details and submit. This provisions a real ThunderID account (`Identity/ThunderIdIdentityDirectory.cs`) and creates the matching `Organizations`/`Users` rows locally.
4. Sign in with that account. `/` resolves your role from the `roles` claim and sends you to the matching dashboard — an Administrator lands on `/admin`. Other roles aren't provisionable through the UI yet (see [`docs/progress.md`](./docs/progress.md)), so `/admin` is the only one worth exercising today.
5. From `/admin` → **Users & Roles**, an Administrator can invite further users by email and role (FR-013) — everything else on the Admin Dashboard is a mock/placeholder page; see [Project Structure](#project-structure) below for which parts are real.

---

## Project Structure

### Backend (`backend/`)

Modular monolith: one `Features/<Name>/` folder per SRS component/owner (a component and its owner in [SRS §18](./docs/srs/18-team-roster-and-work-allocation.md)), not per technically-related entity group — that's why `Departments`/`Locations`/`OrganizationPolicies` live in `Features/OrgConfig/` (Component D) rather than `Features/Assets/` (Component A), even though Assets needs Department/Location as reference data. Every feature registers itself with one `AddXxxFeature()` extension method (its `Module.cs`), so `Program.cs` reads as a manifest — platform setup (JSON, Swagger, auth, rate limiting, health, CORS, DB), one `Add…Feature()` call per module, then the middleware pipeline — instead of a 30-line block of fully-qualified `AddScoped<>()` calls.

```
backend/
  Program.cs                  Composition root: platform + one Add…Feature() per module + pipeline
  Domain/                     Entities, enums and cross-cutting constants — no behaviour. All files
                               share one flat `CoreGrid.Api.Domain` namespace regardless of
                               subfolder (so cross-entity navigation properties, and constants
                               referenced from any feature, never need an extra `using`) — the
                               subfolders below are physical organisation only:
    Identity/                 Organization, User, CoreGridRole
    OrgConfig/                Department, Location, OrganizationPolicy
    Assets/                   Asset, AssetType, AssetCategory, AssetAttributeDefinition,
                               AssetAttributeValue, AssetHistory, AssetHistoryEventTypes,
                               AssetStatuses, AssetConditions
    Transfers/                AssetTransfer, DisposalRequest
    Verification/              VerificationCampaign, VerificationTask, Discrepancy
    Audit/                     AuditLogEntry
    Agents/                    AgentWorkflow, AgentExecutionStep, AgentApproval, AgentNames,
                               WorkflowDecisions
    Maintenance/                MaintenanceRecord
    Notifications/               Notification, NotificationTypes
  Data/
    CoreGridDbContext.cs        EF Core model configuration (one `modelBuilder.Entity<T>()` block
                               per entity, including that entity's org-scoped query filter —
                               keep it there, not scattered into partial classes)
    Auditing/                  AuditSaveChangesInterceptor — the generic FR-063 audit-log writer;
                               new entities are covered automatically, nothing to wire up
                               per-feature
  Features/
    Shared/                    Cross-cutting kernel — the only namespace other features import
                               from each other:
      Api/                      ApiExceptionFilter, ErrorEnvelope, InvalidModelStateResponseFactory
      Auth/                     Policies, RoleGroups, CoreGridPolicyRequirement/Handler,
                               ServicePrincipal, AuthorizationOutcomeLoggingMiddleware
      CurrentUser/               ICurrentUser + CurrentUserContext — resolved once per request by
                               `Features/Identity/RoleEnrichmentMiddleware`, read by
                               `CoreGridControllerBase`, the audit interceptor and the FR-006
                               query-filter provider instead of each running its own lookup
      Exceptions/                NotFoundException, ValidationException, BusinessRuleException,
                               ConflictException, ForbiddenException — mapped to their status
                               codes by `Api/ApiExceptionFilter`, one place
      Paging/                    PagedQuery, PagedResult<T>, QueryableExtensions.ToPagedResultAsync
      Scoping/                   DepartmentScope — Staff's own-department restriction on the
                               Assets/Maintenance/Transfers/Disposals list/detail endpoints
      Storage/, Reporting/, Finance/, Http/, Health/
                                 File upload validation, CSV/PDF export helpers, the one
                               straight-line depreciation calculator, correlation-id/security
                               headers middleware, health checks
      CoreGridControllerBase.cs  Base class every controller inherits; exposes `GetCurrentUserAsync()`
    Identity/                   RoleEnrichmentMiddleware, CurrentOrganizationProvider,
                               IIdentityDirectory/ThunderIdIdentityDirectory (the ThunderID
                               management-API client), MeController
    Setup/                      SetupController — the one unauthenticated write path
    Users/                      Administrator-only user administration
    OrgConfig/                  Component D: departments, locations, organisation policy
    Assets/                     Component A: assets, asset types, asset categories
    Maintenance/                 Maintenance records, preventive scheduling
    Transfers/, Disposals/       Component C — kept as two folders (not merged) so the existing
                               test suite's namespaces don't churn for no behavioural gain
    Verification/                Component D: verification campaigns, tasks, discrepancies
    Audit/                       Read-only audit log API (FR-064)
    Dashboard/                   Cross-cutting real-time indicators (FR-081)
    Notifications/                In-app Notification Centre (FR-080)
    Agents/, AgentTools/          The agentic workflow orchestrator/nodes and their read-only
                               tool endpoints (SRS §7)
  Migrations/                  EF Core migrations — the schema source of truth (SRS §2.3, C-02)
  db/                          Generated, readable SQL exports of the migrations — see db/README.md; never hand-edited
```

Every non-trivial feature above follows `Controllers/`, `Services/`, `DTOs/` subfolders once it outgrows a flat layout (`Setup/` and `Users/` stay flat — one controller, one model file each).

**Two feature shapes, pick based on size:**
- **Small** (one controller, one model file): flat in `Features/<Name>/`, e.g. `Features/Users/UsersController.cs` + `UsersModels.cs`. Use this until a feature outgrows it.
- **Larger** (several controllers and/or a real service layer): `Features/<Name>/Controllers/`, `Services/`, `DTOs/` subfolders, e.g. `Features/Assets/`, `Features/OrgConfig/`, `Features/Verification/`. Promote a flat feature to this shape once it needs more than one controller or its logic outgrows the controller itself.

**Adding a new business component:** create `Features/<Name>/` (flat or subfoldered per the rule above) with an `<Name>Module.cs` (`public static class XxxModule { public static IServiceCollection AddXxxFeature(this IServiceCollection services) }`) registering its services, and one `builder.Services.AddXxxFeature();` line in `Program.cs`. Add its entities under a matching `Domain/<Name>/` folder (remember: namespace stays `CoreGrid.Api.Domain`, no `using` changes needed elsewhere) and its own `modelBuilder.Entity<T>()` block — including an org-scoped `HasQueryFilter` (see FR-006 below) — in `CoreGridDbContext`, then run a migration (below). Give every route a named policy from `Features/Shared/Auth/Policies.cs` (add one there and to `RoleGroups.cs` if the permission is new — never a bare `[Authorize]` or an inline `Roles = "..."` string). If it needs a type another feature will also use (a DTO, a base class), put it in `Features/Shared/` rather than reaching into another feature's namespace — that cross-feature `using` is a sign the type belongs in `Shared/`, not that it's fine to import anyway.

### Frontend (`frontend/src/`)

```
frontend/src/
  app/App.tsx                The route table — the composition root
  features/
    auth/                    Sign-in, role routing/guards, the CoreGridRole type
      components/  hooks/  lib/  pages/  services/
    setup/                   First-run organisation + admin setup
    dashboard/                Post-login landing + per-role dashboards
    users/                   Real Users & Roles feature (list + invite)
  shared/                     Cross-feature reusable pieces
    components/  hooks/  lib/  pages/
  styles/index.scss           Carbon overrides + CoreGrid's own BEM-ish classes (cg-*)
  main.tsx                    Vite entry point — ThunderIDProvider + BrowserRouter
```

A new feature gets its own `features/<name>/` folder with the same internal shape (`components/`, `hooks/`, `pages/`, `services/`) as `features/users/` — copy that one as the template.

**Import convention:** the `@/` alias (configured in `tsconfig.app.json` and `vite.config.ts`) maps to `frontend/src/`. Use relative imports (`../hooks/useX`) for anything inside the same feature folder; use the `@/` alias (`@/shared/...`, `@/features/auth/...`) whenever you're crossing into another feature or into `shared/` — it keeps cross-boundary dependencies visible at a glance instead of buried in `../../../` chains.

---

## Code Comments

Default to **no comments** — a well-named function, variable and file already say what the code does. Add one only when it captures something the code can't say for itself:

- a non-obvious **why** (a constraint from ThunderID, an SRS requirement, a workaround for a specific bug)
- a hidden **invariant** a future change could silently break
- something genuinely surprising about the behaviour

Don't write a comment that just restates what the next line does, and don't reference the current ticket, PR, or "fix" in a comment — that belongs in the commit message and goes stale the moment the code moves on. If you're tempted to explain *what* a block does, that's usually a sign it should be a better-named function instead.

---

## Development Workflow

### Making changes to the database schema

1. Add or edit an entity under `backend/Domain/`.
2. Update `backend/Data/CoreGridDbContext.cs` if the change affects relationships, indexes, or constraints.
3. `dotnet ef migrations add <DescriptiveName>`
4. `dotnet ef database update`
5. Regenerate the readable SQL export — see [`backend/db/README.md`](./backend/db/README.md).

### Making changes to endpoints

1. Add or update a controller under `backend/Features/`.
2. That's it — Swagger picks up new routes automatically from the controller, no separate generation step.

### Before You Push

All of these should pass before you open or update a PR — none of them are optional:

1. `dotnet build` from `backend/` — zero warnings, zero errors.
2. `npx tsc -b --force` from `frontend/` — zero errors.
3. `npm run build` from `frontend/` — the production build has to actually succeed, not just type-check.
4. **Exercise the change in a real browser** against a running backend + ThunderID. A green build proves the code compiles, not that the feature works — click through the actual flow you changed.
5. If your change completes or advances an item in [`docs/progress.md`](./docs/progress.md), tick it in the same PR.
6. Reference the requirement ID (e.g. `FR-013`) your change implements in the commit message or PR description, per [SRS §12.1](./docs/srs/12-individual-contribution-and-work-allocation.md#121-contribution-evidence-requirements) — that's what lets a requirement be traced to code, tests and a reviewer.

### Branch and PR conventions

- Create a feature branch from `development`, named for your component: `feature/<component>-<short-description>` (e.g. `feature/assets-qr-generation`).
- All PRs target the `development` branch.
- Use the PR template ([`.github/pull_request_template.md`](./.github/pull_request_template.md)) — it's applied automatically when you open a PR on GitHub.
- Every PR needs at least one review from another member before merge (SRS §12.1) — no self-merging.

---

## Need Help?

- Open an [issue](https://github.com/CoreGrid-org/CoreGrid/issues)
- See the full [SRS](./docs/srs/00-front-matter.md) for the system's requirements and architecture
- See [`docs/progress.md`](./docs/progress.md) for what's actually built versus still planned
