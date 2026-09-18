# Backend refactor plan

Status: **Phase 1 (§3) is complete and committed on `development`.** Phases 2–6 are still plan-only; each starts only when you say "start". §13 scores the codebase against the rubric below, before Phase 1 and after every phase — the "before" and "after Phase 1" rows are measured against the real repo; "after Phase 2" onward are targets that get corrected to actual once that phase runs.

Target: `backend/` (.NET 10 / EF Core / PostgreSQL) plus the minimum `backend.Tests/` and `frontend/` edits needed so the app keeps working end to end. Baseline commit: `9747b4a` on `development`.

---

## 0. Baseline facts found during the survey

These shape the plan. Everything below was verified against the code, not assumed.

| # | Finding | Where | Consequence |
|---|---|---|---|
| B1 | `backend.Tests` **does not compile**. The `<ProjectReference>` to `CoreGrid.Api.csproj` is missing (it has been added and removed four times in git history; last removed in `fb77167`). | `backend.Tests/backend.Tests.csproj` | `dotnet test` fails with ~40 `CS0103` errors. CI's backend job is red on `development`. No test can currently prove anything. |
| B2 | Two tests target `POST /api/maintenance/seed`, an endpoint that no longer exists. | `AuthorizationMatrixTests.cs` | Dead tests; would fail once the project compiles (404, not 401/403). |
| B3 | `AgentToolsAuthMiddleware` is never registered in `Program.cs`. `context.Items["IsAgentServicePrincipal"]` is never read. | `Features/AgentTools/AgentToolsAuthMiddleware.cs` | Dead code. Test comments claim it runs; it does not. The service-principal test only passes because `compute-depreciation` is `[AllowAnonymous]`. |
| B4 | `POST /api/agent-tools/compute-depreciation` is `[AllowAnonymous]`. | `AgentToolsController.cs` | Violates NFR-10 (every endpoint except `/health` and `/swagger` requires auth). |
| B5 | Agent-tool endpoints accept `?organizationId=` from the query string and fall back to "the single org". | `AgentToolsController.ResolveOrganizationIdAsync` | Violates SEC-ID-02 ("never infer organisation from request content"). |
| B6 | `MaintenanceService.CancelMaintenanceAsync` writes `AssetHistory.EventType = "MAINTENANCE_CANCELLED"`, which is **not** in `CK_AssetHistory_EventType`. | `MaintenanceService.cs:581` | Cancelling an `IN_PROGRESS` record throws a DB check-constraint violation → 500. Real bug. |
| B7 | `AgentServicePrincipal_ReachesAgentToolsWithoutAUsersRow` posts camelCase JSON (`acquisitionCost`) to an API whose naming policy is `snake_case_lower`. | `AuthorizationMatrixTests.cs` | Every field silently binds to `0` — a live demonstration of the under-posting problem this refactor fixes. Test must switch to snake_case. |
| B8 | EF model validation warns twice (`10622`): `AssetType`→`AssetAttributeDefinition` and `Asset`→`AssetAttributeValue` have a query filter on the required end only. | `Data/CoreGridDbContext.cs` | Child rows are not org-filtered; a query that starts from `AssetAttributeValues` bypasses FR-006. |
| B9 | `CLAUDE.md` says FR-006 (global org query filter) is `❌`; it is actually implemented on 17 entities and `PROGRESS.md` marks it `✅`. | `CLAUDE.md` | Stale guidance; will be corrected. |
| B10 | Migration snapshot is in sync (`dotnet ef migrations has-pending-model-changes` → "No changes"). Local Postgres container is up and healthy. | — | The refactor needs **no schema migration** (nothing in `Domain/` changes shape). |
| B11 | `POST /api/transfers/{id}/reject` and `POST /api/disposals/{id}/reject` (SRS §9.4) do not exist, although `TransferStatus.REJECTED`, `AssetTransfer.RejectionReason` and `DisposalStatus.REJECTED` are modelled. | Transfers/Disposals | Unreachable enum states; see §9 (optional). |
| B12 | `TransferService` writes no `AssetHistory` rows; verification completion writes none either (only the register-correction path does). | FR-027 | Lifecycle history is incomplete for two components. |
| B13 | `CondemnAssetRequest.EvidenceUrl` is accepted and then dropped. | `DisposalDtos.cs` | Silent data loss (SRS §9.4: "condemn with reason and evidence"). |
| B14 | Staff have no department scoping on asset / maintenance / transfer / disposal lists. Only the dashboard applies `DashboardScope`. | Appendix B `CanReadAssets` | Staff see the whole organisation; matrix says own department only. |
| B15 | Current-user lookup by `sub` claim happens **three times per request** (`RoleEnrichmentMiddleware`, `CoreGridControllerBase.GetCurrentUserAsync`, `CurrentUserAccessor` for audit, each its own query) and is re-implemented a fourth way in `MeController`. | Identity / Shared | Duplicate code and three DB round-trips per request. |
| B16 | 71 `catch` blocks and 89 `new { message = ... }` literals in controllers all do the same exception→status mapping, inconsistently (Assets: business rule → 400; Transfers/Disposals: → 422; Campaign delete: `catch (Exception)` → 400 with raw message). | all controllers | Leaks internal messages (NFR-14), wrong codes (§5.4), massive duplication. |
| B17 | `"NEW","GOOD","FAIR","POOR","UNSERVICEABLE"` is declared 6 times; asset status literals (`"ACTIVE"`, `"UNDER_MAINTENANCE"`, …) appear 13 times outside `AssetStatusConstants`; role-list constants are declared 16 times across controllers. | everywhere | Drift risk; no single source of truth. |
| B18 | Several "return one item" paths load the **entire org list** and pick from memory: `VerificationCampaignService.GetCampaignByIdAsync`, `VerificationTaskService.CompleteTaskAsync`, `DiscrepancyService.RaiseManualAsync/ResolveAsync`. | Verification | O(n) per call; NFR-07 violation. |
| B19 | Unpaginated list endpoints: asset-categories, asset-types, asset-types/{id}/attributes, departments, locations, organization-policies, transfers, assets/{id}/transfers, disposals, verification-campaigns, verification-tasks, discrepancies, agent-workflows, notifications (hard `Take(50)`). | 14 endpoints | NFR-07 / IF-01 / §5.4 require page/pageSize/sort/search + totalCount on every list. |
| B20 | Missing platform pieces required by the SRS: named authorisation policies (Appendix B), `/health` (NFR-20), HSTS (NFR-09), rate limiting (NFR-16, AI-27), transient-failure retry → 503 (NFR-24), correlation id (§5.4), structured 400 (NFR-11), auth-outcome logging (SEC-ID-09), upload content sniffing (NFR-13). | `Program.cs` | None exist today. |
| B21 | DI registration is one 30-line block in `Program.cs` with fully-qualified names; features have no registration entry point. | `Program.cs` | Not modular; every feature edit touches the composition root. |
| B22 | Frontend has no shared HTTP client: `handle()` + `authHeaders()` are copy-pasted in 12 API files with three different error-parsing behaviours. | `frontend/src/features/*/api` | Out of scope for the backend refactor; listed in §10 as a follow-up only. |

---

## 1. Principles for the whole refactor

1. **Wire compatibility first.** JSON property names, enum strings and route paths stay the same unless a row in §7 says otherwise, and every such change has its frontend edit listed in §10.
2. **One way to do each thing.** Current user, org scoping, paging, error mapping, constants, DTO mapping, upload validation, CSV/PDF helpers — each gets exactly one implementation in `Features/Shared/` and every feature uses it.
3. **Controllers are thin.** Resolve the caller, call the service, return. No `try/catch`, no role strings, no `new { message }`.
4. **Services own rules and throw typed exceptions.** `NotFoundException` (404), `ValidationException` (400), `BusinessRuleException` (422), `ConflictException` (409), `ForbiddenException` (403). One filter maps them.
5. **Every list is paged at the database.** `PagedQuery` in, `PagedResult<T>` out, clamped `pageSize`, stable default sort.
6. **Every endpoint declares a named policy.** No bare `[Authorize]`, no `Roles = "..."` strings, no `[AllowAnonymous]` except setup/health/swagger. A fallback policy makes "forgot the attribute" fail closed.
7. **No schema migration.** `Domain/` entity shapes are untouched; only behaviour, structure and query filters change. (If any step turns out to need one, it is called out before it is done.)
8. **Build: 0 warnings, 0 errors. Tests: green. Frontend: `tsc -b` + `vite build` green.** Each phase ends at that gate.

---

## 2. Target structure (modular monolith)

```
backend/
  Program.cs                      composition root: builder.Services.AddCoreGridPlatform(); .AddAssetsFeature(); … one line per module
  Domain/                         unchanged layout, one flat namespace; constants consolidated (see §6.1)
  Data/                           CoreGridDbContext + Migrations + Auditing interceptor (persistence kernel, not a feature)
  Features/
    Shared/                       cross-cutting kernel — the only namespace other features may import from each other
      Api/                        ApiExceptionFilter, ErrorEnvelope (ProblemDetails + message/code/errors),
                                  InvalidModelStateResponseFactory, CoreGridControllerBase
      Auth/                       Policies (Appendix B names), RoleGroups, AuthorizationExtensions,
                                  ServicePrincipal (claims detection, replaces AgentToolsAuthMiddleware),
                                  AuthorizationOutcomeLoggingMiddleware (SEC-ID-09)
      CurrentUser/                ICurrentUser + CurrentUserContext (resolved once per request; used by controllers,
                                  audit interceptor and the FR-006 org-filter provider)
      Exceptions/                 NotFound/Validation/BusinessRule/Conflict/Forbidden exceptions
      Paging/                     PagedQuery, PagedResult<T>, QueryableExtensions.ToPagedResultAsync, SortExtensions
      Scoping/                    DepartmentScope + DepartmentScopeExtensions (moved from Dashboard; used by lists)
      Storage/                    IFileStorageService, CloudflareR2StorageService, PhotoUploadValidator (shared)
      Reporting/                  CsvWriter, PdfComponents (StatBox/CountTable), ReportFormat resolver
      Finance/                    StraightLineDepreciation (the single depreciation / elapsed-life calculator)
      Http/                       CorrelationIdMiddleware, SecurityHeadersMiddleware
      Health/                     HealthChecks registration (DB, ThunderID issuer)
    Identity/                     ← moved from top-level Identity/ + Data/Auditing/CurrentUserAccessor + Me/
      IdentityModule.cs           registration
      RoleEnrichmentMiddleware.cs
      CurrentOrganizationProvider.cs
      IIdentityDirectory.cs, ThunderIdIdentityDirectory.cs
      MeController.cs, MeModels.cs
    Setup/                        flat, unchanged shape (+ SetupModule.cs, validation attributes)
    Users/                        flat, unchanged shape (+ UsersModule.cs, UserResponse mapper)
    OrgConfig/                    Controllers/ Services/ DTOs/ + OrgConfigModule.cs
    Assets/                       Controllers/ Services/ DTOs/ Helpers/ + AssetsModule.cs
    Maintenance/                  Controllers/ Services/ DTOs/ + MaintenanceModule.cs
    Transfers/                    Controllers/ Services/ DTOs/ + TransfersModule.cs
    Disposals/                    promoted to Controllers/ Services/ DTOs/ + DisposalsModule.cs
    Verification/                 Controllers/ Services/ DTOs/ + VerificationModule.cs
    Audit/                        flat + AuditModule.cs
    Dashboard/                    flat + DashboardModule.cs (DepartmentScope moves to Shared/Scoping)
    Notifications/                Controllers/ Services/ DTOs/ + NotificationsModule.cs
    Agents/                       Controllers/ Services/ DTOs/ + AgentsModule.cs
    AgentTools/                   Controllers/ Services/ DTOs/ + AgentToolsModule.cs (middleware file deleted)
```

Module convention: `public static class XxxModule { public static IServiceCollection AddXxxFeature(this IServiceCollection services) }`. `Program.cs` becomes: platform (json, swagger, auth, authz policies, rate limiting, health, cors, db) + one `Add…Feature()` per folder + middleware pipeline.

Decision (recommended, not forced): **Transfers and Disposals stay as two folders** even though both are Component C. Merging would rename every namespace the existing 86 Component-C tests reference for no behavioural gain. Disposals is promoted to the subfolder shape because it already has two services.

---

## 3. Phase 1 — Value-type under-posting fixes (your list, plus same-class siblings)

Rule applied per property, after reading how each is consumed:
- **Mandatory** → `[Required] T?` (+ `[Range]`/`[EnumDataType]` where meaningful); service reads `.Value` after model validation has run.
- **Optional** → `T?`, service substitutes the documented default.
- Records (`Users`, `Setup`) use `[property: Required]` on positional parameters.

| File | Property | Decision | Reason |
|---|---|---|---|
| `AgentTools/DTOs/AgentToolsDtos.cs` `ComputeDepreciationRequest` | `AcquisitionCost` | `[Required][Range(0, 1e12)] decimal?` | Computation is meaningless without it; today `0` silently returns a zero row. |
| | `AcquisitionDate` | `[Required] DateOnly?` | Same. |
| | `UsefulLifeYears` | `[Required][Range(1, 100)] int?` | Same; `<= 0` is currently a silent "no depreciation". |
| `Agents/DTOs/AgentWorkflowDtos.cs` `CreateAgentWorkflowRequest` | `AssetId` | `[Required] Guid?` | `Guid.Empty` today becomes "Asset not found" 400 by accident. |
| `Assets/DTOs/CreateAssetAttributeDefinitionRequest.cs` | `IsRequired` | `[Required] bool?` | Client must state intent; frontend modal always sends it. |
| `Assets/DTOs/CreateAssetRequest.cs` | `AssetTypeId`, `DepartmentId`, `LocationId` | `[Required] Guid?` | Mandatory FKs. |
| | `AcquisitionDate` | `[Required] DateOnly?` | Mandatory (FR-021). |
| | `AcquisitionCost` (your second "AcquisitionDate" line) | `[Required][Range(0, 1e12)] decimal?` | Mandatory; `CK_Assets_AcquisitionCost >= 0` becomes a 400 instead of a 409. |
| `Assets/DTOs/CreateAssetTypeRequest.cs` | `AssetCategoryId` | `[Required] Guid?` | Mandatory FK. |
| | `UsefulLifeYears` | `[Required][Range(1, 100)] int?` | `CK_AssetTypes_UsefulLifeYears > 0`. |
| `Assets/DTOs/UpdateAssetAttributeDefinitionRequest.cs` | `IsRequired` | `[Required] bool?` | Full-replace update. |
| | `DisplayOrder` | `[Required][Range(0, int.MaxValue)] int?` | Full-replace update (create keeps `int?` optional = auto-append). |
| `Assets/DTOs/UpdateAssetRequest.cs` | `AssetTypeId`, `DepartmentId`, `LocationId`, `AcquisitionDate`, `AcquisitionCost` | as Create | Full-replace update. |
| `Assets/DTOs/UpdateAssetTypeRequest.cs` | `AssetCategoryId`, `UsefulLifeYears` | as Create | Full-replace update. |
| `Disposals/DTOs/DisposalDtos.cs` `SubmitDisposalRequest` | `AssetId` | `[Required] Guid?` | Mandatory. |
| | `DisposalMethod` | `[Required] DisposalMethod?` | Absent today silently becomes `SCRAP` (enum default 0). |
| | `EstimatedResidualValue` | `[Required][Range(0, 1e12)] decimal?` | P2 needs a recorded amount; absent today reads as `0` and passes P2. |
| `Maintenance/DTOs/ApproveMaintenanceRequest.cs` | `AssigneeId` | `[Required] Guid?` | FR-036 assigns an officer. |
| | `EstimatedCost` | `[Required][Range(0, 1e12)] decimal?` | FR-036 records an estimate; BR1 depends on it. |
| `Maintenance/DTOs/CompleteMaintenanceRequest.cs` | `ActualCost` | `[Required][Range(0, 1e12)] decimal?` | FR-038/040. |
| | `CompletionDate` | `[Required] DateOnly?` | FR-038; absent today = `0001-01-01`. |
| `Maintenance/DTOs/CreateMaintenanceRequest.cs` | `AssetId` | `[Required] Guid?` | Mandatory. |
| | `Type` | `[Required] MaintenanceType?` | Absent today = `CORRECTIVE` by enum default. |
| | `Priority` | `[Required] MaintenancePriority?` | Absent today = `LOW` by enum default. |
| `Maintenance/DTOs/ReportFaultRequest.cs` | `AssetId` | `[Required] Guid?` | Mandatory. |
| `OrgConfig/DTOs/CreateLocationRequest.cs`, `UpdateLocationRequest.cs` | `DepartmentId` | `[Required] Guid?` | Mandatory FK. |
| `OrgConfig/DTOs/SaveOrganizationPolicyRequest.cs` | all 8 numerics | `[Required]` + `[Range]` (`decimal?`/`int?`) | The class comment says "full replace, never a partial patch"; a missing field today silently zeroes a threshold. Ranges: percentages/ratios `0–100`, `ConfidenceFloor` `0–1`, day/hour windows `0–3650`/`0–8760`. |
| `Transfers/DTOs/TransferDtos.cs` `InitiateTransferRequest` | `AssetId`, `ToDepartmentId`, `ToLocationId` | `[Required] Guid?` | Mandatory. |
| `Users/UsersModels.cs` | `UpdateUserRequest.Role` **and** `CreateUserRequest.Role` | `[property: Required] CoreGridRole?` | Absent today = `Staff` (enum 0) — an admin PATCH without `role` silently demotes a user. |
| `Verification/DTOs/CampaignDto.cs` `CreateCampaignRequest`, `UpdateCampaignRequest` | `PeriodStart`, `PeriodEnd` | `[Required] DateOnly?` | Absent today = `0001-01-01` and the "end before start" check passes. |
| | `UpdateCampaignRequest.Status` (sibling, not in your list) | `[Required] CampaignStatus?` | Absent today = `Active`, silently re-opening a completed campaign. |
| `Verification/DTOs/DiscrepancyDto.cs` `ResolveDiscrepancyRequest` | `ApplyCorrection` | `bool?` → treated as `false` | Optional by design (only two types support it). |
| | `RaiseDiscrepancyRequest.Type` (sibling) | `DiscrepancyType?` → `Other` | Already documented default; make it explicit. |
| `Verification/DTOs/VerificationTaskDto.cs` `CompleteVerificationTaskRequest` | `AssertedPresent` | `[Required] bool?` | The assertion *is* the request; absent today = "not present" → auto-raises a `Missing` discrepancy. |

Also in this phase (NFR-11): `[Required]`/`[MaxLength]`/`[EmailAddress]`/`[MinLength]` on the string members of every request DTO so the existing hand-written `IsNullOrWhiteSpace` checks in services collapse into model validation, and `[ApiController]`'s automatic 400 is reshaped by `InvalidModelStateResponseFactory` into the error envelope (§5.1) with a field-level `errors` array.

Compile fallout to fix in the same phase: every service that reads these properties (`.Value`), every test that constructs these DTOs (`DisposalServiceTests`, `TransferServiceTests`, `DiscrepancyResolutionServiceTests`, `AgentToolsServiceTests`), and the camelCase JSON in `AuthorizationMatrixTests` (B7).

Frontend impact: **none on the wire** — verified that every mandatory field above is already sent by the corresponding modal/page (`is_required`, `apply_correction`, `period_start/end`, `estimated_residual_value`, `estimated_cost`, all eight policy numbers, `asserted_present`).

---

## 4. Phase 2 — Shared kernel (`Features/Shared/`)

Built first because every feature refactor in Phase 3 depends on it.

### 4.1 Errors and controller shape
- `Exceptions/`: `NotFoundException`, `ValidationException` (field errors), `BusinessRuleException` (422), `ConflictException` (409), `ForbiddenException` (403, carries optional payload such as disposal preconditions).
- `Api/ErrorEnvelope`: `ProblemDetails` extended with `code`, `message`, `errors[]`, `correlationId`. `message` is kept so the frontend's `getErrorMessage` and the transfers/disposals `handle()` keep working unchanged; `preconditions` is emitted as an extension for `POST /disposals/{id}/approve`.
- `Api/ApiExceptionFilter`: single mapping (typed exceptions → their codes; `DbUpdateConcurrencyException` → 409; transient `NpgsqlException`/`TimeoutException` → 503 (NFR-24); anything else → 500 with a generic message and the exception logged with the correlation id — never the exception text (NFR-14)).
- `Api/CoreGridControllerBase`: no DbContext; exposes `CurrentUser` (see 4.2). All 71 `catch` blocks and 89 `new { message }` literals are deleted.
- Status-code alignment to §5.4: validation → 400, not found → 404, state-machine → 409, business rule → 422. Assets/OrgConfig/Verification/Maintenance move their business-rule failures from 400 to 422 (frontend handles `!response.ok` generically, so no UI change).

### 4.2 Current user, once
- `CurrentUser/ICurrentUser` (`Id`, `OrganizationId`, `Role`, `DepartmentId`, `IsServicePrincipal`) populated by `RoleEnrichmentMiddleware` from the single lookup it already performs, stored in a scoped `CurrentUserContext`.
- `CoreGridControllerBase.GetCurrentUserAsync`, `Data/Auditing/CurrentUserAccessor` (and its extra DI scope + query), `MeController`'s own lookup and `CurrentOrganizationProvider`'s claim parsing all read from the same context. Three DB round-trips per request become one.
- Deleted: `ICurrentUserAccessor`, `CurrentUserAccessor`, the `organization_id` claim rewrite (the provider reads the context directly; the `roles` claim rewrite stays because `[Authorize]` policies read it).

### 4.3 Paging
- `Paging/PagedQuery` (`Page`, `PageSize`, `SortBy`, `SortDirection`, `Search`) with clamping (`1 ≤ page`, `1 ≤ pageSize ≤ 100`; export paths may pass `MaxExportPageSize = 500`).
- `Paging/PagedResult<T>` (existing shape kept: `items`, `total_count`, `page`, `page_size`, `total_pages`).
- `QueryableExtensions.ToPagedResultAsync(query, pagedQuery, projection, ct)` and `SortExtensions.ApplySort(map)`. The six hand-rolled `Skip/Take/Ceiling` blocks (Users, AuditLog, Assets, AssetHistory, Maintenance, AuditReport) are replaced.

### 4.4 Authorisation
- `Auth/Policies`: the Appendix B names — `CanReadAssets`, `CanManageAssets`, `CanVerifyAssets`, `CanRequestMaintenance`, `CanManageMaintenance`, `CanRequestTransfer`, `CanApproveTransfer`, `CanConfirmReceipt`, `CanRequestDisposal`, `CanApproveDisposal`, `CanManageCampaigns`, `CanResolveDiscrepancy`, `CanReadAuditLog`, `CanManageConfiguration`, `CanManageUsers`, `CanInitiateWorkflow`, `CanReadWorkflows`, `CanApproveWorkflow`, `CanGenerateReports`, `CanReadNotifications`, `AgentToolAccess`.
- Role membership exactly as SRS §4.6, with the two documented deviations kept and recorded: `CanConfirmReceipt` also allows Administrator and `CanVerifyAssets` also allows Administrator (both already true today and covered by tests / `canActOnAnyTask`); `GET /api/users` stays readable by InventoryOfficer (needed for the assignee picker). `AgentToolAccess` = service-principal claims (`gty=client-credentials` or `client_id==sub`) — the logic from the dead middleware becomes a requirement handler.
- `FallbackPolicy = RequireAuthenticatedUser` (NFR-10) so an endpoint with no attribute fails closed; `[AllowAnonymous]` only on `/api/setup/*`, `/health`, swagger.
- SEC-ID-10 / AI-28: no write policy includes the service principal; a test proves it against every mutating route.
- SEC-ID-09: `AuthorizationOutcomeLoggingMiddleware` logs 401/403 with subject, org, endpoint, timestamp (never the token).
- The 16 role-string constants in controllers are deleted.

### 4.5 Department scoping (B14)
- `Scoping/DepartmentScope` moves out of Dashboard. `DepartmentScope.For(ICurrentUser)`; `IQueryable<Asset>.ApplyScope(...)`, plus overloads via `Asset` navigation for maintenance/transfers/disposals. Applied in Phase 3 to the list and detail endpoints of Assets, Maintenance, Transfers, Disposals for Staff (Appendix B "restricted to their own department by a service-layer filter").

### 4.6 Storage, reporting, finance helpers
- `Storage/PhotoUploadValidator`: size, MIME **and** magic-byte sniffing for JPEG/PNG/WebP (NFR-13), used by both photo endpoints (the two copies in `MaintenanceController` and `DiscrepanciesController` collapse).
- `Reporting/CsvWriter` and `Reporting/PdfComponents`: the duplicated `CsvEscape`/`WriteRow` and `StatBox`/`CountTable` from the two report services.
- `Finance/StraightLineDepreciation`: one implementation with `ElapsedWholeYears` and `ElapsedFractionalYears`, replacing the three separate calculations (`AssetService.CalculateResidualValue`, `AgentToolsService.ComputeDepreciation`, `DisposalPreconditionService.CheckP3`). Existing numeric behaviour of each caller is preserved (whole years for depreciation/P3, fractional for residual value/compliance state) and pinned by tests.

### 4.7 Platform middleware (`Program.cs` + `Shared/Http`, `Shared/Health`)
- `CorrelationIdMiddleware`: honours/creates `X-Correlation-Id`, echoes it, pushes a logging scope, and feeds `AuditSaveChangesInterceptor` so audit rows share the request's id (§5.4).
- `SecurityHeadersMiddleware`: `X-Content-Type-Options: nosniff`, `Referrer-Policy`, `X-Frame-Options: DENY`, `Permissions-Policy` minimal set.
- `UseHsts()` outside Development (NFR-09).
- `AddHealthChecks()` with Npgsql and a ThunderID issuer reachability check; `GET /health` anonymous, JSON per-dependency (NFR-20).
- `AddRateLimiter()`: per-user+org policies on `POST /api/agent-workflows` (AI-27), report exports, photo uploads and `POST /api/setup/complete` (NFR-16); 429 with the error envelope.
- `UseNpgsql(o => o.EnableRetryOnFailure(...))` (NFR-24).
- Swagger: policy names shown per operation.

---

## 5. Phase 3 — Feature-by-feature refactor

Common to every feature (not repeated below): thin controllers, typed exceptions, `ICurrentUser`, named policies, `CancellationToken` on every service method (currently missing in Assets, OrgConfig, Verification, Maintenance; `default` is passed 8 times in `MaintenanceController`), `Module.cs` registration, DTO projections defined once as `Expression<Func<TEntity, TDto>>` and reused by list/detail/create/update paths, redundant `using System;`-style imports removed (ImplicitUsings is on).

### 5.1 Identity / Me / Setup / Users
- Move `Identity/*` and `Me/*` into `Features/Identity/`; `Data/Auditing/CurrentUserAccessor` deleted (4.2).
- `UsersController`: `UserResponse` built four times → one mapper; `List` uses `PagedQuery`; Appendix B `CanManageUsers` "cannot deactivate own account" guard added; last-active-Administrator guard also applied to `Update` (role demotion), not only `Deactivate`.
- `SetupController`: `[AllowAnonymous]` explicit; request records get `[Required]`/`[EmailAddress]`/`[MinLength(8)]`; rate-limited.

### 5.2 OrgConfig
- Department/Location/Policy services: validation attributes replace the inline required/length checks; code-uniqueness and FK checks remain in services and throw `ConflictException`/`ValidationException`.
- Lists (`GET /departments`, `/locations`, `/organization-policies`) become paged with `search` (code/name) and `includeInactive` filter; `LocationService.SetLocationActiveAsync` stops re-querying the department name (uses the projection).

### 5.3 Assets
- `AssetService` (1,278 lines): shared projections for `AssetDto`/`AssetDetailDto`/`AssetAttributeValueDto`; `.ToList().ToList()` removed; `ValidateAttributes` + `AttributeValidationRuleEngine` stay but the condition/status constants come from `Domain` (B17); residual value from `Shared/Finance`; Staff department scope on list/detail/history/QR lookup (B14); `GetAssetsAsync` uses `PagedQuery`.
- `AssetTypeService` (713 lines): the five inline `AssetAttributeDefinitionDto` initialisers → one mapper; `GetAssetTypesAsync`/`GetCategoriesAsync` paged with `search` + `includeInactive`; attribute definitions stay unpaginated but capped (bounded sub-resource needed in full by the dynamic form; recorded as the one deliberate NFR-07 exception).
- `AssetHistoryEventTypes` extended to all seven constraint values and used everywhere (fixes B6 and removes every string literal).

### 5.4 Maintenance
- B6 fix: cancellation history entry uses `MAINTENANCE`.
- `MaintenanceRecordDto` projection defined once (currently duplicated in `GetById` and `List`); the InMemory-provider workaround (`ResolveAssetTypeNamesAsync`) stays but is documented in one place.
- Three copies of `validConditions` → `AssetConditions.All`; `ObservedCondition`/`ResultingCondition` validated by an attribute.
- `UploadPhoto` uses `PhotoUploadValidator`; policies `CanRequestMaintenance`/`CanManageMaintenance`; `CreateMaintenance` currently InventoryOfficer-only and `Complete` InventoryOfficer-only — kept as-is (stricter than the matrix) and recorded.
- Staff department scope on list/detail.

### 5.5 Transfers
- `Approve`/`ConfirmReceipt` stop building `TransferResponse` by hand with six ad-hoc queries; after `SaveChanges` they return `GetTransferByIdAsync` (one Include query, existing `MapToResponse`).
- `GET /transfers` and `GET /assets/{id}/transfers` paged; Staff scope.
- FR-027: `TRANSFER` history rows written on initiate/approve/confirm (B12).
- `CanConfirmReceipt` additional condition from Appendix B ("caller belongs to destination department") enforced for InventoryOfficer.

### 5.6 Disposals
- Promote to `Controllers/ Services/ DTOs/`.
- `DisposalApprovalResult` (a hand-rolled result union) is replaced by typed exceptions: separation-of-duties → `ForbiddenException` with preconditions payload; invalid state → `ConflictException`; failed preconditions → `BusinessRuleException` with payload. Controller shrinks to one call.
- `SubmitDisposalRequestAsync`/`ApproveDisposalAsync` reuse `MapToResponse` after reload instead of hand-building.
- `EvidenceUrl` recorded in the condemnation `AssetHistory` payload (B13).
- `GET /disposals` paged; Staff scope.

### 5.7 Verification
- `GetCampaignByIdAsync`, `CompleteTaskAsync`, `RaiseManualAsync`, `ResolveAsync` get real single-row queries (B18).
- Campaign/task/discrepancy lists paged with existing filters preserved (`campaignId`, `mine`, `onlyPending`, `onlyOpen`) plus `search`.
- `DeleteCampaignAsync` runs in one transaction (three `SaveChanges` today) and `catch (Exception)` in the controller goes away.
- FR-027: `VERIFICATION` history row on task completion (B12).
- `AuditReportService`/`CampaignReportService` use `Shared/Reporting`; `AuditReportFilter` becomes `PagedQuery`-based with the export path using the export cap.

### 5.8 Audit, Dashboard, Notifications
- `AuditLogController`: `_db` field removed (base already exposes nothing now → inject `CoreGridDbContext` directly or move the query into a small `AuditLogService`); `PagedQuery`.
- `DashboardController`: `GetChartsCore` indirection removed; `DepartmentScope` from Shared.
- `NotificationService.GetForUserAsync`: `Take(50)` → paged; `UnreadCountDto` kept.

### 5.9 Agents and AgentTools
- `AgentToolsController`: `?organizationId=` and single-org fallback removed (B5); org comes from `ICurrentUser` — for a human caller their org, for a service principal the org of the referenced asset/department/workflow row is **not** trusted from input either: the tool endpoints require `AgentToolAccess` and the M0 single-tenant org is resolved server-side once (documented as the M0 rule, matching `SetupController`'s one-organisation invariant). `compute-depreciation` loses `[AllowAnonymous]` (B4). Middleware file deleted (B3).
- `AgentWorkflowsController`: policies `CanInitiateWorkflow` (rate-limited), `CanReadWorkflows`, `CanApproveWorkflow`; list paged with the existing `status` filter.
- Dead DTOs removed: `PlannerObjectiveRequest`, `PolicyValidationRequest` (no references).
- FR-027: `AGENT_RECOMMENDATION` history row when a workflow records a recommendation (B12).
- `AgentWorkflowService` and `MaintenanceAnalysisAgentService` both build the same `AgentExecutionStep` for node 2 → one helper.

---

## 6. Phase 4 — Constants, dead code, duplication sweep

### 6.1 Single sources of truth (Domain)
- `AssetStatusConstants` → split into `AssetStatuses` (7 values + `All`) and `AssetConditions` (5 values + `All`, ordered as the dashboard needs). All 13 status literals and 6 condition arrays replaced.
- `AssetHistoryEventTypes`: all seven constraint values.
- `WorkflowDecisions` (`APPROVE/REJECT/REVISE`), `AgentNames`, `NotificationTypes` — string literals used across services today.

### 6.2 Removed
- `Features/AgentTools/AgentToolsAuthMiddleware.cs` (B3)
- `Data/Auditing/ICurrentUserAccessor.cs`, `CurrentUserAccessor.cs` (4.2)
- `Identity/ICurrentOrganizationProvider` claim-parsing implementation (replaced by context-backed one; interface kept for the DbContext filter and the tests' `NullCurrentOrganizationProvider`)
- `PlannerObjectiveRequest`, `PolicyValidationRequest`
- `DisposalApprovalResult`
- `DashboardController.GetChartsCore`
- `AuditLogController._db`
- The two `/api/maintenance/seed` tests (B2)
- 16 role-string constants, 71 controller `catch` blocks, 6 paging blocks, 6 condition arrays, 3 depreciation calculators, 2 CSV escapers, 2 PDF stat-box builders, 2 upload validators, 4 current-user lookups
- Root-level `seed_moe.sql`: **candidate only** — it hard-codes org/user ids and is not referenced by any doc, script or workflow. Left untouched unless you confirm deletion.

---

## 7. Phase 5 — Wire changes (the complete list)

Everything not in this table is byte-for-byte compatible.

| Endpoint | Change | Frontend edit (§10) |
|---|---|---|
| `GET /api/asset-categories`, `/asset-types`, `/departments`, `/locations`, `/organization-policies` | array → `PagedResult`; new `page,pageSize,sortBy,sortDirection,search,includeInactive` | hooks in `useAssets.ts`, `useOrgConfig.ts` read `.items`; pickers use a shared `fetchAllPages` helper |
| `GET /api/transfers`, `/assets/{id}/transfers`, `/disposals` | array → `PagedResult`; paging params | `useTransfers.ts`, `useDisposals.ts` |
| `GET /api/verification-campaigns`, `/verification-tasks`, `/discrepancies` | array → `PagedResult`; paging + `search` | `useCampaigns.ts`, `useCampaignTasks.ts`, `useDiscrepancies.ts` |
| `GET /api/agent-workflows` | array → `PagedResult` | `useWorkflows.ts` |
| `GET /api/notifications` | array (max 50) → `PagedResult` | `useNotifications.ts` |
| All 4xx/5xx bodies | `{message}` → error envelope that **still contains `message`**, plus `code`, `errors[]`, `correlation_id`; 400 for validation, 422 for business rules (Assets/OrgConfig/Verification/Maintenance change from 400 → 422 on business-rule failures) | none required; `getErrorMessage` already prefers `message` |
| `POST /api/agent-tools/compute-depreciation` | requires authentication (`AgentToolAccess` or any signed-in user) | none |
| `GET /api/agent-tools/*` | `organizationId` query parameter ignored/removed | none (no frontend caller) |
| `POST /api/agent-workflows`, report exports, photo uploads, `POST /api/setup/complete` | may return 429 | none |
| every response | `X-Correlation-Id` header added | none |
| `GET /health` | new | none |

---

## 8. Phase 6 — Tests and documentation

- `backend.Tests.csproj`: restore `<ProjectReference Include="..\backend\CoreGrid.Api.csproj" />` (B1). Fix compile fallout from §3 DTO changes.
- Update `AuthorizationMatrixTests` to snake_case JSON (B7), delete the seed tests (B2), extend to the full Appendix B matrix incl. **every mutating route denies the service principal** (SEC-ID-10).
- New tests: paging clamps and `total_count`; under-posting → 400 with field errors for each DTO in §3; error envelope shape and no-leak on 500; Staff department scoping on the four scoped lists; `MAINTENANCE_CANCELLED` regression (cancel an in-progress record succeeds); `StraightLineDepreciation` pins the numbers the three previous calculators produced; query filters on `AssetAttributeDefinition`/`AssetAttributeValue` (B8).
- Docs: `CONTRIBUTING.md` § Project Structure rewritten to §2; `CLAUDE.md` FR-006 note corrected (B9) and the "every controller resolves the caller via `GetCurrentUserAsync`" convention replaced by `ICurrentUser`; `doc/PROGRESS.md`: named policies → ✅, Staff scoping, health, rate limiting, HSTS, correlation id, test project fix; `backend/db/README.md` unchanged (no migration).

---

## 9. Correctness gaps found that are **not** part of the refactor unless you say so

These are real SRS gaps discovered during the survey. They are small, but they add behaviour rather than restructure it, so they are opt-in:

1. `POST /api/transfers/{id}/reject` and `POST /api/disposals/{id}/reject` (SRS §9.4; enum states and `RejectionReason` column already exist) — B11.
2. `POST /api/assets/{id}/verify` (SRS §9.2, FR-031) — verification today only exists through campaign tasks.
3. `PUT /api/maintenance/{id}` amend (SRS §9.3).
4. `GET /api/workflows/{id}/execution-summary` (SRS §9.6) — `AgentExecutionSteps`/`AgentApprovals` are persisted but never exposed.
5. Frontend pagination controls on the pages whose lists become paged (the hooks will request page 1 with a sensible size so nothing breaks, but no page navigation UI is added).

---

## 10. Frontend edits required to keep the app working (minimum set)

Only the files below change, and only to consume `PagedResult` where §7 lists a shape change:

- `frontend/src/shared/lib/apiClient.ts` (new, small): `fetchAllPages(fn)` helper for reference-data pickers — **not** a full HTTP-client consolidation (that is B22, a separate follow-up).
- `features/assets/api/assets.ts`, `hooks/useAssets.ts`, `types/asset.ts` — categories/types/departments/locations paged.
- `features/settings/api/orgConfig.ts`, `hooks/useOrgConfig.ts` — policies paged.
- `features/transfers/services/transfers.ts`, `disposals.ts`, `hooks/useTransfers.ts`, `useDisposals.ts`, `types.ts`.
- `features/audit/api/campaigns.ts`, `discrepancies.ts`, `verificationTasks.ts`, `hooks/useCampaigns.ts`, `useDiscrepancies.ts`, `useCampaignTasks.ts`.
- `features/workflows/api/workflows.ts`, `hooks/useWorkflows.ts`.
- `features/notifications/api/notifications.ts`, `hooks/useNotifications.ts`.
- Existing Vitest suites touching those hooks (`CampaignsPanel.test.tsx`, `DiscrepanciesPanel.test.tsx`, `WorkflowsPage.test.tsx`, `AuditLogPanel.test.tsx`) updated for the paged shape.

Gate: `npx tsc -b --force` and `npm run build` and `npm test` green.

---

## 11. Execution order and gates

| Step | Work | Gate |
|---|---|---|
| 1 | Fix `backend.Tests.csproj` reference; delete seed tests; snake_case the depreciation test | `dotnet build` 0/0 · `dotnet test` green (true baseline) |
| 2 | Phase 1 DTO under-posting (§3) + compile fallout in services/tests | build · tests |
| 3 | Phase 2 shared kernel (§4) — added alongside old code, nothing wired yet | build |
| 4 | Phase 3 per feature, in this order: Identity/Users/Setup → OrgConfig → Assets → Maintenance → Transfers → Disposals → Verification → Audit/Dashboard/Notifications → Agents/AgentTools. Each feature: move files, module registration, policies, exceptions, paging, scoping, dedupe | build · tests after **each** feature |
| 5 | Phase 4 sweep (§6): delete dead code, constants, `Program.cs` slimmed to modules + platform | build 0 warnings · tests |
| 6 | Frontend consumers (§10) | tsc · vite build · vitest |
| 7 | Phase 6 tests + docs (§8) | full test suite · `dotnet ef migrations has-pending-model-changes` still "No changes" |
| 8 | Manual smoke against the running stack (Postgres container is up): setup → sign in → asset register/list → maintenance report/approve/complete/cancel → transfer → condemn/dispose → campaign/verify/resolve → workflow → reports export → notifications | click-through per CONTRIBUTING "Before You Push" |

Commits: one per step above (or per feature inside step 4), on a feature branch from `development`, each referencing the FR/NFR/SEC ids it touches. No pushes without your go-ahead.

---

## 12. Decisions I will make unless you say otherwise

1. Validation uses DataAnnotations (as your instructions show), not FluentValidation (SRS NFR-11 names it). Same outcome — structured 400 with field errors — without a new dependency. Say "FluentValidation" if you want the package instead.
2. Transfers and Disposals remain two feature folders (§2).
3. `asset-types/{id}/attributes` stays unpaginated (bounded, needed in full by the dynamic form).
4. `GET /api/users` stays readable by InventoryOfficer for the assignee picker (deviation from `user:manage` Admin-only, already the case today).
5. Business-rule failures move from 400 → 422 in the four features that currently return 400 (§5.4 of the SRS).
6. `seed_moe.sql` is not touched.
7. Items in §9 are **not** done.

---

## 13. System marks (0–100), before and after each phase

A single "is this refactor working" number, scored against a fixed rubric so it's auditable rather than a vibe. **Baseline** and **After Phase 1** are measured against the real repo (build output, test run, and the B-item list in §0). **After Phase 2** through **After Phase 6** are targets computed by the same rubric against what that phase's own section (§4–§8) commits to fixing — each gets replaced with a measured number, and corrected if the real result differs, once that phase actually runs.

### 13.1 Rubric

Eight categories, weighted to 100. Each row gives what a 0 and a full-marks score mean, so a score is a claim that can be checked against §0's findings, not an opinion.

| # | Category | Cap | 0 means | Full marks means |
|---|---|---|---|---|
| 1 | Input Validation & Correctness | 15 | A value-type field silently defaults to a wrong value on omission, and at least one endpoint 500s on a legal input. | Every mandatory field is server-validated; every documented business rule (BR1–BR3, P1–P6, PR-01–PR-09) holds; no known 500-class defect. |
| 2 | Security & Authorization | 15 | An unauthenticated path exists where it shouldn't, org scope can be influenced by request content, and no role's data access actually matches SRS §4.6. | Every route declares a least-privilege named policy; org scope only ever comes from the local user mirror; the fail-closed default and the service principal's zero write permissions are both proven by test (SEC-ID-02, SEC-ID-10, AI-28). |
| 3 | Code Quality & Architecture | 15 | The same logic is duplicated in double digits across the codebase; there is no feature-module boundary; the composition root is a monolithic block. | One implementation per cross-cutting concern, living in `Shared/`; one `Module.cs` per feature; `Program.cs` reads as a manifest, not a 100-line block. |
| 4 | Test Coverage & CI Health | 15 | The test project does not compile. | It compiles, the full suite is green, and the SRS's Appendix B authorization matrix is proven end to end by an automated test. |
| 5 | Performance & Scalability | 10 | List endpoints return an unbounded result set; a "return one row" path loads the whole table into memory first. | Every list is paginated at the database with a clamped page size; no single-row lookup scans more than it needs to (NFR-07). |
| 6 | API Contract Consistency | 10 | The same class of failure returns different status codes in different features; every response is shaped ad hoc. | One status-code mapping used everywhere; one error envelope; a correlation id on every response (§5.4). |
| 7 | Observability & Resilience | 10 | No health endpoint, no rate limiting, no handling for a transient dependency failure. | `/health` reports each dependency individually (NFR-20); rate limits protect the auth-adjacent and cost-bearing routes (NFR-16); transient DB failures retry before failing (NFR-24). |
| 8 | Documentation & Traceability | 10 | The contributor-facing docs contradict the code. | `CLAUDE.md` / `CONTRIBUTING.md` / `doc/PROGRESS.md` accurately describe the current architecture, and every requirement id is traceable to real code. |

### 13.2 Score history

| Stage | 1. Correctness /15 | 2. Security /15 | 3. Code Quality /15 | 4. Tests /15 | 5. Performance /10 | 6. API Contract /10 | 7. Observability /10 | 8. Docs /10 | **Total /100** |
|---|---|---|---|---|---|---|---|---|---|
| **Baseline** (commit `9747b4a`, measured) | 6 | 5 | 5 | 2 | 4 | 4 | 2 | 6 | **34** |
| **After Phase 1** (measured, current `development`) | 12 | 5 | 6 | 11 | 5 | 5 | 2 | 7 | **53** |
| After Phase 2 (target) | 12 | 8 | 7 | 11 | 5 | 7 | 8 | 7 | **65** |
| After Phase 3 (target) | 14 | 14 | 12 | 13 | 9 | 9 | 8 | 7 | **86** |
| After Phase 4 (target) | 14 | 14 | 15 | 13 | 9 | 9 | 8 | 7 | **89** |
| After Phase 5 (target) | 15 | 14 | 15 | 14 | 10 | 9 | 8 | 7 | **92** |
| After Phase 6 (target) | 15 | 15 | 15 | 15 | 10 | 10 | 9 | 10 | **99** |

The full-plan target is **99, not 100** — see §13.4 for why one point deliberately stays open.

### 13.3 Why each score is what it is

**Baseline — 34/100.** Every category starts low for a concrete, cited reason: 30-odd mandatory fields across 15 DTOs silently default instead of validating and the test project doesn't compile at all (Correctness 6, Tests 2 — B1, B7, plus the whole §3 table); the agent-tools auth path is dead code sitting behind an anonymous endpoint that trusts organisation id from the query string (Security 5 — B3–B5, B14); duplication is the norm, not the exception — 16 role-string constants, 71 near-identical `catch` blocks, 4 different "who is the caller" implementations, 3 depreciation calculators (Code Quality 5 — B15–B17, B21); 14 endpoints have no pagination and several "return one row" methods load the whole table first (Performance 4 — B18, B19); status codes disagree feature to feature and there's no error envelope or correlation id (API Contract 4 — B16); none of `/health`, HSTS, rate limiting or retry-on-failure exist (Observability 2 — B20); and `CLAUDE.md` asserts FR-006 is unimplemented when it's actually on 17 entities (Documentation 6 — B9).

**After Phase 1 — 53/100 (+19).** The two categories Phase 1 actually targeted move the most: **Tests 2→11** (+9) — the compile break is fixed, the suite runs, 186/186 pass (161 pre-existing plus 25 a teammate's concurrent merge added) — held below full marks because there's no test yet asserting the new validation behaviour itself and the Appendix B matrix isn't built (that's §8). **Correctness 6→12** (+6) — every field in the §3 table is now `[Required]`/`[Range]` and verified against its real caller; held below full marks by B6 (the `MAINTENANCE_CANCELLED` 500), deliberately left for §5.4, and the NFR-11 string-validation sweep noted but not done in this pass. Everything Phase 1 didn't touch stays essentially flat: **Security** unchanged at 5 (B3–B5, B14, SEC-ID-09 all still open), **Code Quality** ticks 5→6 only for the extracted-local-variable pattern and its explanatory comments, not structural dedup. **Performance** ticks 4→5 and **API Contract** 4→5 for one external reason worth being honest about: a teammate's independent merge (`c92a52f`) paginated the Transfers and Disposals list endpoints while this refactor was in flight — real progress, but not this plan's work. **Observability** stays at 2 (no Phase 1 scope there) and **Documentation** ticks 6→7 for this plan document itself existing and tracking real findings, not for any correction to `CLAUDE.md` (that's still open, B9, §8).

**After Phase 2 (target) — 65/100 (+12).** Phase 2 is explicitly infrastructure "added alongside old code, nothing wired yet" (§11 step 3), so most categories should *not* jump yet — a target that claimed otherwise would be lying about what §4 actually does. The exception is anything registered globally in `Program.cs`, which takes effect the instant it's merged regardless of per-feature migration: the fail-closed fallback policy and `AuthorizationOutcomeLoggingMiddleware` land immediately (**Security** 5→8, real but partial — the named per-route policies and B3–B5/B14 fixes still need Phase 3), and the whole platform middleware set — `/health`, HSTS, rate limiting, retry-on-failure, security headers, correlation id — lands immediately too (**Observability** 2→8, the biggest single jump in the whole plan; **API Contract** 4→7 for the correlation id and the exception filter catching anything that isn't already handled by an old per-controller `catch`). **Code Quality** 6→7 only because the kernel now exists, well-factored, even though the old duplicated code it will replace is still sitting right next to it until Phase 3 deletes it. **Correctness**, **Tests**, and **Performance** don't move — no bug fixes, no new tests, no endpoint is paginated yet in this phase.

**After Phase 3 (target) — 86/100 (+21), the largest single-phase gain.** This is where the kernel actually gets wired into every feature, so nearly every open B-item closes at once: **Correctness** 12→14 (B6, B8, B12, B13 fixed — the real 500, the EF filter gap, the missing history rows, the dropped `EvidenceUrl`); **Security** 8→14 (B3 dead middleware deleted, B4 anonymous endpoint closed, B5 query-string org id removed, B14 Staff department scoping actually applied, every controller migrated off `Roles = "..."` strings onto the named policies §4.4 only defined until now); **Code Quality** 7→12 (B15–B17, B21 actually eliminated feature by feature, not just available); **Tests** 11→13 (new regression coverage for each of the above); **Performance** 5→9 (all remaining unpaginated endpoints paginated, the O(n) single-row lookups fixed — B18, B19); **API Contract** 7→9 (status codes now consistent everywhere the per-feature catches are deleted). One point is held back in Correctness and Security each for residual risk in a feature-by-feature migration this size, and the opt-in items in §9 stay undone by design.

**After Phase 4 (target) — 89/100 (+3).** A focused sweep, so a focused gain: **Code Quality** 12→15 (full marks — this phase's entire job is finishing the duplication and dead-code removal §6 lists, and slimming `Program.cs` to the module manifest §2 describes). Nothing else is in scope, so nothing else moves.

**After Phase 5 (target) — 92/100 (+3).** Closes the loop the backend-only Phase 3 opened: the paginated endpoints are only real if the one client that calls them (the React frontend) actually requests bounded pages instead of assuming an array — **Correctness** 14→15 and **Performance** 9→10 (both to full marks) for exactly that, plus **Tests** 13→14 for the updated Vitest suites.

**After Phase 6 (target) — 99/100 (+7), the ceiling.** The remaining single points close: **Security** 14→15 (the full Appendix B matrix, incl. every mutating route denying the service principal, is now an automated test — SEC-ID-10 stops being "true by inspection" and starts being "true by CI"); **Tests** 14→15 (that same matrix plus the pinned depreciation/paging/scoping regressions); **API Contract** 9→10 (documented and tested, not just implemented); **Observability** 8→9 (docs now correctly describe the platform pieces so future work doesn't quietly regress them); **Documentation** 7→10 (full marks — `CLAUDE.md`, `CONTRIBUTING.md` and `doc/PROGRESS.md` all corrected, B9 closed).

### 13.4 Why the ceiling is 99, not 100

**Observability & Resilience never reaches its full 10**, capping at 9 even after every phase. Two of that category's SRS requirements are outside what a backend code refactor can close: SEC-ID-11 (multi-factor authentication for Administrator) is a ThunderID console configuration, not application code, and NFR-16/AI-27 (rate limiting) is an SRS "Should", not "Must" — this plan wires the mechanism (§4.7) but tuning its limits against real traffic is a production-operations exercise, not a one-time code change. Separately, the five items in §9 (`reject` endpoints, `POST /assets/{id}/verify`, maintenance amend, the workflow execution-summary endpoint, frontend pagination controls) are real gaps but deliberately opt-in — doing them without being asked would be scope creep, so they stay off this scorecard's target path entirely. A 99 that's honestly short of 100 is more useful than a 100 that quietly assumes work nobody asked for.
