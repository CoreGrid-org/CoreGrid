# Progress Tracker

Tracks what's actually built, against the ownership in [SRS §12](srs/12-individual-contribution-and-work-allocation.md) and [SRS §18](srs/18-team-roster-and-work-allocation.md). Each section lists items as Completed, In Progress, or Not Started — tick an item only once it's actually in the repo, and update this file in the same PR that lands the work it describes.

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
| Staff department scoping | `Features/Shared/Scoping/DepartmentScope` — applied to the Assets/Maintenance/Transfers/Disposals list and detail endpoints (Appendix B: "Staff are restricted to their own department by a service-layer filter") |
| EF Core migrations + generated `db/schema.sql` export | |
| CI pipeline (build/test on push and PR) | `.github/workflows/ci.yml` — backend and frontend jobs only; no secret-scanning job |
| Backend test project | `backend.Tests`, xUnit — InMemory suite + a real-Postgres suite for append-only checks; compiles and runs in CI (376 backend tests, 100% passing) |
| Frontend test project | Vitest + React Testing Library (51 frontend tests, 100% passing) |
| Multi-agent orchestration pipeline | Full Planner -> Maintenance -> Budget -> Policy pipeline is genuinely connected end-to-end for the first time in the project; new `BudgetAnalysis` jsonb column on `AgentWorkflows` table |

**❌ Not Started**

| Task | Notes |
|---|---|
| CI: real-Postgres test suite (`AppendOnlyTests`) isn't reliably exercised in CI | The CI Postgres service isn't migrated before `dotnet test` runs, and the suite's own default connection string (`localhost:5433`) doesn't match the service's mapped port (`5432`) unless `TEST_DB_CONNECTION` is set — neither is currently wired up |

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

**🟡 In Progress**

| Task | Notes |
|---|---|
| FR-023: QR label | Real QR image generated and shown in-app; no printable-label download yet |
| FR-027: Immutable, ordered per-asset lifecycle history | `STATUS_CHANGE`/`FIELD_AMENDMENT` entries done; verification/maintenance/transfer/disposal/agent-recommendation entries are written by other components |
| FR-032: Assets exit only via disposal | No delete endpoint exists (satisfies this on its own); full confirmation pending Component C's disposal flow |
| Planner Agent: in-process migration | Currently a standalone Python/FastAPI service called over HTTP; target architecture is an in-process node |

**❌ Not Started**

| Task | Notes |
|---|---|
| FR-030: Computed residual value | Currently a free-entry client field, not derived server-side from acquisition cost/date + useful life |
| Tests | No `AssetServiceTests.cs` yet |

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

**❌ Not Started**

| Task | Notes |
|---|---|
| FR-077–079: Email/SMS delivery | Deliberately out of scope for this phase — no email code exists |
| AC4: Notification failure isolation | Blocked on FR-077–079 |
| Test coverage: Notifications feature, photo upload's storage call | |
| Frontend tests | No test file exists for any Component B page yet |
| Copy cleanup | Remove rendered `(FR-0XX ...)` references and em-dashes from `CreateMaintenancePage.tsx`, `ReportFaultPage.tsx`, `MaintenancePage.tsx` |
| Hardcoded colors / shared components sweep | Own files not yet audited for inline styles / duplicated logic |

## Component C — Transfer & Disposal (Bhanuka Samarasinghe)

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
| Budget Analysis Agent | Migrated from standalone Python/LangGraph to in-process C# service (BudgetAgentService.cs), following team-wide architecture decision and matching PlannerAgentService.cs's blueprint (deterministic tools -> LLM call -> deterministic fallback). Uses configurable OpenAI-compatible endpoint (Budget:Endpoint/Model/ApiKey config), defaulting to Gemini's OpenAI-compatible endpoint for cost consistency. BudgetScopeGuard provides structural validation and a real deterministic fallback using OrganizationPolicy's actual RepairToReplaceCostThreshold when configured. 19 new unit tests (188 total repo-wide, 0 failures on full unfiltered run including Postgres-backed AppendOnlyTests). Original Python implementation (agent-service/) preserved untouched pending final decommission decision. Wired into the live multi-agent orchestration pipeline as Node 3 (AgentWorkflowService.cs), sequenced correctly after Maintenance Analysis. Graceful degradation on failure (does not hard-fail the workflow). New POST /api/agent-workflows/{id}/run-budget-agent endpoint for independent re-runs. |
| Tests | 105 Component C-specific unit tests (86 transfer/disposal/tools + 19 budget agent tests across BudgetScopeGuardTests and BudgetAgentServiceTests), plus reject×2/amend×3/verify×6 added for the SRS §9 opt-in items. Repo-wide suite: 376 backend tests, 51 frontend tests, both currently 100% passing |

**❌ Not Started**

| Task | Notes |
|---|---|
| Hardcoded colors / shared components sweep | Own files not yet audited |

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
| FR-081/082/086: Dashboard indicators + visualisations | Org-wide for Administrator/Auditor, department-scoped for Staff/Inventory Officer |
| Reports > Audit tab: real server-side pagination | Discrepancy list is paginated server-side; export still returns every row |
| Reports page: Audit tab hidden from Inventory Officer | Matches the backend's own Auditor/Administrator-only authorisation |
| React (org structure/users/policy admin, audit dashboard, campaigns, discrepancy resolution, Reports > Asset Inventory) | |
| Users & Roles page: search + pagination | `GET /api/users` supports `search`/`page`/`pageSize`; picker dropdowns elsewhere unaffected |
| Policy Compliance Agent + human-approval checkpoint | Deterministic rule engine, node-4 recommendation step, approval workflow — no LLM call, by team decision. Fixed incorrect AgentExecutionStep sequence (was hardcoded 3, now correctly 4). Now consumes real FinancialAssessmentResultDto from Node 3 instead of always evaluating null financial facts. |
| CI pipeline ownership | Backend/frontend jobs |
| Tests (append-only, discrepancy resolution, authorisation matrix) | |

**🟡 In Progress**

| Task | Notes |
|---|---|
| Policy Compliance Agent: full orchestration | Node 4 sequenced after nodes 1/2/3 (full 4-node pipeline now connected); executing an approved action against the underlying business record is stubbed |
| Append-only enforcement | Restricted DB role + grants exist and are proven correct by tests; the app's runtime connection still needs to be split from the migration-owner one to take effect |

**❌ Not Started**

| Task | Notes |
|---|---|
| Hardcoded colors / shared components sweep | `WorkflowsPage.tsx`, `ReportsPage.tsx`, `UsersPage.tsx`, dashboard pages still have inline styles |
| MSW for frontend tests | Considered, not adopted |

## Program-wide

**❌ Not Started**

| Task | Notes |
|---|---|
| Sidebar nav grouping for Inventory Officer / Auditor | Admin layout already grouped into sections; Inventory Officer/Auditor layouts left as flat lists, each owner's call |
