# Individual Report — Hasitha Erandika (Student 4, Group Leader)

| Item | Detail |
|---|---|
| Student ID | ⟦ID⟧ |
| GitHub | `HasithaErandika` |
| Role | **Group Leader** (Team Roster §18.2, §18.6) |
| Primary component | **D — Audit & Compliance, Organisation Configuration, User Administration** |
| Requirements owned | FR-001 – FR-015 (identity, access, configuration, users), FR-056 – FR-066 (verification, discrepancies, audit), FR-081 – FR-086 (dashboards, reports) |
| Agent owned | **Policy Compliance Agent** (Node 4), the **agent orchestrator** and the **human-approval checkpoint** |
| Business-specific operations | `PATCH /api/discrepancies/{id}/resolve` (FR-062) · `PATCH /api/agent-workflows/{id}/decide` (FR-071–075) |
| Golden cases owned (Team Roster §18.7) | GC-01, GC-03, GC-04, GC-08, GC-10, GC-11, GC-12 |
| Git footprint | CoreGrid: 77 commits, ≈ 68.5k insertions (2026-08-08 → 2026-09-28) · coregrid-mobile: 22 commits, ≈ 16.8k insertions |

## 1. Contribution Statement

As Group Leader I set up the CoreGrid repositories and engineering foundations on 2026-08-08 and remained the main integrator until submission. My work falls into four areas.

1. **Component D.** Organisation setup; departments, locations and users (provisioned through ThunderID SCIM); organisation policy thresholds; verification campaigns with automatic task generation; scan-to-verify; automatic and manual discrepancy raising and resolution; the append-only audit log; campaign and audit reports with PDF/CSV export; and the role-aware dashboards.
2. **The agentic core.** I built the workflow orchestrator (`AgentWorkflowService`), the persisted workflow model (`AgentWorkflows`, `AgentExecutionSteps`, `AgentApprovals`), the deterministic Policy Compliance Agent and rule engine, and the Administrator approval checkpoint that every other agent's output flows into.
3. **The cross-cutting platform every component depends on.**
   - ThunderID OIDC sign-in for web and mobile.
   - JWT validation and per-request user resolution.
   - Named authorisation policies with a fail-closed fallback, plus department scoping.
   - The EF Core organisation query filter and the generic audit interceptor.
   - Correlation ids, the uniform error envelope, health checks, rate limiting and security headers.
4. **Leadership and delivery.**
   - Repository structure, CI for both repositories, and Docker images.
   - Integration merges of teammates' branches, and the backend modularisation refactor.
   - The SRS, ADR set, setup guides and mobile documentation, and the consolidated report.
   - Evaluation deployment on Azure, Vercel and GitHub Pages.

## 2. Owned Work (Team Roster §18.6)

### 2.1 Backend (ASP.NET Core)

**Component D controllers and endpoints**

| Controller | Endpoints |
|---|---|
| `SetupController` | `GET /api/setup/status`, `POST /api/setup/complete`: creates the organisation, the first Administrator in ThunderID and the CoreGrid user mirror; rate-limited |
| `MeController` | `GET /api/me`: caller profile, role and department |
| `UsersController` | `GET` (search + paging), `POST` invite, `PATCH {id}` role/department, `PATCH {id}/deactivate`, `PATCH {id}/activate`, `POST {id}/reset-password` (ThunderID `update-credentials`) |
| `DepartmentsController`, `LocationsController` | List, create, update, activate, deactivate |
| `OrganizationPoliciesController` | List, get, create, update; at most one policy per asset type plus an org-wide default (FR-015) |
| `VerificationCampaignsController` | CRUD; `GET {id}/report`; `GET {id}/report/export` (PDF/CSV) |
| `VerificationTasksController` | List; `PATCH {id}/complete` |
| `DiscrepanciesController` | `GET /api/discrepancies`; verification photo upload; manual raise; `PATCH {id}/resolve` |
| `AuditLogController`, `AuditReportController` | Paged audit log; Audit report + export |
| `DashboardController` | `GET summary`, `GET charts`: org-wide for Administrator/Auditor/Officer, department-scoped for Staff |
| `AgentWorkflowsController` | List, get, `execution-summary`, create, `evaluate`, `run-policy-agent`, `decide` (with teammates' `run-maintenance-agent` and `run-budget-agent`) |

**Business rules I implemented**
- The organisation's last active Administrator cannot be deactivated.
- A department or location still referenced by a non-disposed asset cannot be deactivated.
- A campaign generates officer verification tasks synchronously when it is created.
- A mismatch in presence, location or condition during scan-to-verify raises a discrepancy automatically.
- Discrepancy resolution covers every resolution type, with an optional register correction.
- Workflow decisions are Administrator-only, require a reason and store a workflow snapshot; revisions are capped at 2.
- Deactivated users are denied even when they present a valid token (FR-009).

**Cross-cutting platform (`Features/Shared/`, `Features/Identity/`, `Data/`)**

| Mechanism | Purpose |
|---|---|
| JWT bearer + JWKS validation | FR-002 |
| `RoleEnrichmentMiddleware` + `CurrentUserContext` / `ICurrentUser` | Resolves token `sub` → `Users.ExternalSubjectId` once per request; auto-provisions the user mirror (FR-003, FR-004) |
| `CoreGridControllerBase.GetCurrentUserAsync()` | Every controller's single entry point to the caller |
| `Policies`, `CoreGridPolicyRequirement`/`Handler`, fallback policy | Appendix B named policies; any endpoint without one is denied (FR-005, NFR-10) |
| `DepartmentScope` | Staff limited to their own department on the list and detail endpoints for assets, maintenance, transfers and disposals |
| `HasQueryFilter` on every organisation-scoped entity | FR-006; child entities filtered through their parent navigation |
| `AuditSaveChangesInterceptor` | FR-063: every entity mutation logged generically |
| `CorrelationIdMiddleware`, `AuthorizationOutcomeLoggingMiddleware`, `SecurityHeadersMiddleware` | Traceability, 401/403 logging without tokens, response hardening |
| `ApiExceptionFilter`, `ErrorEnvelope`, `InvalidModelStateResponseFactory` | Uniform 400/403/404/409/422/503 responses |
| `/health` checks | Database + ThunderID issuer reachability (NFR-20) |
| `RateLimiting` | Per-user+org policies (NFR-16, AI-27) |
| `ThunderIdIdentityDirectory` (`IIdentityDirectory`) | SCIM create/update/deactivate, password reset |

**Backend modularisation.** In commits `3fe85bd` and `4dda6bb` I restructured the backend into one `Features/<Component>/` folder per SRS owner, with `Controllers/`, `Services/` and `DTOs/` and a per-feature `*Module.cs` for DI registration, and swept out constants and dead code. The structure is documented in `CONTRIBUTING.md`.

### 2.2 Database (PostgreSQL / EF Core)

| Item | Detail |
|---|---|
| Entities owned | `Organizations`, `Users`, `Departments`, `Locations`, `OrganizationPolicies`, `VerificationCampaigns`, `VerificationTasks`, `Discrepancies`, `AuditLogEntries`, `AgentWorkflows`, `AgentExecutionSteps`, `AgentApprovals` |
| Migrations authored | `InitialCreate`, `RemoveOrganizationExternalId`, `AddAuditLog`, `AddVerificationAndDiscrepancies`, `AddAgentWorkflows`, `AllowAdHocDiscrepancies` |
| Organisation isolation | Global query filter, owned and documented centrally (DR-04), proven by `QueryFilterTests` |
| Append-only | A restricted DB role with `INSERT`/`SELECT`-only grants on `AuditLogEntries` and `AssetHistory`, proven against real PostgreSQL by `AppendOnlyTests` (DR-12) |
| Agent state schema | `jsonb` columns for the plan and per-agent outputs; relational step and approval tables (ADR-006) |
| Schema export | The `backend/db/schema.sql` + numbered `backend/db/migrations/NNNN_*.sql` export process (`make db-schema`) |

### 2.3 React

Approximate insertions per feature folder, from `git log --numstat`:

| Feature | Work |
|---|---|
| Foundations | Vite/React/TypeScript project, ThunderID sign-in, `RoleRoute`, role layouts with grouped sidebar navigation, `permissions.ts` / `usePermissions()`, shared components and SCSS tokens, Carbon theming |
| `setup`, `auth` | First-run Setup wizard; sign-in, sign-out, Access-restricted page; forgot-password and change-password via ThunderID recovery |
| `settings` (≈ 3.1k) | Departments, locations, organisation policy parameters |
| `users` (≈ 1.1k) | Users & Roles: search, paging, invite, role/department change, deactivate, reset password |
| `audit` (≈ 3.1k) | Campaigns, verification tasks, discrepancies and resolution, audit log |
| `reports` (≈ 2.3k) | Reports page; Audit tab with server-side pagination; Disposal report tab; PDF/CSV export |
| `dashboard` (≈ 1.6k) | Administrator/Auditor/Officer dashboards, KPI tiles, three required charts (FR-082) |
| `workflows` (≈ 1.7k) | Workflows page: per-agent outputs, execution summary, Approve / Reject / Request revision with mandatory reason |
| Integration | Maintenance (≈ 1.0k), transfers (≈ 1.8k) and assets (≈ 1.1k): wiring, shared-component refactors, UI fixes and role-permission alignment on teammates' features |

### 2.4 Flutter

| Area | Work |
|---|---|
| Project foundation | `e6ab745` Flutter core init: Riverpod, `go_router`, Dio `ApiClient`, theme, folder conventions (ADR-004) |
| Authentication | `flutter_appauth` OIDC + PKCE with custom-scheme redirect, `flutter_secure_storage` token storage, roles scope, `/api/me` resolution with readable failure reasons, local TLS handling for development, password recovery |
| App shell | Role-gated router redirect, bottom navigation shell, onboarding, Access-restricted screen, account screen, org-config providers |
| Dashboards | Officer and Staff dashboards, find-asset card, greeting header |
| Verification (FR-058, FR-059) | Campaign list/detail, task list ordered by due date, scan-to-verify flow, raise discrepancy |
| Workflows | Workflow list, initiate evaluation (start of the cross-platform flow), workflow detail with live status and decision |
| Shared UI kit | `state_views` (loading/empty/error), `surfaces`, `status_pill`, `photo_evidence_card`, formatters |
| Delivery | Mobile CI (`flutter analyze`, `flutter test`, release APK on `main`); UI revamp; integration of teammates' transfer and notification branches |

### 2.5 Agentic AI — Orchestrator, Policy Compliance Agent and approval checkpoint

| Aspect | Detail |
|---|---|
| Orchestrator | `AgentWorkflowService`: creates the workflow, runs Planner → Maintenance → Budget → Policy, records an `AgentExecutionStep` per node, enforces one in-flight workflow per asset (409), and turns any node failure into `FAILED_SAFE` with a recorded reason |
| Policy agent responsibility | Apply organisation policy to the combined evidence, produce the final recommendation, and decide whether human approval is required |
| Input | Workflow + Node 3 `FinancialAssessmentResultDto` (degrades gracefully if absent) |
| Output | Rule results (PASS/FAIL per rule), recommendation, high-impact flag; workflow → `COMPLETED_ADVISORY`, revision, or `AWAITING_APPROVAL`; step sequence 4 |
| Tool permissions | `get_asset_compliance_state`, `get_organization_policies` (read-only) |
| Rule engine | `PolicyRuleEngine` + `AssetActionRecommendationEngine`: repair-to-replace cost ratio, minimum service life, condition, failure frequency, valuation validity, confidence floor |
| Why no LLM | A compliance gate guarding a high-impact action must be deterministic, reproducible and auditable. Rules decide; the model only advises upstream (SRS §7.3) |
| Approval checkpoint | `PATCH /api/agent-workflows/{id}/decide`: `CanApproveWorkflow` (Administrator only); APPROVE / REJECT / REVISE with mandatory reason; full workflow snapshot stored in `AgentApprovals`; audited by the interceptor |
| Execution gate | Approved disposal recommendations are executed through Component C's disposal approval, whose P6 precondition requires the approved workflow |

### 2.6 Tests

| Suite | Authored |
|---|---|
| Backend (19 classes) | `AuthorizationMatrixTests` (15, all four roles across the endpoint matrix through `WebApplicationFactory`), `PolicyRuleEngineTests` (17), `DepartmentScopeTests` (9), `DiscrepancyResolutionServiceTests` (8), `AgentWorkflowServiceTests` (8), `RecordRequestValidationTests` (8), `AssetActionRecommendationEngineTests` (7), `PagingTests` (7), `AppendOnlyTests` (5, real PostgreSQL), `ValidationAndErrorEnvelopeTests` (5), `PhotoKeysTests` (5), `QueryFilterTests` (4), `LlmSettingsTests` (4), `UserMirrorProvisioningTests` (3), `AuditDateRangeValidationTests` (3), `CampaignReportServiceTests` (2), plus initial `AssetServiceTests`, `MaintenanceServiceTests`, `StraightLineDepreciationTests`; test infrastructure `CoreGridWebApplicationFactory`, `TestAuthHandler`, `FixedCurrentOrganizationProvider` |
| React (17 files) | `AuditPage`, `AuditLogPanel`, `CampaignsPanel`, `DiscrepanciesPanel`, `AuditReportPanel`, `ReportsPage`, `WorkflowsPage`, `RoleLayout`, `PolicyParametersPanel`, `permissions`, `recovery`, `campaignScopeLabel`, `notificationDisplay`, `depreciation`, `dates`, `errorMessage`, `statusTag` |
| Flutter (8 files) | `app_shell_test`, `campaigns_test`, `scan_to_verify_test`, `verification_task_list_screen_test`, `initiate_workflow_screen_test`, `find_asset_card_test`, `auth_config_test`, `widget_test` |
| CI | Both GitHub Actions pipelines (see §2.7) |

Run of 2026-10-04: backend **437/437**, React **113/113**, Flutter **69/69**, `flutter analyze` 0 issues, build 0 warnings.

### 2.7 Git evidence and CI

| Item | Evidence |
|---|---|
| Repository setup | Initial commits `d0a914a` (frontend) and `88d7d03` (backend), 2026-08-08/09; GitHub organisation repositories; branch prefixes `feature/audit-*`, `feature/config-*`, `feature/ci-*` |
| CI — main repo | `.github/workflows/ci.yml` (`a726f62`, hardened in `e46e777`, `fdfa755`): backend job with `-warnaserror`, PostgreSQL 16 service container, migrations, tests; frontend job with Vitest and type-checked build; concurrency cancelling |
| CI — mobile repo | `c7f9386`, `fd95d9d`: analyze, test, release APK artifact |
| Containers | `backend/Dockerfile`, `frontend/Dockerfile` + `nginx.conf`, `docker-compose.yml`, `Makefile` (`make check` mirrors CI) |
| Pull requests | CoreGrid #1–#7, #10–#12, #15, #19, #24, #33, #37; mobile #1, #2, #5, #8, #10 merge. Several are integration merges of teammates' branches into `development`. ⟦Add links and reviewers⟧ |
| Integration | Merged and reconciled teammates' branches; restored teammates' transfer screens in mobile after a conflicting refactor (`f077d49`) |

### 2.8 Documentation

- **SRS** (`docs/srs/`): baseline authoring and maintenance, including the roster and allocation (§18) and the AI disclosure (Appendix E).
- **ADRs** (`docs/architecture/decision-records.md`): owner of ADR-001, ADR-004, ADR-010 and ADR-011; co-owner of ADR-006; the 2026-10-04 revision of ADR-003 to match the code; coordination of the full set (Team Roster §18.6).
- **Setup guides** (`docs/setup/`): ThunderID, Cloudflare R2, AI agents. Also the ThunderID scripts (`scripts/thunderid/`).
- **Mobile documentation** (now `docs/coursework/mobile/`) and the progress and contribution trackers (`docs/progress.md`, `docs/coursework/team/contribution-history.md`).
- **Project files:** `README.md`, `frontend/README.md`, `CONTRIBUTING.md`.
- **Coursework:** consolidated group report and the individual report structure.

### 2.9 Deployment (coursework)

API container on Microsoft Azure; PostgreSQL on Azure; React on Vercel; `coregrid-web` (features, user manual, changelog) on GitHub Pages; release APK. ⟦Add URLs and screenshots⟧.

## 3. Key Commits

| Commit | Date | Description |
|---|---|---|
| `d0a914a`, `88d7d03` | 2026-08-08/09 | Frontend and backend initialisation |
| `cf43d93`, `add01a4`, `eb16174` | 2026-08-09 → 08-12 | Login, auth, user provisioning |
| `5702ca1`, `713e7d8` | 2026-08-09/10 | Single-organisation configuration baseline |
| `1a19900` | 2026-08-15 | Backend file restructure |
| `a708373` | 2026-08-15 | Component D baseline: audit & compliance, org config, user administration |
| `87b2fd3` | 2026-08-15 | User management and role dashboards |
| `a726f62` | 2026-08-21 | CI workflow, policy management, agent workflows |
| `aa00c9f` | 2026-09-12 | Org query filter, user auto-provisioning, endpoint authorisation lockdown |
| `4ca2f62`, `8141231` | 2026-09-14 | Component D completion; tests |
| `0040165` | 2026-09-15 | Authorisation verification |
| `3fe85bd`, `4dda6bb` | 2026-09-19 | Backend modularisation phases 1–4 |
| `e46e777` | 2026-09-19 | RBAC wiring, SRS gaps, CI cleanup |
| `583671e` | 2026-09-26 | Reusable shared components, UI/UX fixes |
| `3ea91f0`, `2bfcb66` | 2026-09-26 | Tests for photos/LLM/campaign reports; LLM and photo endpoints |
| `242682c` | 2026-09-26 | Campaign report models, disposals, maintenance |
| `fdfa755` | 2026-09-26 | Docker components, CI fixes |
| `cf7b274`, `29dd490` | 2026-09-27 | Role authorisation fixes; mobile ThunderID auth |
| `0743fba` | 2026-09-27 | Password reset via ThunderID with tests |
| mobile `e6ab745` | 2026-08-18 | Flutter core init |
| mobile `1eec808`, `3c7eb3d` | 2026-09-15 | Dashboards, verification, workflows; onboarding |
| mobile `c5258c2`, `1c72182`, `3c62524` | 2026-09-26 | Auth return flow, roles scope, assets API refactor and tests |
| mobile `635f6db`, `e8d3d92`, `91df428` | 2026-09-27 | UI revamp, shell, org config + password reset |
| mobile `c7f9386`, `fd95d9d` | 2026-09-28 | Mobile CI |

## 4. Challenges and Learning

⟦Write in your own words. Things from your history you could discuss:
- Moving tenant isolation from per-call-site `.Where` clauses to one EF global query filter, and filtering child entities through their parent.
- Resolving the caller once per request instead of in every controller, the audit interceptor and the filter provider.
- One identity provider for three flows: web PKCE, mobile AppAuth custom scheme, and admin provisioning through SCIM.
- Keeping the Policy agent deterministic, and defending that in front of "AI" expectations.
- Integrating four members' branches into `development`, resolving conflicts, and keeping CI green.
- The backend modularisation refactor while teammates kept working.⟧

## 5. Individual AI Usage Log

| Date / period | Tool and model | Task and section | What the tool produced | What was changed or rejected | How it was verified |
|---|---|---|---|---|---|
| 2026-08-08 – 09-25 | Claude Code — Sonnet 5 | Backend/frontend foundation, authentication, Component D, administration, organisation isolation, authorisation, tests, docs | Coding assistance, explanations, debugging guidance, test interpretation | Adapted or rejected suggestions to fit CoreGrid's architecture, requirements and environment | Builds, API requests, DB checks, backend/frontend tests, CI, manual testing |
| 2026-09-26 – 09-27 | Claude Code — Opus 5.5 | Commits `242682c`, `d6b9358`, `fdfa755`, `cf7b274`, `29dd490` | Coding and debugging suggestions | Kept only code matching the requirements and observed runtime behaviour | Tests, API requests, logs, Docker/CI checks, mobile login, role checks |
| 2026-09 | Codex Luna | Contribution index and AI-usage disclosure | Git-history audit, disclosure structure | Documentation only; no code changed | Cross-checked against the Git history |
| 2026-10-04 | Claude Code — Opus 5.5 | Readiness review; report drafts; README, ADR-003/004 and SRS corrections | Repository analysis (build/test runs, Git statistics), drafts with placeholders | ⟦What you changed or rejected⟧ | ⟦e.g. re-ran the test commands; checked each claim against the code⟧ |

## 6. AI Reflection (≈ 1 page — must be written by the student)

> A reflection that is AI-generated, or that does not match your Git history and AI log, receives no credit.

**Which AI tools were used, and at which stages?** ⟦⟧

**What did they do well, and what did they get wrong?** ⟦⟧

**What did you change, add or reject, and why?** ⟦⟧

**What did you learn about your own skills and understanding?** ⟦⟧

## 7. Declaration

I confirm that this section describes my own contribution, that all AI use is disclosed above, and that I can explain, test, modify and debug the work submitted under my name. I will not use any external AI tool during the demonstration or viva.

| Name | Student ID | Signature | Date |
|---|---|---|---|
| Hasitha Erandika | ⟦⟧ | | |
