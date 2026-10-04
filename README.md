<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="frontend/public/assets/w-coregrid.webp">
    <img src="frontend/public/CoreGrid.png" alt="CoreGrid" width="140">
  </picture>
</p>

<h1 align="center">CoreGrid</h1>
<p align="center">An open-source, self-hosted, agentic-AI-assisted asset lifecycle management platform for government &amp; institutional assets.</p>

CoreGrid registers, identifies, maintains, transfers, verifies and disposes of an organisation's physical assets on one role-controlled platform. A four-agent, human-approved AI workflow supports repair, transfer and disposal decisions. Each deployment serves **one organisation** (single-tenant): the organisation installs and runs its own copy. Full detail is in the [Software Requirements Specification](docs/srs/00-front-matter.md); planned and in-progress work is tracked in the [issue tracker](https://github.com/CoreGrid-org/CoreGrid/issues).

## Repositories

| Repository | Contents |
|---|---|
| `CoreGrid-org/CoreGrid` (this one) | ASP.NET Core API, PostgreSQL schema and migrations, React management application, CI |
| `CoreGrid-org/coregrid-mobile` | Flutter field-operations app (Android) |
| `CoreGrid-org/coregrid-web` | Public static site (Docusaurus, published on GitHub Pages): features, user manual and changelog |

## Repository layout

| Path | Contents |
|---|---|
| `backend/` | ASP.NET Core 10 Web API: `Features/<Component>/` (controllers, services, DTOs), `Domain/`, `Data/` (EF Core `DbContext`, audit interceptor), `Migrations/`, `db/` (generated SQL schema export) |
| `backend.Tests/` | xUnit tests: service, validation, authorisation-matrix, query-filter, and real-PostgreSQL append-only tests |
| `frontend/` | React 19 + Vite management application (Carbon Design System) — see [`frontend/README.md`](frontend/README.md) |
| `scripts/` | ThunderID configuration, performance tests (`perf/`), database export helpers (`db/`) — see [`scripts/README.md`](scripts/README.md) |
| `docs/srs/` | Baselined Software Requirements Specification |
| `docs/architecture/` | Architecture Decision Records |
| `docs/setup/` | ThunderID, Cloudflare R2 and AI-agent setup guides |
| `docs/coursework/` | University coursework material (SE3090); not part of the product documentation |
| `docker-compose.yml` | Local ThunderID + PostgreSQL |
| `Makefile` | Developer commands (`make help`) |

## Technology

| Layer | Stack |
|---|---|
| API | C# / ASP.NET Core 10, EF Core 10 + Npgsql, Swagger/OpenAPI |
| Database | PostgreSQL 16 |
| Identity | ThunderID (OIDC / OAuth 2.0 with PKCE, SCIM provisioning) |
| Web | React 19, TypeScript, Vite, React Router 7, IBM Carbon |
| Mobile | Flutter, Riverpod, go_router, Dio, flutter_appauth |
| Agentic AI | In-process C# orchestrator with four agents (Planner, Maintenance Analysis, Budget Analysis, Policy Compliance); any OpenAI-compatible chat-completions endpoint |
| Object storage | Any S3-compatible store (Cloudflare R2 by default) |

## Quick start (local)

Prerequisites: .NET 10 SDK, Node 22, Docker, `make`.

```bash
make setup            # dotnet-ef, NuGet + npm restore, frontend/.env
make infra-bootstrap  # first run only: ThunderID + PostgreSQL (later: make infra-up)
make db-update        # apply EF Core migrations
make dev              # API on http://localhost:5083, web on http://localhost:5173
```

Open the web app and complete first-run **Setup**, which creates the organisation and its first Administrator. Swagger is at `http://localhost:5083/swagger`, and health is at `http://localhost:5083/health`.

ThunderID configuration (applications, roles, SCIM client) is described in [`docs/setup/thunderid.md`](docs/setup/thunderid.md), object storage in [`docs/setup/cloudflare-r2.md`](docs/setup/cloudflare-r2.md), and the model key in [`docs/setup/ai-agents.md`](docs/setup/ai-agents.md).

## Configuration

The API is configured through `appsettings.json`, `dotnet user-secrets` (development) or environment variables. A double underscore marks a nested key. Values are never committed.

| Variable | Purpose |
|---|---|
| `ConnectionStrings__CoreGrid` | PostgreSQL connection string |
| `Cors__AllowedOrigins__0` | Web app origin |
| `ThunderID__Issuer`, `ThunderID__Resource`, `ThunderID__OuId` | Token validation and directory |
| `ThunderID__ScimClientId`, `ThunderID__ScimClientSecret` | User provisioning |
| `ThunderID__RoleIds__Administrator` (… per role) | Role mapping |
| `Llm__ApiKey` (optional `Llm__Endpoint`, `Llm__Model`) | Model access for the Planner and Budget agents; without a key, both use a deterministic fallback |
| `CloudflareR2__AccountId`, `__AccessKeyId`, `__SecretAccessKey`, `__BucketName` | Photo evidence storage |

The web app reads `VITE_API_URL` and `VITE_THUNDERID_*` at build time (see `frontend/.env.example`).

## Deployment

The API and the web app each ship with a production `Dockerfile` (`make docker-build`). Any container host and any managed PostgreSQL will work. Deployment order:

1. PostgreSQL
2. `dotnet ef database update`
3. API
4. Web app build (with `VITE_*` pointing at the API)
5. Register the deployed origins and redirect URIs in ThunderID
6. First-run Setup

See [SRS §14](docs/srs/14-deployment-and-operations.md).

## Tests

```bash
make check            # what CI runs: zero-warning build + all tests
make test-backend     # xUnit (append-only tests need PostgreSQL on :5433)
make test-frontend    # Vitest + React Testing Library
make perf             # load and latency tests against a running API (needs CG_TOKEN; see scripts/perf/README.md)
```

CI (`.github/workflows/ci.yml`) runs on every push and pull request to `main` and `development`. The backend job builds with warnings as errors, migrates a PostgreSQL 16 service container and runs the tests. The frontend job runs the tests and a type-checked build.

## Components

### Component A — Asset Registry and QR Identification

Component A provides the asset register used by the rest of CoreGrid. Inventory Officers can configure asset categories and types, define ordered custom attributes for each type, and register assets with an organisation-scoped unique code, department, location, acquisition details, and validated attribute values. Asset records support search and filtering, condition updates, depreciation information, and lifecycle history.

#### Scan, lookup, and verification

Each asset’s QR payload identifies its asset code. The Flutter [mobile application](https://github.com/CoreGrid-org/coregrid-mobile) scans the label and retrieves the authoritative record through `GET /api/assets/qr/{code}`. Manual code entry provides a fallback lookup. The API scopes lookup to the authenticated organisation.

For physical verification, field users confirm the asset’s presence, actual location, and condition in the mobile workflow. CoreGrid compares those observations with the register and records a discrepancy when they differ. Verification can be completed from a campaign task or through the ad-hoc verification flow. The React application supports asset registration, configuration, search, and record management; mobile screens support field lookup and verification.

#### Design decisions

##### Custom attribute storage — ADR-006

Each asset type has configurable attribute definitions in `AssetAttributeDefinitions`. An asset’s values are stored separately in `AssetAttributeValues`, linked to both the asset and its definition. Values use typed fields, and the application validates each value against its definition before saving it.

This relational design was chosen because asset attributes must remain connected to valid definitions and support type-aware validation and search. It supports FR-019 validation and FR-028 filtering without adding new database columns for every asset type. Foreign keys preserve those relationships, while indexes support common searches. Displaying an asset’s attributes requires joining its value rows to their definitions; variable agent workflow records use JSONB, where flexible record shapes are more appropriate. See [ADR-006](docs/architecture/decision-records.md#adr-006--relational-attribute-value-storage-and-jsonb-workflow-state) for the options and rationale.

##### Asset identity and lifecycle

Asset codes are unique within an organisation. Asset amendments and lifecycle events are recorded in asset history; assets with lifecycle history are retained and leave the active register through the disposal workflow.

#### Component A references

- [Component ownership (SRS §12)](docs/srs/12-component-ownership.md)
- [Functional requirements FR-021–FR-032](docs/srs/06-functional-requirements.md#64-component-a--asset-registry-and-qr-identification-fr-021--fr-032)
- [Custom attribute storage rationale](docs/srs/08-data-requirements.md#84-custom-attribute-storage-strategy)
- [Physical schema for Component A](docs/srs/appendix-e-physical-database-schema.md#e7-component-a--asset-registry--qr-identification)

### Component B — Maintenance Management

Faults are reported from the web or mobile app, optionally with a photo, which is stored as a private object and served only through short-lived signed URLs. Officers can also create maintenance records directly. Records follow `REQUESTED → APPROVED → IN_PROGRESS → COMPLETED/CANCELLED`. While a record is active the asset is locked `UNDER_MAINTENANCE`, which blocks transfer and disposal. Completion (`POST /api/maintenance/{id}/complete`) is one transaction: it checks actual cost against the organisation's variance tolerance, updates the asset's cumulative cost, repair count and condition, condemns the asset if the resulting condition is *Unserviceable*, and writes history. A background service schedules preventive maintenance once an asset type's interval has elapsed. Lifecycle events raise in-app notifications. The **Maintenance Analysis Agent** computes repair count, MTBF, cost trend and a 12-month projection, without a model call.

### Component C — Transfer & Disposal

Transfers move an asset between departments: `REQUESTED → APPROVED → COMPLETED` (or `REJECTED`/`CANCELLED`). While this happens the asset moves `TRANSFER_REQUESTED → IN_TRANSIT → ACTIVE`. Receipt is confirmed by scanning the arriving asset's QR on mobile, and confirmation updates the department and location atomically. Disposal starts with condemnation (`CONDEMNED`), then a disposal request (`PENDING`, asset `DISPOSAL_REQUESTED`) that an Administrator approves (`APPROVED`, asset `DISPOSED`) only when preconditions P1–P6 pass and the approver is not the requester. P6 requires a linked agent workflow that reached approval with a PASS validation result. Requests can be sent back for revision or rejected. Concurrent edits are detected through PostgreSQL `xmin` and return `409`. The **Budget Analysis Agent** weighs residual value against repair and replacement cost and the department's budget, and ranks the options.

### Component D — Audit & Compliance, Organisation Configuration, User Administration

First-run Setup creates the organisation and its first Administrator. Administrators manage departments, locations, users (invited, re-roled, deactivated and password-reset through ThunderID SCIM) and per-asset-type policy thresholds. Auditors run verification campaigns, which generate officer tasks. Mismatches found during scan-to-verify raise discrepancies automatically, and auditors resolve them (`PATCH /api/discrepancies/{id}/resolve`). Every entity change is written to an append-only audit log by `AuditSaveChangesInterceptor`. Dashboards and Reports (inventory, maintenance, disposal, audit) export to PDF or CSV. The **Policy Compliance Agent** evaluates the combined evidence against organisation policy using a deterministic rule engine, and pauses high-impact recommendations for Administrator approval (`PATCH /api/agent-workflows/{id}/decide`).

### Platform foundations

These are shared by every component:
- ThunderID sign-in, with each request's caller resolved once into `ICurrentUser`.
- Named authorisation policies with a fail-closed fallback.
- An EF Core organisation query filter.
- Department scoping for Staff.
- Correlation ids, a uniform error envelope, `/health`, rate limiting, and CI.

See [`CONTRIBUTING.md`](CONTRIBUTING.md#project-structure) for where code goes.

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md).

## License

Apache License 2.0 - see [LICENSE](LICENSE).
