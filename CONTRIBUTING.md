# Contributing to CoreGrid

Thank you for your interest in contributing to CoreGrid. This guide covers the development environment, the project structure and the contribution workflow.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) with npm (CI uses Node 22)
- [Docker](https://docs.docker.com/get-docker/) with Docker Compose v2
- `curl`, `jq` and `make`; `psql` is optional (performance tests, `make db-shell`)

Swagger is generated from the controllers at runtime and EF Core is used directly, so there is no code-generation step.

---

## Quick Setup

```bash
git clone https://github.com/CoreGrid-org/CoreGrid.git
cd CoreGrid
./setup.sh          # or: make setup
```

`setup.sh` is safe to re-run. It:

1. checks the prerequisites;
2. installs `dotnet-ef` and restores the NuGet and npm packages;
3. creates `backend/.env` and `frontend/.env` from their examples (an existing file is never overwritten);
4. starts PostgreSQL and ThunderID in Docker (on the first run it creates and initialises ThunderID, which takes a few minutes);
5. applies the EF Core migrations;
6. checks the ThunderID configuration: it prints the console's admin password, tests the backend client credentials, and asks for any value it cannot discover;
7. creates one **test account per role**, all with password `Login@123456`. These are for local development only.

| Role | Email | Client |
|---|---|---|
| Administrator | `admin@coregrid.test` | Web |
| Inventory Officer | `officer@coregrid.test` | Web and mobile |
| Auditor | `auditor@coregrid.test` | Web |
| Department Staff | `staff@coregrid.test` | Mobile |

**On a fresh ThunderID you must still do the one-time console setup** in [`docs/setup/thunderid.md`](./docs/setup/thunderid.md): the user type, the roles, the frontend, backend and mobile applications, and CORS. `setup.sh` stops at step 6 and tells you which values to enter. Re-run it once the console setup is done, and it will create the accounts.

Then start the API and the web app:

```bash
make dev            # API http://localhost:5083, web http://localhost:5173
```

Options: `./setup.sh --yes` never prompts; `./setup.sh --skip-users` skips the test accounts.

---

## Manual Setup

These are the same steps `setup.sh` performs, for when you want to do them yourself.

### 1. Infrastructure (ThunderID + PostgreSQL)

```bash
make infra-bootstrap   # first time only: docker compose up -d
```

`docker-compose.yml` includes ThunderID's quick-start bundle and adds CoreGrid's own PostgreSQL. The result is:
- **ThunderID** on `https://localhost:8090`, container `coregrid-thunderid` (`coregrid-thunderid-1` on older checkouts);
- **`coregrid-postgres`** on host port **5433**, not 5432, to avoid clashing with another local Postgres.

The first run pulls a large image, so allow several minutes.

**Afterwards, start and stop with `make infra-up` and `make infra-stop`.** Don't run `docker compose up -d` again against an initialised ThunderID: it re-runs ThunderID's one-shot setup container, which then fails with a user-type conflict. `make infra-up` starts only the server and the database.

### 2. ThunderID

Follow [`docs/setup/thunderid.md`](./docs/setup/thunderid.md) once per ThunderID instance. [`infra/thunderid/coregrid.yaml`](./infra/thunderid/coregrid.yaml) is an export of a working configuration, which you can use to check your console settings.

### 3. Backend configuration

`appsettings.json` and `appsettings.Development.json` contain only logging. Every environment-specific value lives in **`backend/.env`**: the connection string, CORS origin, ThunderID IDs and client credentials, model key and R2 storage. The file is git-ignored and `Program.cs` loads it on start-up. Real environment variables override it, and blank values are ignored.

```bash
cp backend/.env.example backend/.env       # setup.sh does this for you
# then set ThunderID__ScimClientSecret=<CoreGrid Backend client secret>
```

[`backend/.env.example`](./backend/.env.example) already holds the local defaults: Docker Postgres, the CORS origin, and the ThunderID IDs of the reference configuration in `infra/thunderid/`. Change the IDs if your ThunderID instance differs. The file also shows the Supabase connection-string options. In Docker or the cloud, set the same names as real environment variables.

### 4. Database and API

```bash
make db-update      # dotnet ef database update against localhost:5433
make backend        # http://localhost:5083, Swagger at /swagger, health at /health
```

To migrate another database, such as Supabase, pass its connection string: `make db-update DB_CONNECTION='Host=…;Port=5432;…;SSL Mode=Require'`. Schema changes are made only through EF Core migrations. [`backend/db/README.md`](./backend/db/README.md) covers the generated SQL exports.

### 5. Frontend

```bash
make frontend-install
cp frontend/.env.example frontend/.env     # if setup.sh hasn't already
make frontend       # http://localhost:5173
```

In `frontend/.env`, set `VITE_THUNDERID_CLIENT_ID` to the CoreGrid Frontend **Client ID** (not its Application ID). `VITE_API_URL` and the redirect URLs are already right for local development. `ThunderIDProvider` in `frontend/src/main.tsx` reads these values.

### 6. First sign-in

| Service | URL |
|---|---|
| Web app | http://localhost:5173 |
| API / Swagger | http://localhost:5083/swagger |
| ThunderID console | https://localhost:8090/console |
| PostgreSQL | localhost:5433 (`make db-shell`) |

1. On a database with no organisation yet, the web app sends you to `/setup`, because `GET /api/setup/status` reports `needs_setup`.
2. Setup creates the organisation and the first Administrator, both in ThunderID and in CoreGrid's own `Users` table. If you ran `setup.sh`, this is already done for `admin@coregrid.test`.
3. Sign in. Each role lands on its own area: Administrator on `/admin`, Inventory Officer on `/inventory`, Auditor on `/audit`. Staff use the mobile app.
4. Add more users from **Users & Roles**. A user who exists only in ThunderID, with a CoreGrid role, gets a CoreGrid record automatically on first sign-in.

---

## Project Structure

### Backend (`backend/`)

Modular monolith: one `Features/<Name>/` folder per SRS component/owner (components and their maintainers are listed in [SRS §12](./docs/srs/12-component-ownership.md)), not per technically-related entity group — that's why `Departments`/`Locations`/`OrganizationPolicies` live in `Features/OrgConfig/` (Component D) rather than `Features/Assets/` (Component A), even though Assets needs Department/Location as reference data. Every feature registers itself with one `AddXxxFeature()` extension method (its `Module.cs`), so `Program.cs` reads as a manifest — platform setup (JSON, Swagger, auth, rate limiting, health, CORS, DB), one `Add…Feature()` call per module, then the middleware pipeline — instead of a 30-line block of fully-qualified `AddScoped<>()` calls.

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
5. Link the GitHub issue your change resolves (`Closes #123`) so its status updates when the PR merges.
6. Reference the requirement ID (e.g. `FR-013`) your change implements in the commit message or PR description, per [SRS §18.2](./docs/srs/18-development-workflow-and-change-control.md#182-requirement-traceability) — that's what lets a requirement be traced to code, tests and a reviewer.

### Branch and PR conventions

- Create a feature branch from `development`, named for your component: `feature/<component>-<short-description>` (e.g. `feature/assets-qr-generation`).
- All PRs target the `development` branch.
- Use the PR template ([`.github/pull_request_template.md`](./.github/pull_request_template.md)) — it's applied automatically when you open a PR on GitHub.
- Every PR needs at least one review from a maintainer other than its author before merge ([SRS §18.1](./docs/srs/18-development-workflow-and-change-control.md#181-branches-and-pull-requests)). No self-merging.

---

## Need Help?

- Open an [issue](https://github.com/CoreGrid-org/CoreGrid/issues)
- See the full [SRS](./docs/srs/00-front-matter.md) for the system's requirements and architecture
- See the [issue tracker](https://github.com/CoreGrid-org/CoreGrid/issues) for what's planned and in progress
