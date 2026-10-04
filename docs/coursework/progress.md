# Progress Tracker

Tracks what's actually built against the requirements in the [SRS](../srs/00-front-matter.md), grouped by the component ownership in [SRS §12](../srs/12-component-ownership.md). Each section lists items as Completed, In Progress, or Not Started. Tick an item only once it is actually in the repository, and update this file in the same PR that lands the work (SRS §18.2).

**Legend:** ✅ done and in the repo · 🟡 partially done · ❌ not started

## Cross-cutting (Identity, Access, Admin Shell)

**✅ Completed**

| Task | Notes |
|---|---|
| FR-001: ThunderID OIDC sign-in (PKCE) | |
| FR-002: Backend JWT validation (issuer + RS256 via JWKS) | |
| `GET /api/me` — resolve the caller's CoreGrid profile/role by `sub` | |
| FR-003: Resolve `OrganizationId` from the local user mirror | `RoleEnrichmentMiddleware` resolves it once per request onto the scoped `CurrentUserContext` (`Features/Shared/CurrentUser/`); no `organization_id` claim rewrite — every consumer (controllers, the audit interceptor, the FR-006 query-filter provider) reads the context instead of re-deriving it |
| FR-004: Create/refresh local user mirror on first request; audit role changes | Role itself is admin-driven only, never refreshed from the token |
| FR-005: Every endpoint declares an authorisation policy | Named policies (Appendix B) — see below |
| FR-006: Global `OrganizationId` query filter | `CoreGridDbContext.HasQueryFilter` on every org-scoped entity, including the four with no `OrganizationId` column of their own (`AssetAttributeDefinition`, `AssetAttributeValue`, `AgentExecutionStep`, `AgentApproval`), filtered through their required parent navigation instead |
| Named ASP.NET Core authorisation policies | Appendix B's literal policy names (`CanReadAssets`, `CanManageAssets`, …) — `Features/Shared/Auth/Policies.cs` + `CoreGridPolicyRequirement`/`CoreGridPolicyHandler`; a fail-closed fallback policy (NFR-10) denies any route with no attribute at all |
| NFR-16/AI-27: Rate limiting | Per-user+org policies on workflow initiation, report exports, photo uploads, `POST /api/setup/complete` |
| NFR-09: HSTS | Enabled outside Development |
| NFR-20: `GET /health` | Anonymous, per-dependency JSON (DB + ThunderID issuer reachability) |
| §5.4: Correlation id | `CorrelationIdMiddleware` — every response carries `X-Correlation-Id`; audit rows share the originating request's id |
| SEC-ID-09: Authorisation outcome logging | `AuthorizationOutcomeLoggingMiddleware` logs every 401/403 with subject, org, endpoint, timestamp — never the token |
| FR-007: Frontend hides/protects unpermitted routes | `RoleRoute` |
| FR-008: Sign-out clears state, revokes token, ends IdP session | |
| FR-009: Deactivated user denied even with a valid token | |
| First-Administrator provisioning via Setup | Creates ThunderID account + CoreGrid role |
| FR-013: Admin invites a user by email + role | |
| FR-014: Change a user's role/department, deactivate/reactivate | Guards against deactivating the org's last active Administrator |
| Self-service password recovery ("Forgot password?") | ThunderID's hosted RECOVERY flow, enabled by `scripts/thunderid/enable-password-recovery.sh`; every role, Administrators included. CoreGrid's `/forgot-password` hands off to `/gate/recovery`. Emails need ThunderID's SMTP configured, deferred to deployment — see `docs/setup/thunderid.md` step 8 |
| My Profile: change password | Every signed-in web user; same emailed-link recovery flow, no current-password form (Known Gaps in `docs/setup/thunderid.md`) |
| Administrator password reset for any user | `POST /api/users/{id}/reset-password` (`CanManageUsers`) → ThunderID `update-credentials`; Users & Roles → ⋯ → Reset password. Like user creation, it's an admin-entered password field in CoreGrid — a documented deviation from FR-001's "never present a password entry field"; CoreGrid still stores none |
| Staff department scoping | `Features/Shared/Scoping/DepartmentScope` — applied to the Assets/Maintenance/Transfers/Disposals list and detail endpoints and to fault reporting (Appendix B: "Staff are restricted to their own department by a service-layer filter"). **Staff only** — Inventory Officer reads organisation-wide per SRS §4.6 (previously also restricted, which hid inbound transfers from the destination officer who must confirm receipt) |
| Every read endpoint declares a policy | Transfer/disposal/dashboard-summary reads → `CanReadAssets`; notifications → `CanReadNotifications`; campaign reads → new `CanReadCampaigns` (Officer/Auditor/Administrator — Staff no longer read campaigns); `GET /api/users` adds Auditor (maintenance assignee filters) |
| Web permission map | `frontend/src/features/auth/lib/permissions.ts` + `usePermissions()` — one mirror of the backend grants that every page's action buttons read, instead of per-page `role ===` checks. Officer's "Confirm receipt" only shows for transfers into their own department (`/api/me` now returns `department_id`) |
| EF Core migrations + generated `db/schema.sql` export | |
| CI pipeline (build/test on push and PR) | `.github/workflows/ci.yml` has backend and frontend jobs. Backend restores/builds `backend.Tests` with warnings as errors, sets `TEST_DB_CONNECTION` to port 5433, applies EF migrations, and runs the real-Postgres tests. The sibling mobile workflow runs Flutter analysis/tests and a release APK build. |
| Mobile CI | `coregrid-mobile/.github/workflows/ci.yml` runs `flutter analyze`, `flutter test`, and `flutter build apk --release`; team status is passing. |
| Backend test project | `backend.Tests`, xUnit — InMemory suite plus real-Postgres append-only checks. 452 tests in 26 classes, 100% passing (2026-10-04 run); CI confirmed passing. |
| Frontend test project | Vitest + React Testing Library (115 tests in 23 files, 100% passing — 2026-10-04 run) |
| Multi-agent orchestration pipeline | Full Planner -> Maintenance -> Budget -> Policy pipeline is genuinely connected end-to-end for the first time in the project; new `BudgetAnalysis` jsonb column on `AgentWorkflows` table |
| Optional fallback LLM provider | Planner and Budget agents try Gemini, then `LlmFallback` (Groq `openai/gpt-oss-120b`), then their deterministic fallback; shared `ILlmClient`; timeouts fall through to the next provider. `LlmSettingsTests`, `BudgetAgentTests` |
| Agent subsystem structure | `Features/Agents/Services/` has one folder per agent (`Planner/`, `Maintenance/`, `Budget/`, `Policy/`) plus `Orchestration/` (`AgentWorkflowService`, `WorkflowPipeline`, `WorkflowRouting`) and `Llm/`; `Features/AgentTools/` gives each agent its own tool interface (`IPlannerTools`, `IMaintenanceTools`, `IBudgetTools`, `IPolicyTools`), so the §7.4 allow-list is enforced at compile time |
| Environment configuration in `backend/.env` | `DotEnvFile` loads the git-ignored `backend/.env`; `appsettings*.json` hold only logging; `backend/.env.example` lists every variable (local, Supabase, R2, Gemini, Groq) |

## Component A — Asset Registry & QR Identification (Jayashan Guruge)

**✅ Completed**

| Task | Notes |
|---|---|
| FR-016: Create asset categories | Full CRUD, incl. deactivate/reactivate |
| FR-017: Create asset types | Name, code, category, useful life, default maintenance interval |
| FR-018: Ordered custom attribute definitions per asset type | |
| FR-019: Attribute value validation on create/update | `AttributeValidationRuleEngine` — min/max, minLength/maxLength, maxDate |
| FR-020: Dynamic attribute-driven detail form | Fields render purely from the selected asset type's attribute definitions |
| FR-021: Register an asset | Type, name, department, location, acquisition date/cost, attributes |
| FR-022: Unique human-readable asset code | Org prefix + monotonic sequence, DB-constrained |
| FR-025: Manual asset-code entry | Resolves via `GET /api/assets/qr/{code}` |
| FR-026: Amend asset fields/attributes/department/location | Writes a `FIELD_AMENDMENT` history entry per change |
| FR-028: Search/filter/sort/pagination | Server-side; search also matches asset type/category name and dynamic attribute values |
| FR-029: Record condition | New/Good/Fair/Poor/Unserviceable |
| Database (`AssetCategories`, `AssetTypes`, `AssetAttributeDefinitions`, `AssetAttributeValues`, `Assets`, `AssetHistory`) | |
| React (asset list/detail/register/update, dynamic attribute forms, category/type/attribute config, searchable pickers) | |
| Planner Agent | Rejects out-of-scope objectives, produces a typed execution plan; wired into workflow creation |
| FR-023: QR label | QR image is generated and shown in-app; printable-label download is implemented in the frontend. |
| FR-027: Immutable, ordered per-asset lifecycle history | Asset history is append-only and ordered; verification, maintenance, transfer, disposal, and agent entries are written by their owning services. |
| FR-032: Assets exit only via disposal | No asset delete endpoint exists; the Component C disposal flow provides the terminal exit path. |
| Planner Agent: in-process implementation | `PlannerAgent` is registered under `backend/Features/Agents/Services/Planner/` and is wired into workflow creation. |
| FR-030: Computed residual value | Server-side residual value, frontend straight-line preview, and depreciation edge-case tests are present. |
| Tests | `backend.Tests/AssetServiceTests.cs`, frontend depreciation tests, and mobile asset tests are present.  |

## Component B — Maintenance Management (Seneja Ramanayaka)

**✅ Completed**

| Task | Notes |
|---|---|
| FR-033: Fault reporting | Searchable asset picker |
| FR-034: Photograph attachment | Uploads to Cloudflare R2 as a private object; a fresh, short-lived signed URL is minted only on an authorized read of the record |
| FR-035: Direct maintenance entry | Officer creates a record directly, sets type/priority |
| FR-036: Approval & assignment | Assigns an officer, records estimated cost |
| FR-037: Defined status sequence | `REQUESTED → APPROVED → IN_PROGRESS → COMPLETED/CANCELLED`, guarded transitions |
| FR-038: Complete maintenance | Records actual cost, work performed, completion date, resulting condition |
| FR-039: Asset `UNDER_MAINTENANCE` lock | Blocks transfer/disposal while active |
| FR-040: Cumulative recalculations | Cumulative cost, repair count, last repair date recomputed atomically on completion |
| FR-041: Preventive scheduling | Scheduled by a background service when an asset type's maintenance interval has elapsed |
| FR-042: List/filter/sort/pagination | Filters: status, priority, type, asset, department, assignee, date range; server-side sort + pagination |
| FR-080: Notification Centre | Backend `Notifications` feature + header bell panel with live unread count |
| FR-084: Reports > Maintenance tab | Filters, stats, by-asset-type breakdown, PDF/CSV export |
| BR1: Cost-variance tolerance | Enforced against organisation policy on completion |
| BR2: Resulting condition Unserviceable | Sets asset to `CONDEMNED` |
| BR3: Atomic transaction | Completion is a single DB transaction |
| AC1: Re-completion block (409) | |
| AC2: Cost aggregation correctness | |
| AC3: Condemnation verification | |
| Maintenance Analysis Agent | Runs automatically after Planner on every new workflow (repair count, MTBF, cost trend, 12-month projection); manual re-run action; rendered on the Workflows page |
| Backend test coverage | Maintenance service, preventive scheduler, failure-statistics engine, and the Maintenance Analysis Agent node |
| Copy cleanup | No rendered `(FR-0XX ...)` references or em-dashes remain in `MaintenancePage.tsx`, `CreateMaintenanceModal.tsx`, `ReportFaultModal.tsx` (the create/report-fault pages became modals); em-dashes left only in code comments |
| Hardcoded colors / shared components sweep | No hex colours or inline styles left in `features/maintenance/`; error copy, field stacks, filter widths and the detail page's key-value grid use shared classes in `styles/index.scss` |

**❌ Not Started**

| Task | Notes |
|---|---|
| FR-077–079: Email/SMS delivery | Deliberately out of scope for this phase — no email code exists |
| AC4: Notification failure isolation | Blocked on FR-077–079 |

## Component C — Transfer & Disposal (Nipuna Bhanuka / Bhanuka)

**✅ Completed**

| Task | Notes |
|---|---|
| FR-044/045/046: Transfer state machine | |
| SRS §9.4: Reject transfer / reject disposal | `POST /api/transfers/{id}/reject`, `POST /api/disposals/{id}/reject` — `CanApproveTransfer`/`CanApproveDisposal` (Administrator only), same as their approve counterpart. Transfer reject releases the asset back to `ACTIVE`; disposal reject reverts it to `CONDEMNED` so a fresh request can be raised |
| FR-047: Transfer history endpoint | |
| FR-049: Condemn asset | |
| FR-050: Submit disposal | |
| FR-051/052: Disposal precondition evaluation (P1–P6) | All six preconditions plus separation-of-duties, fully implemented |
| FR-053: Disposal revision | |
| FR-054/055: Disposal approval + terminal state | |
| Database (`AssetTransfers`, `DisposalRequests`) | Real FK constraints |
| React (Administrator, Inventory Officer, Auditor screens) | Live precondition checklist, approve/reject/request-revision, initiate transfer, confirm receipt, condemn, submit disposal. **Administrator now has full parity with Inventory Officer's own operational actions** (initiate transfer, confirm receipt, condemn, submit disposal), not just the approval half — `InitiateTransferModal`/`CondemnAssetModal`/`SubmitDisposalModal` extracted to `features/transfers/components/`. The three per-role pages (`TransfersPage`/`InventoryTransfersPage`/`AuditorTransfersPage`) are now one `TransfersPage` that takes the route's role; `lib/capabilities.ts` decides which actions and audit-trail columns show, and the tables, paging wrapper, precondition checklist, revision and compliance-detail modals are shared components |
| FR-084: Reports > Disposal tab | Real — `DisposalReportPanel.tsx`, same "fetch every page and aggregate client-side" pattern as Maintenance/Inventory's own report panels; no dedicated report backend endpoint needed. Filters: status, method, date range. Stats: disposals in scope, total proceeds, average approval time. PDF/CSV export |
| Agent tool endpoints for Budget Analysis Agent | `get_asset_financials`, `get_department_budget_summary`, `compute_depreciation` |
| Budget Analysis Agent | Migrated to the in-process C# `BudgetAgent` (`BudgetTriage` + `BudgetAssessmentValidator`), with deterministic tools, configurable model access, scope guard, fallback, Node 3 orchestration, graceful degradation, and independent re-run endpoint. |
| Tests | Transfer, disposal, precondition, Budget Agent, authorization, and frontend role-boundary test files are present. The repository's CI is passing; exact suite counts should be taken from the CI run rather than this hand-maintained tracker. |
| Hardcoded colors / shared components sweep | No hex colours or inline styles left in `features/transfers/`; actions columns, button rows and pagination spacing use shared classes |

## Component D — Audit & Compliance + Org Configuration + User Administration (Hasitha Erandika)

**✅ Completed**

| Task | Notes |
|---|---|
| Organisation creation (Setup) | |
| FR-010/011/012: Department/Location CRUD | Amend + activate/deactivate; guards against deactivating a department/location a non-disposed asset still references |
| FR-013: User administration (invite by role) | |
| FR-014: User role/department change, deactivation | |
| FR-015: Organisation policy parameters | At most one policy per asset type, including the org-wide default |
| FR-056/057: Verification campaigns, task generation | Officer assignment synchronous on creation |
| FR-059: Officer scan-to-verify | |
| FR-060/061: Automatic + manual discrepancy raising | |
| FR-062: Discrepancy resolution | |
| FR-063/064: Append-only audit log | Generic `AuditSaveChangesInterceptor` covers every entity automatically |
| FR-065/084/085: Campaign report + PDF/CSV export | Single-campaign report plus the org-wide Reports > Audit tab |
| FR-081/082/086: Dashboard indicators + visualisations | Org-wide for Administrator/Auditor/Inventory Officer, department-scoped for Staff |
| Reports > Audit tab: real server-side pagination | Discrepancy list is paginated server-side; export still returns every row |
| Reports page: Audit tab hidden from Inventory Officer | Matches the backend's own Auditor/Administrator-only authorisation |
| React (org structure/users/policy admin, audit dashboard, campaigns, discrepancy resolution, Reports > Asset Inventory) | |
| Users & Roles page: search + pagination | `GET /api/users` supports `search`/`page`/`pageSize`; picker dropdowns elsewhere unaffected |
| Policy Compliance Agent + human-approval checkpoint | Deterministic rule engine, node-4 recommendation step, approval workflow — no LLM call, by team decision. Fixed incorrect AgentExecutionStep sequence (was hardcoded 3, now correctly 4). Now consumes real FinancialAssessmentResultDto from Node 3 instead of always evaluating null financial facts. |
| CI pipeline ownership | Backend/frontend jobs |
| Tests (append-only, discrepancy resolution, authorisation matrix) | |
| Hardcoded colors / shared components sweep | No hex colours left in `features/workflows/`, `features/users/`, `features/reports/`, `features/dashboard/`: rule-outcome icons and the chart palette (series colour, condition ramp, axis/grid/label colours) are SCSS tokens in `styles/index.scss`, and the duplicated `CONDITION_COLORS` array is gone. The one inline style left is `LineChart`'s tooltip position, which is computed per hover |

**🟡 In Progress**

| Task | Notes |
|---|---|
| Policy Compliance Agent: full orchestration | Node 4 sequenced after nodes 1/2/3 (full 4-node pipeline now connected); executing an approved action against the underlying business record is stubbed |
| Append-only enforcement | Restricted DB role + grants exist and are proven correct by tests; the app's runtime connection still needs to be split from the migration-owner one to take effect |

**❌ Not Started**

| Task | Notes |
|---|---|
| MSW for frontend tests | Considered, not adopted |

## Program-wide

**✅ Completed**

| Task | Notes |
|---|---|
| Sidebar nav grouping for Inventory Officer / Auditor | `InventoryLayout.tsx` and `AuditLayout.tsx` use `navGroups` with the same Assets / Operations / Compliance sections as `AdminLayout.tsx` (no Administration section, since neither role has users/settings access) |
| One-command local setup | `setup.sh` / `make setup`: prerequisites, dependencies, `.env` files, Docker start-up that never re-runs ThunderID's one-shot setup, migrations, ThunderID credential check, one test account per role (`*@coregrid.test`, local only). `make infra-up` fixed to start existing containers only |
| ThunderID reference configuration | `infra/thunderid/coregrid.yaml`: sanitised export of the working configuration, for checking a console setup (not auto-loaded) |
| Database export tooling | `make db-export` (`scripts/db/export-migrations.sh`); missing `0016`/`0017` SQL exports generated; `schema.sql` regenerated in the documented non-idempotent form |
| Sass deprecation fix | `styles/index.scss` uses `sass:list` (`list.length`, `list.nth`) instead of the deprecated globals |
| Shared inline-notification width | `.cds--inline-notification { max-inline-size: 100% }` in `styles/index.scss` replaces the per-call `style={{ maxWidth: "100%" }}`; removed from Components B, C and D. `features/assets/`, `features/audit/`, `features/settings/` etc. still carry the now-redundant inline prop |

**🟡 In Progress**

| Task | Notes |
|---|---|
| Performance test run | Suite is in place (`scripts/perf/`, `make perf`: dataset seed, k6 50-VU 70:30 load test, agent latency, slow queries, generated results table) and validated with stubbed k6 output; the measured run against the deployed API is still to be done |
