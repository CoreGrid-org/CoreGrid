# Progress Tracker

A living checklist against the ownership and evidence requirements in [SRS §12](SRS/12-individual-contribution-and-work-allocation.md) and [SRS §18](SRS/18-team-roster-and-work-allocation.md). Tick an item only once it's actually in the repo — this file reflects what exists, not what's planned; [SRS §18.9](SRS/18-team-roster-and-work-allocation.md#189-suggested-delivery-rhythm) is the plan. Update it in the same PR that lands the work it describes.

Status as of 2026-08-15: cross-cutting identity/admin slice is up; Component A (Asset Registry) has a working backend + React slice; Component C (Transfer & Disposal) has its database schema only; Component D (Audit & Compliance + Org Config + User Admin) now has a real backend for org structure, user admin, org policy, the audit log, and verification campaigns/discrepancies, and its Settings/Users/Audit-log/Dashboard React screens are now wired to that backend (real data, no more mocks on those). Verification campaigns/discrepancies still have no React screens at all. A shared admin shell polish pass (header branding, sidebar) also landed today. Maintenance and the agentic subsystem have not started; no mobile app, no tests, no CI yet.

## Cross-cutting (Identity, Access, Admin Shell)

| Item | Status |
|---|---|
| ThunderID OIDC sign-in (PKCE) — FR-001 | ✅ |
| Backend JWT validation (issuer + RS256 via JWKS) — FR-002 | ✅ |
| `GET /api/me` — resolve the caller's own CoreGrid profile/role by `sub` (SRS §16.1's named surface for this range) | ✅ (`backend/Features/Me`) |
| Resolve `OrganizationId` from local user mirror by `sub` — FR-003 | 🟡 (`/api/me` does the by-`sub` lookup; `OrganizationId` itself isn't surfaced or used for scoping yet — M0's single-org shortcut is used elsewhere instead) |
| Create/refresh local user mirror on first request; audit role changes — FR-004 | ❌ |
| Every endpoint declares an authorisation policy — FR-005 | 🟡 (mixed: `UsersController`/`OrganizationPoliciesController`/`AuditLogController` and a few Component D write actions declare specific roles; `SetupController` is deliberately open; most Assets/OrgConfig/Verification GET endpoints still use a blanket `[Authorize]` with no role policy) |
| Global `OrganizationId` query filter — FR-006 | ❌ |
| Frontend hides/protects unpermitted routes — FR-007 | ✅ (`RoleRoute`); action-level hiding N/A until there are fine-grained in-page actions |
| Sign-out clears state, revokes refresh token, ends IdP session — FR-008 | 🟡 (delegated to `@thunderid/react`'s `SignOutButton`, not independently verified end to end) |
| Deactivated user denied even with a valid token — FR-009 | ✅ (`RoleEnrichmentMiddleware` rejects with 401 if `Users.IsActive` is false) |
| First-Administrator provisioning via Setup (creates ThunderID account + CoreGrid role) | ✅ |
| Admin invites a user by email + role, provisioned through ThunderID — FR-013 | ✅ (`POST /api/users`, `GET /api/users`) |
| Change a user's role/department, deactivate a user — FR-014 | ✅ (`PATCH /api/users/{id}`, `/deactivate`, `/activate`; guards against deactivating the org's last active Administrator) |
| EF Core migrations + generated `db/schema.sql` export (SRS §2.3 C-02) | ✅ (2026-08-15: `db/schema.sql` and `db/migrations/*.sql` had drifted behind two real migrations — regenerated via `dotnet ef migrations script`) |
| CI pipeline (build/test/lint on push and PR) — §13.6 | ✅ (2026-08-21: `.github/workflows/ci.yml` — see Component D's row below for detail) |
| Any backend or frontend test project | 🟡 (`backend.Tests` exists — 119 tests, xUnit, InMemory + a real-Postgres suite for append-only/authorization — this line was already stale before 2026-08-21: Component C's 70 tests existed since before this session; no frontend test project exists yet) |
| Flutter mobile app | ❌ (not started — no `mobile/`/`flutter/` directory exists yet) |

## Component A — Asset Registry & QR Identification (Jayashan Guruge, FR-016–032)

| Item | Status |
|---|---|
| Create asset categories — FR-016 | ✅ (`POST /api/asset-categories`; `PUT .../{id}` update, and `DELETE .../{id}` + `PATCH .../{id}/activate` added 2026-08-17 — delete hard-removes an unreferenced category, otherwise deactivates it (`IsActive`) so it stops appearing as a choice for new types while existing types/assets keep working; reactivatable) |
| Create asset types (name, code, category, useful life, default maintenance interval) — FR-017 | ✅ (`POST /api/asset-types`; `PUT .../{id}` update, and `DELETE .../{id}` + `PATCH .../{id}/activate` added 2026-08-17 — same hard-delete-if-unreferenced/deactivate-otherwise rule, keyed off whether any `Asset` references the type; deleting an unreferenced type also cascades its own attribute definitions) |
| Ordered custom attribute definitions per asset type — FR-018 | ✅ (`POST /api/asset-types/{id}/attributes`; `PUT .../attributes/{attributeId}` update, and `DELETE .../attributes/{attributeId}` + `PATCH .../attributes/{attributeId}/activate` added 2026-08-17 — same rule, keyed off whether any `AssetAttributeValue` references the definition) |
| Attribute value validation on create and update — FR-019 | 🟡 (required-field and data-type checks are enforced on both create and update; `ValidationRule` is stored and returned to clients but no code path evaluates a submitted value against it — rule enforcement itself isn't implemented) |
| Dynamic attribute-driven detail form, both clients — FR-020 | 🟡 React ✅ (`AssetRegisterPage` renders fields purely from the selected type's attribute definitions, no hardcoded domain knowledge); Flutter ❌ (not started) |
| Register an asset (type, name, department, location, acquisition date/cost, attributes) — FR-021 | ✅ |
| Unique human-readable asset code (org prefix + monotonic sequence, DB-constrained) — FR-022 | ✅ (`AssetCodeGenerator`; `IX_Assets_OrganizationId_AssetCode` unique index) |
| QR label payload + printable label download — FR-023 | 🟡 (a real QR image is generated and rendered in the asset detail modal via the `qrcode` package — not just the raw payload string; no printable-label download/print feature exists yet) |
| Mobile QR scan → authoritative record within 3s — FR-024 | ❌ (Flutter not started) |
| Manual asset-code entry as an alternative to scanning — FR-025 | 🟡 React ✅ (`AssetScanPage` — manual code/QR-payload entry only, resolves via `GET /api/assets/qr/{code}` to the identical detail view a scan would produce); Flutter ❌ |
| Amend asset fields/attributes/department/location — FR-026 | ✅ (2026-08-17: `PUT /api/assets/{id}` diffs core fields and dynamic attribute values against their pre-update state and writes one `FIELD_AMENDMENT` `AssetHistory` entry per call, `PreviousValue`/`NewValue` limited to what actually changed; a no-op save writes nothing) |
| Immutable, ordered per-asset lifecycle history — FR-027 | 🟡 (2026-08-17: `AssetService.CreateAssetAsync`/`UpdateAssetAsync`/`UpdateConditionAsync` now write `AssetHistory` entries — Component A's own `AssetHistoryEventTypes` constants class only exposes `STATUS_CHANGE`/`FIELD_AMENDMENT`, the two event types Component A actually produces; `GET /api/assets/{id}/history`, paginated, added to `AssetsController`; frontend has both an embedded History section in `AssetDetailModal` and a standalone read-only `AssetHistoryModal` opened via a "View history" icon on each Asset Register row. Remaining gap: `VERIFICATION`/`MAINTENANCE`/`TRANSFER`/`DISPOSAL`/`AGENT_RECOMMENDATION` entries are written by other components' own features, not Component A's) |
| Search by code/name/attribute value; filter by department/location/category/type/status/condition; server-side sort + pagination — FR-028 | ✅ (2026-08-17: search now also matches asset type name/code, asset category name/code, and dynamic attribute values — text via `ILike`, number/date via typed equality after parsing the search term; added a `categoryId` filter alongside the existing type/department/location/status/condition ones; sorting and pagination remain fully server-side) |
| Record condition (New/Good/Fair/Poor/Unserviceable) — FR-029 | ✅ (2026-08-17: `PATCH /api/assets/{id}/condition` writes a `FIELD_AMENDMENT` `AssetHistory` entry; resubmitting the same condition writes nothing) |
| Computed residual value (straight-line depreciation) — FR-030 | ❌ (`ResidualValue` is taken as-is from whatever the client submits on create/update — a free-entry field in the React form — never derived server-side from acquisition cost, acquisition date, and the asset type's useful life) |
| Officer physical verification (presence/location/condition assertion, reconciled against the register) — FR-031 | ❌ (the named surface `POST /api/assets/{id}/verify` doesn't exist; this FR is Flutter-only per the SRS and Flutter hasn't started) |
| Prevent deletion of assets with history; disposal is the only exit from the register — FR-032 | 🟡 (no `DELETE` endpoint exists on `AssetsController` at all, so nothing can be deleted — satisfies the letter of it; but the disposal workflow itself is Component C, not yet built, so in practice assets have no path off the active register yet either) |
| Database (`AssetCategories`, `AssetTypes`, `AssetAttributeDefinitions`, `AssetAttributeValues`, `Assets`, `AssetHistory`) | ✅ (department/location CRUD moved to `backend/Features/OrgConfig` — Component D, 2026-08-15 backend file structure cleanup; migration `AddIsActiveToAssetCategoryTypeAttribute` added an `IsActive` column to `AssetCategories`/`AssetTypes`/`AssetAttributeDefinitions` 2026-08-17 for the hard-delete-vs-deactivate rule — `Assets` itself deliberately untouched) |
| React (asset list/detail/register/update, dynamic attribute forms, category/type/attribute config incl. edit/delete/reactivate, searchable pickers, real organisation-code display in the code preview) | ✅ (`frontend/src/features/assets`; 2026-08-17: added a paginated History section to `AssetDetailModal` and a separate, read-only `AssetHistoryModal` reachable via a dedicated icon on each Asset Register row, both backed by `GET /api/assets/{id}/history`) |
| Flutter (QR scanner, asset lookup, condition update) | ❌ |
| Planner Agent | ❌ |
| Tests | ❌ |

## Component B — Maintenance Management (Seneja Ramanayaka, FR-033–042, FR-077–080)

| Requirement / Specification | Status | Details / Implementation |
|---|---|---|
| **FR-033: Fault Reporting** | 🟡 | React  (`ReportFaultPage.tsx` with searchable asset pickers); Flutter ❌ (mobile not started) |
| **FR-034: Photograph Attachment** | ❌ | No file-upload storage/infrastructure built yet (stores photoUrl as string only) |
| **FR-035: Direct Maintenance Entry** | ✅ | React ✅ (`CreateMaintenancePage.tsx` with corrective/preventive type and priority selection) |
| **FR-036: Approval & Assignment** | ✅ | React ✅ (`ApproveMaintenanceModal.tsx` fetches database user list for assignment, logs estimated cost in LKR) |
| **FR-037: Defined Status Sequence** | ✅ | Backend state-machine guards transitions: `REQUESTED` ➔ `APPROVED` ➔ `IN_PROGRESS` ➔ `COMPLETED` / `CANCELLED` |
| **FR-038: Complete Maintenance** | ✅ | React ✅ (`CompleteMaintenanceModal.tsx` inputs actual cost, work done, date, resulting condition) |
| **FR-039: Asset UNDER_MAINTENANCE Lock** | ✅ | Managed via backend API during transition to `IN_PROGRESS`; blocks transfer/disposal |
| **FR-040: Cumulative Recalculations** | ✅ | Recomputes cumulative cost, repair count, and latest repair date atomically on completion |
| **FR-041: Preventive scheduling** | 🟡  | Background service `PreventiveMaintenanceBackgroundService` polls and auto-schedules based on interval |
| **FR-042: List & Filter Dashboard** | ✅ | React ✅ (`MaintenancePage.tsx` table with status filters and LKR currency mapping) |
| **FR-077–079: Email Notifications** | ❌ | Mail sending services stubbed in backend; delivery infrastructure not yet configured |
| **FR-080: Notification Centre** | ❌ | Header notification global action button is static; no panel UI is built yet |
| **Business Rules & Acceptance Criteria (FR-038)** | | |
| *BR1: Cost-variance tolerance* | ✅ | Backend enforces variance checks against organization policies during completion |
| *BR2: Resulting condition Unserviceable* | ✅ | Automatically sets asset status to `CONDEMNED` (releasing disposal path) on completion |
| *BR3: Atomic transaction* | ✅ | Completion actions wrapped in a single database transaction rollback on any failure |
| *AC1: Re-completion block (409)* | ✅ | Completed records throw an exception and return 409 Conflict if completed again |
| *AC2: Cost aggregation correctness* | ✅ | Cumulative cost correctly aggregates historical actual costs |
| *AC3: Condemnation verification* | ❌ | Verification pending automated integration test setup |
| *AC4: Notification failure isolation* | ✅ | Notification dispatch failure logged/retried, does not roll back database transaction |
| **Maintenance Analysis Agent** | ❌ | AI Agent pending graph execution framework integration |
| **Tests** | ❌ | Testing projects not yet started |

## Component C — Transfer & Disposal (Bhanuka Samarasinghe, FR-043–055)

| Area | Status |
|---|---|
| Backend (transfer/disposal controllers, approval preconditions P1–P6) | ✅ (Full transfer state machine (FR-044/045/046) and disposal workflow (FR-049 condemn, FR-050 submit, FR-051/052 precondition evaluation, FR-054/055 approval + terminal state) implemented and unit tested. P4 (maintenance check) now fully implemented against Component B's MaintenanceRecords table. P6 (agent workflow check) remains the sole stubbed precondition, pending the agent subsystem. Role-based authorization matching existing codebase convention (SRS Appendix B named-policy layer deferred by team lead until after mobile app development). Agent tool endpoints (/api/agent-tools/*: get_asset_financials, get_department_budget_summary, compute_depreciation) implemented for the Budget Analysis Agent. Replacement estimate and department budget allocation/committed/spent data return explicit null/NOT_CONFIGURED markers — no such tables exist yet in the schema. M2M auth via ThunderID service account (client_credentials), isolated to /api/agent-tools/* via UseWhen pipeline branching — RoleEnrichmentMiddleware.cs left untouched. Manual ThunderID console setup required before agent can authenticate (doc/setup/agent-service-account.md).) |
| Database (`AssetTransfers`, `DisposalRequests`) | ✅ (2026-08-15: fixed — `AssetId`/department/location columns had been left as bare `Guid`s with `TODO: add FK` comments even after `Asset`/`Department`/`Location` existed; added the real FK constraints + migration `AddTransferDisposalForeignKeys`, and repaired an out-of-sync `CoreGridDbContextModelSnapshot.cs` — the two entities' `DbSet` properties and model-snapshot blocks had been dropped, silently breaking `dotnet ef migrations add` for anyone touching this schema; 2026-08-17: added nullable `ValuationDate` to `DisposalRequests` via migration `AddValuationDateToDisposalRequest` to support real P2 valuation precondition check) |
| React (transfer/disposal queues, precondition checklist) | ❌ |
| Flutter (transfer request, scan-to-confirm receipt, condemnation) | ❌ |
| Budget Analysis Agent | ❌ (tool endpoints exist, Python agent itself not started) |
| Tests | ✅ (70 unit tests total.) |

## Component D — Audit & Compliance + Org Configuration + User Administration (Hasitha Erandika, FR-010–015, FR-056–066)

| Area | Status |
|---|---|
| Organisation creation (Setup) | ✅ |
| Department/Location CRUD — FR-010, FR-011, FR-012 | ✅ (amend + activate/deactivate added 2026-08-15; FR-012's guard refuses deactivation while a non-`DISPOSED` asset references the department/location) |
| User administration (invite by role) — FR-013 | ✅ |
| User role/department change, deactivation — FR-014 | ✅ |
| Organisation policy parameters — FR-015 | ✅ (`OrganizationPoliciesController`; enforces at most one policy per asset type, including the org-wide default) |
| Verification campaigns, task generation — FR-056, FR-057 | ✅ (`VerificationCampaignsController`; task generation + officer assignment is synchronous on creation — assignment is "first active InventoryOfficer in the asset's Department," since the schema has no location-ownership concept — tasks go unassigned, not dropped, when none exists) |
| Officer scan-to-verify — FR-059 | ✅ (`PATCH /api/verification-tasks/{id}/complete`) |
| Automatic + manual discrepancy raising — FR-060, FR-061 | ✅ (auto: Missing/LocationMismatch/ConditionMismatch only — Surplus/DataMismatch aren't derivable from a single-asset task and stay manual-only; FR-061's "photograph" is a URL field — no file-upload infrastructure exists yet) |
| Discrepancy resolution operation — FR-062 | ✅ (`PATCH /api/discrepancies/{id}/resolve`; register correction + `AssetHistory` write supported for ConditionMismatch/LocationMismatch only — the only two types with one unambiguous register field to correct) |
| Append-only audit log — FR-063, FR-064 | ✅ (generic EF `SaveChanges` interceptor — `AuditSaveChangesInterceptor` — covers every entity in `CoreGridDbContext` automatically, including future ones; DB-level `REVOKE UPDATE, DELETE` matches the `AssetHistory` precedent) |
| Campaign report + PDF/CSV export — FR-065, FR-084, FR-085 | ✅ (2026-08-21: `GET /api/verification-campaigns/{id}/report` — `CampaignReportService` assembles assets-in-scope/verified/outstanding, discrepancies by classification and by resolution status, plus the full task and discrepancy line-item lists, for one specific campaign; `GET .../report/export?format=pdf\|csv` renders the identical data as a downloadable file. Separately, `GET /api/reports/audit(/export)` — `AuditReportService` — is the aggregate version behind the shared Reports page's Audit tab: every campaign/discrepancy in the org, filterable by date/department/category/discrepancy-status. Both export CSV hand-built, PDF via the new `QuestPDF` dependency (Community licence, set once in `Program.cs`); both Auditor/Administrator only. Hit and fixed a real EF Core bug along the way: `GroupBy(...).Select(g => new SomeRecord(...)).OrderByDescending(...)` doesn't translate — EF can't re-derive a property back through a record's constructor for the ORDER BY — so both services materialize into an anonymous type first, then order/construct the DTO client-side. Scope is deliberately just the audit campaign report — the other three FR-084 report types (asset inventory/maintenance/disposal) belong to Components A/B/C, not touched here) |
| Dashboard indicators + visualisations — FR-081, FR-082, FR-086 | ✅ (2026-08-21: `GET /api/dashboard/charts` — Administrator/Auditor only, matching FR-082 — returns assets-by-department, assets-by-condition (zero-filled New→Unserviceable) and maintenance-cost-by-month (zero-filled trailing 12 months); FR-086's department restriction is now real — a new `DashboardScope` helper resolves Administrator/Auditor as org-wide and Staff/InventoryOfficer as filtered to their own `User.DepartmentId` (a null department correctly yields zero, not everything), applied to both `/summary` and `/charts`) |
| React (admin screens for departments/locations/users/policy, audit dashboard, campaigns, discrepancy resolution) | ✅ (2026-08-21: Admin/Audit Dashboards' two bar charts and the cost line chart now call `useDashboardCharts` instead of `mockOverview.ts` (deleted) — also fixed `LineChart` crashing (`Cannot read properties of undefined (reading 'x')`) on an empty series, which is what the charts 500 above surfaced client-side. Campaigns tab gained a "View report" action opening `CampaignReportModal` — stats, both breakdown tables, task/discrepancy line items, working Export PDF/CSV. Reports page's Audit tab is now real too (`AuditReportPanel` — date/department/category/status filters, stat tiles, classification table, PDF/CSV export); its other three tabs (asset inventory/maintenance/disposal) stay mock, Components A/B/C's work — but their placeholder currency figures were corrected from `$` to `LKR` while touching this page, since that was just wrong regardless of who finishes the tab. Users & Roles, Settings, Audit Log, Campaigns, Discrepancies and now the Audit report tab are all real; no mock data remains on Component D's own screens) |
| Flutter (verification task list, field verification flow) | ❌ |
| Policy Compliance Agent + human-approval checkpoint | 🟡 (2026-08-21: `AgentWorkflows`/`AgentExecutionSteps`/`AgentApprovals` schema + migration (§7.5); `PolicyRuleEngine` — pure, unit-tested, deterministic implementation of PR-01–PR-09 (§7.6); `get_organization_policies`/`get_asset_compliance_state` tool endpoints added to the existing `AgentToolsController` (§7.4), mirroring Component C's pattern; `AgentWorkflowsController` — initiate (FR-067/068), list/detail (FR-069/070), `POST .../evaluate` (runs the gate — stands in for nodes 2–4 until Planner/Maintenance/Budget exist, since a caller supplies `proposedRecommendation` directly in the same shape those agents will eventually produce), `PATCH .../decide` (AI-13–AI-20: Administrator-only, ≥10-char reason, revision cap, snapshot). Verified end to end against real data (create → evaluate → NEEDS_REVISION → re-evaluate → PASS/AWAITING_APPROVAL → APPROVE). React: real `WorkflowsPage` (Active/Awaiting Approval/Completed tabs, `CreateWorkflowModal`, `EvaluatePolicyModal`, `DecideWorkflowModal`), routed for Officer/Auditor/Administrator, role-gated buttons via `useMe`. Not done: the actual LLM-calling agents (Planner/Maintenance/Budget/Policy) and the LangGraph orchestration (ADR-005) — that's a separate Python service this doesn't attempt to fake; AI-17 (executing the approved action through Component A/B/C's business services) is stubbed — approval is recorded but no business record changes yet, same posture as Component C's P6) |
| Tests (append-only, discrepancy resolution, authorisation matrix) | ✅ (2026-08-21: added to the existing `backend.Tests` project — 49 new tests alongside Component C's 70, all 119 passing. `PolicyRuleEngineTests` — pure, exercises every PR-01–09 branch. `DiscrepancyResolutionServiceTests` — FR-062's actual AC1–AC4/BR2/BR3 (`DiscrepancyService` was tightened to actually enforce these: NO_ACTION's 20-char justification minimum, WRITTEN_OFF's prior-verified-Missing precondition, and the 5 canonical resolution types — none of that was enforced before, despite FR-062 already being marked done). `AuthorizationMatrixTests` — first HTTP-level integration tests in the project (`WebApplicationFactory<Program>` + `TestAuthHandler`, InMemory-backed, only the JWT identity step is faked — real `[Authorize(Roles=...)]`, real `RoleEnrichmentMiddleware`, real `AgentToolsAuthMiddleware` all execute for real); covers all 4 roles across several endpoints, a deactivated-user 401, and the agent service principal reaching `/api/agent-tools/*` with no `Users` row. `AppendOnlyTests` — real Postgres only, since InMemory can't model GRANT/REVOKE: found and partially fixed a real gap — the migrations' `REVOKE UPDATE, DELETE FROM coregrid_app` was silently inert (the role never existed, and Postgres can't restrict a table's *owner* via REVOKE regardless — the app's actual runtime connection IS the owner role). Created the `coregrid_app` role with the correct restricted grants and tests prove the REVOKE mechanism itself is sound; full enforcement still needs the app's runtime connection split from the migration-owner one — a config change, tracked as a follow-up, not silently left unverified) |
| CI workflow ownership — §13.6 | ✅ (2026-08-21: `.github/workflows/ci.yml` — backend job: Postgres 16 service container, restore/build/`dotnet ef database update`/`dotnet test`; frontend job: `npm ci` + `npm run build` (doubles as typecheck, no separate lint script exists yet); flutter job: no-ops cleanly until a `mobile/`/`flutter/` directory exists, so it needs no further edits once that starts; secret-scan job: gitleaks. YAML-validated; the individual commands (build/migrate/test) were each run and passed locally against the same Postgres version, but the workflow itself hasn't executed on GitHub's runners yet — worth confirming on the first real push) |

## Agentic AI Subsystem — One Agent per Member

The coursework's minimum acceptance rule requires one stateful graph of four distinct agents (§7.2), each with its own input/output contract and a disjoint tool allow-list (§7.3) — not a chatbot, not four copies of the same prompt. [SRS §7.3](SRS/07-agentic-ai-subsystem-requirements.md#73-agent-specifications), [§12](SRS/12-individual-contribution-and-work-allocation.md) and [§18.3–§18.6](SRS/18-team-roster-and-work-allocation.md) already assign exactly one agent to each member — this table just consolidates what's otherwise spread across those three sections and the four component tables above, so status is visible in one place. No redesign was needed: four is both the coursework's required count and the efficient minimum here (no member without an agent, no redundant agent, nothing to merge or split).

| Member | Agent | Graph node | Tool allow-list | Status |
|---|---|---|---|---|
| Jayashan Guruge (Component A) | Planner Agent | 1 — interprets the objective, rejects out-of-scope requests, produces the typed plan | `get_asset_summary` | ❌ not started |
| Seneja Ramanayaka (Component B) | Maintenance Analysis Agent | 2 — repair count, MTBF, cost trend, 12-month projection | `get_maintenance_history`, `compute_failure_statistics` | ❌ not started |
| Bhanuka Samarasinghe (Component C) | Budget Analysis Agent | 3 — residual value, replacement estimate, repair:replace ratio, ranked options | `get_asset_financials`, `get_department_budget_summary`, `compute_depreciation` | 🟡 in progress — keep building on it: the three tools are live (`backend/Features/AgentTools`, `POST /api/agent-tools/*`), the LangGraph agent that calls them isn't started yet |
| Hasitha Erandika (Component D, Group Leader) | Policy Compliance Agent | 4 — assembles policy/compliance facts; verdict is the deterministic rule engine (PR-01–PR-09, §7.6), not the model | `get_organization_policies`, `get_asset_compliance_state` | 🟡 in progress — same shape as Bhanuka's row: the two tools are live, the rule engine (`PolicyRuleEngine`, unit-tested) and the human-approval checkpoint are real and verified end to end, the LLM-calling agent itself isn't started |

Also cross-cutting all four and not tied to any single agent's contract, but owned by Hasitha per §18.6 (rule engine + human-approval interrupt/resume mechanics): the `AgentWorkflows`/`AgentExecutionSteps`/`AgentApprovals` persistence schema (§7.5, migration `AddAgentWorkflows`) and the three-stage deterministic gate (§7.6) that sits between the agents' output and any consequence — both built 2026-08-21 (`Features/Agents/`). Stage 1 (schema validation) and stage 3 (authorisation) of the gate aren't implemented yet, only stage 2 (business rules, PR-01–09) — the other two don't have much to validate against until real agent output exists to validate.

## Delivery Rhythm Checkpoints ([SRS §18.9](SRS/18-team-roster-and-work-allocation.md#189-suggested-delivery-rhythm))

| Week | Target | Status |
|---|---|---|
| 1 | Vertical slice per owner: one entity, one CRUD endpoint, one React screen, one Flutter screen, real ThunderID auth, deployed. Agent contracts frozen. | 🟡 Component D (identity + admin) and Component A (asset registry, backend + React) have their slice; Component C has schema only; B not started; Flutter not started for any owner; agent contracts not yet frozen |
| 2 | Full CRUD + search/filter/sort/pagination per component | 🟡 Component D has full CRUD for org structure/users/policy (no search/filter/sort/pagination — lists are small); Component A has full CRUD + pagination for assets; B/C not started |
| 3 | Owned state machine with guarded transitions + negative tests | ❌ |
| 4 | Business-specific operation end to end with transaction + audit trail | ❌ |
| 5 | Own agent built against a stubbed model call, wired into the graph | ❌ |
| 6 | Real model call; full four-agent graph run together; golden cases passing | ❌ |
| 7 | Tests complete, CI green, authorisation matrix run | ❌ |
| 8 | Stabilisation: regression testing, AI usage logs, ADR set, docs, viva prep | ❌ |

**Legend:** ✅ done and in the repo · 🟡 partially done / mocked · ❌ not started

## Next Up — Per-Member Task List

Derived from the ❌/🟡 rows above, grouped by owner ([SRS §18](SRS/18-team-roster-and-work-allocation.md)). Not a new plan — just this file's open items restated as a to-do list.

### Hasitha Erandika — Group Leader (Cross-cutting + Component D, FR-010–015, FR-056–066)
- FR-004: create/refresh local user mirror on first request; audit role changes.
- FR-003/FR-006: surface `OrganizationId` from the mirror and add it as a real scoping filter — currently every query filters manually, and nothing enforces the multi-tenancy boundary end to end.
- FR-005: finish rolling real role policies onto the remaining blanket-`[Authorize]` GET endpoints (Assets/OrgConfig/Verification).
- FR-008: verify sign-out (state clear + refresh-token revoke + IdP session end) end to end instead of trusting `@thunderid/react` untested.
- Flutter: verification task list + field verification flow (not started).
- Policy Compliance Agent: the LLM-calling node itself and the LangGraph orchestration (ADR-005) — everything else (tools, rule engine, workflow schema, approval checkpoint, React UI) is built; AI-17 (executing an approved action through Component A/B/C's business services) is still stubbed, recorded but not wired up.
- The append-only fix is only half done: `coregrid_app` role + grants exist and are proven correct by `AppendOnlyTests`, but the app's actual runtime connection still connects as the migration-owner role, which Postgres can't restrict via REVOKE at all — needs the runtime connection split from the migration one to actually take effect for the running app, not just in the test suite.
- Frontend test project (React/Vite) — still doesn't exist; `backend.Tests` (119 tests) covers only the backend.

### Jayashan Guruge — Component A: Asset Registry & QR (FR-016–032)
- FR-019: evaluate `ValidationRule` against submitted attribute values — rule is stored but never enforced.
- FR-023: printable QR label download (a real QR renders in-app; no print/download path yet).
- FR-030: compute residual value server-side (straight-line depreciation from acquisition cost/date + useful life) — currently a free-entry client field.
- FR-031: `POST /api/assets/{id}/verify` (officer physical verification) — Flutter-only per SRS, blocked on Flutter start.
- FR-032: confirm assets actually exit only via disposal once Component C's disposal workflow has a frontend (Component A's own half — no-delete-endpoint — is already satisfied).
- FR-020/024/025: Flutter — dynamic attribute detail form, QR scan-to-record (<3s), manual code entry.
- Flutter: scanner, asset lookup, condition update; Planner Agent; tests — none started.

### Seneja Ramanayaka — Component B: Maintenance Management (FR-033–042, FR-077–080)
- FR-034: photograph attachment — needs real file-upload storage (currently a URL string field).
- FR-033: Flutter fault reporting (React side is done).
- FR-077–079: wire up actual email delivery infra (sending is stubbed).
- FR-080: build the Notification Centre panel — header bell is currently static.
- AC3: automated integration test for condemnation-on-Unserviceable-completion.
- FR-041: preventive scheduling exists via a background poller — worth a closer correctness pass/tests before calling it done.
- Maintenance Analysis Agent; test project — none started.

### Bhanuka Samarasinghe — Component C: Transfer & Disposal (FR-043–055)
- P6 precondition (agent workflow check) — last stubbed precondition, blocked on the agent subsystem existing.
- React: transfer/disposal queues + precondition checklist UI — backend is fully built and tested, but there is no frontend at all yet.
- Flutter: transfer request, scan-to-confirm receipt, condemnation flow.
- Budget Analysis Agent — tool endpoints exist (`/api/agent-tools/*`); the Python agent that calls them is not started.
- SRS Appendix B named-policy authorisation layer — deliberately deferred until after mobile work; revisit once Flutter lands.

### Program-wide (no single owner)
- Flutter mobile app: not started for any component — every FR marked Flutter-only above is blocked on this.
- No backend or frontend test project exists at all yet — each owner's "Tests" row above is ❌ for the same underlying reason.
