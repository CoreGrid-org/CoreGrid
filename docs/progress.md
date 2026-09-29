# Progress Tracker

Tracks what's actually built, against the ownership in [SRS §12](srs/12-individual-contribution-and-work-allocation.md) and [SRS §18](srs/18-team-roster-and-work-allocation.md). Each section lists items as Completed, In Progress, or Not Started — tick an item only once it's actually in the repo, and update this file in the same PR that lands the work it describes.

**Legend:** ✅ done and in the repo · 🟡 partially done · ❌ not started

**Documentation audit — 2026-09-28.** This tracker has been reconciled with the current repository tree and
the supplied web/mobile PR history. Pull-request titles and member attribution are recorded in
[`docs/contribution-history.md`](contribution-history.md). A closed PR is treated as delivery evidence only
when its implementation is present in the checked tree; mobile FR-049 is the current exception and remains
unverified because no condemnation screen, route, API call, or test is present in the checked mobile tree.

## Individual allocation evidence — remaining work

The implementation sections below show what is built. This checklist tracks the additional evidence required
to fully satisfy the individual contribution requirements in [SRS §18.3–§18.6](srs/18-team-roster-and-work-allocation.md).
An item is complete only when the artefact is linked or attached, not merely when the feature exists in code.

### Student 1 — Jayashan Guruge — Component A

**Current assessment:** 🟡 Implementation mostly present; individual evidence package incomplete.

- [x] Add the Flutter scanner widget test covering camera success, permission refusal, unknown code, offline
  recovery, and manual-entry fallback.
- [ ] Link the Component A issues and reviewed PRs for FR-016–FR-032, including reviewer names and dates.
- [x] Attach the final React asset-form/component test output and the mobile asset test output.
- [x] Link the Component A README/design note and ADR-006 input for attribute-value storage.
- [x] Complete Jayashan's Appendix E AI log with tool/model, dates, accepted/rejected output, and verification.
- [x] Attach a device or emulator record for QR scan, asset lookup, condition update, and verification.

### Student 2 — Seneja Ramanayaka — Component B

**Current assessment:** 🟡 Implementation mostly present; notification and contribution evidence incomplete.

- [ ] Add or link the notification failure-isolation test proving delivery failure does not roll back maintenance
  completion.
- [ ] Add an explicit storage-provider integration test or recorded verification for maintenance-photo upload and
  retrieval through the configured object storage.
- [ ] Add the Component B notification-provider design note referenced by SRS §18.4.
- [ ] Link the maintenance/notification issues and reviewed PRs for FR-033–FR-042 and FR-077–FR-080, including
  reviewer names and dates.
- [ ] Attach backend, React, and mobile test output for maintenance, notification, photo, and status-transition
  flows.
- [ ] Complete Seneja's Appendix E AI log with tool/model, dates, accepted/rejected output, and verification.

### Student 3 — Nipuna Bhanuka (Bhanuka) — Component C

**Current assessment:** 🟡 Web/API and Budget Agent implementation present; mobile FR-049 and formal evidence incomplete.

- [ ] Reconcile the reported mobile FR-049 PR with the checked `coregrid-mobile` branch. Restore or implement the
  condemnation screen, route, API call, authorization gate, evidence capture, and tests.
- [ ] Add the dedicated concurrency-conflict test for DR-11 using PostgreSQL/EF Core optimistic concurrency.
- [ ] Link negative tests for all disposal preconditions P1–P6 and the separation-of-duties test to FR-051.
- [ ] Add a documented merge-conflict-resolution record for Component C integration work.
- [ ] Add/link Flutter tests for transfer creation, asset identity matching, receipt confirmation, and FR-049.
- [ ] Add the Component C concurrency-control design note and Budget Agent evidence.
- [ ] Link the transfer/disposal issues and reviewed PRs for FR-043–FR-055, including reviewer names and dates.
- [ ] Complete Nipuna's Appendix E AI log with tool/model, dates, accepted/rejected output, and verification.

### Student 4 — Hasitha Erandika — Component D and group integration

**Current assessment:** 🟡 Implementation and documentation coordination mostly present; release/evidence verification incomplete.

- [x] CI is passing for the main CoreGrid repository and the sibling mobile repository, including backend,
  frontend, PostgreSQL integration, Flutter analysis/tests, and mobile APK build. Attach the final run links and
  secret-scanning record to the submission evidence package.
- [ ] Run and record the golden cases required by SRS §13.4, especially approval authorisation, rejection,
  checkpoint resume, and disposal execution.
- [ ] Attach live end-to-end evidence for the four-agent workflow and the human-approval checkpoint.
- [ ] Link Hasitha's reviewed PRs/issues for Component D, authentication, CI, documentation, and integration work.
- [ ] Confirm the authorisation matrix, append-only database checks, and organisation-isolation tests with a
  reproducible test command and output.
- [ ] Attach the mobile authentication, role-gate, dashboard, verification, workflow, and device-run evidence.
- [ ] Complete the consolidated README/report, demonstration script, submission links, and final Appendix E AI
  record for all members.

### Shared completion gate

- [ ] Replace every placeholder student ID and email in the SRS roster.
- [ ] Confirm every PR has a real GitHub link, reviewer, review date, requirement IDs, and final merge status.
- [ ] Reconcile the supplied PR list with local merge commits using the GitHub PR pages as the authority.
- [ ] Store final backend, frontend, and mobile test outputs under the submission evidence location.
- [ ] Update this tracker and SRS §18 only after the evidence links have been checked by the group leader.

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
| Backend test project | `backend.Tests`, xUnit — InMemory suite plus real-Postgres append-only checks. CI is confirmed passing; the current repository contains 25 test classes. |
| Frontend test project | Vitest + React Testing Library (86 frontend tests, 100% passing) |
| Multi-agent orchestration pipeline | Full Planner -> Maintenance -> Budget -> Policy pipeline is genuinely connected end-to-end for the first time in the project; new `BudgetAnalysis` jsonb column on `AgentWorkflows` table |

**🟡 Evidence / follow-up**

| Task | Notes |
|---|---|
| CI and submission evidence | CI passes. Attach the main and mobile run links, APK artifact link, and the secret-scanning result to the final submission package. |

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
| Planner Agent: in-process implementation | `PlannerAgentService` is registered under `backend/Features/Agents/` and is wired into workflow creation. |
| FR-030: Computed residual value | Server-side residual value, frontend straight-line preview, and depreciation edge-case tests are present. |
| Tests | `backend.Tests/AssetServiceTests.cs`, frontend depreciation tests, and mobile asset tests are present. Full execution evidence remains in the shared evidence checklist. |

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
| Budget Analysis Agent | Migrated to the in-process C# `BudgetAgentService`, with deterministic tools, configurable model access, scope guard, fallback, Node 3 orchestration, graceful degradation, and independent re-run endpoint. |
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
| Shared inline-notification width | `.cds--inline-notification { max-inline-size: 100% }` in `styles/index.scss` replaces the per-call `style={{ maxWidth: "100%" }}`; removed from Components B, C and D. `features/assets/`, `features/audit/`, `features/settings/` etc. still carry the now-redundant inline prop |
