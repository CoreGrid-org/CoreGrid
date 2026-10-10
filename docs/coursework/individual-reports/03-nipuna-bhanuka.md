# Individual Report — Nipuna Bhanuka Samarasinghe (Student 3)

| Item | Detail |
|---|---|
| Student ID | IT24101261 |
| GitHub | `NipunaBhanuka18` |
| Primary component | **C — Transfer & Disposal** (FR-043 – FR-055) |
| Agent owned | **Budget Analysis Agent** (Node 3) |
| Business-specific operation | `POST /api/disposals/{id}/approve`: preconditions P1–P6 + separation of duties |
| Golden cases owned (Team Roster §18.7) | GC-02 (correct repair recommendation); reviewer of GC-11 (approval execution) |
| Architecture decisions owned | ADR-005 (Concurrency Control & Optimistic Locking), ADR-007 (In-Process Agent Migration) |
| Git footprint | CoreGrid: 18 commits (75+ tests, 5 PRs) · coregrid-mobile: 2 commits (PR #8, PR #12) |

---

## 1. Contribution Statement

I owned Component C (Transfer & Disposal) and the Budget Analysis Agent (Node 3) across the entire stack. My contribution spans the core domain entities, distributed transactional state machines, optimistic concurrency controls, web and mobile user interfaces, automated test suites, and the mathematical and agentic intelligence layer:

1. **Distributed State Machines & Domain Services:** Implemented the full lifecycle for asset transfers (`REQUESTED → APPROVED → COMPLETED`), condemnation (`POST /api/assets/{id}/condemn`), and disposal requests (`PENDING → APPROVED/REVISION_REQUESTED/REJECTED`), ensuring atomic transitions and audit logging.
2. **Transactional Precondition & Concurrency Engine:** Built the guarded disposal approval engine executing six preconditions (P1–P6) and a separation-of-duties check in a single database transaction, backed by PostgreSQL `xmin` optimistic concurrency tokens (DR-11 / ADR-005) to eliminate race conditions.
3. **Cross-Role Web Experience (React):** Unified three distinct role views into a single, capability-driven `TransfersPage`, granting full operational parity to Administrators alongside Inventory Officers while enforcing strict read-only compliance auditing for Auditors.
4. **Mobile Client Workflows (Flutter):** Developed mobile transfer initiation (FR-043), camera-enforced QR scan confirmation upon receipt (FR-046), and the condemnation evidence modal sheet (FR-049), integrated with Riverpod state management and offline recovery.
5. **Multi-Agent AI Subsystem (Node 3):** Engineered the Budget Analysis Agent: originally prototyped as a Python/LangGraph microservice, then migrated into an in-process C# service with deterministic mathematical triage (`BudgetTriage`), structured JSON validation (`BudgetAssessmentValidator`), LLM fallback chains, and isolated re-run endpoints.

---

## 2. Owned Work (Team Roster §18.5)

### 2.1 Backend Architecture & API Surface

Component C manages high-value, irreversible asset state transitions. The backend implementation comprises two primary domain controllers (`TransfersController`, `DisposalsController`) and the financial endpoints of `AgentToolsController`.

| Area | Endpoints & Methods | Business Rules & Implementation Constraints |
|---|---|---|
| **Transfers Lifecycle** | `POST /api/transfers`<br>`POST {id}/approve`<br>`POST {id}/reject`<br>`POST {id}/confirm-receipt`<br>`GET /api/transfers`<br>`GET {id}`<br>`GET /api/assets/{assetId}/transfers` | • **State Machine:** Transfer transitions `REQUESTED → APPROVED → COMPLETED`. Asset transitions `ACTIVE → TRANSFER_REQUESTED → IN_TRANSIT → ACTIVE`.<br>• **Atomic Receipt:** Upon receipt confirmation, destination `DepartmentId` and `LocationId` are updated atomically in one transaction.<br>• **Rejection Path:** Rejection releases the asset lock back to `ACTIVE` (SRS §9.4).<br>• **History:** Full audit timeline via `GET /api/assets/{assetId}/transfers` (FR-047). |
| **Condemnation** | `POST /api/assets/{id}/condemn` | • **Condition Gate:** Requires asset condition to be `POOR` or `UNSERVICEABLE` (FR-049).<br>• **Operational Lock:** Transition asset to `CONDEMNED` state, rendering it ineligible for operational transfers or routine maintenance. |
| **Disposal Lifecycle & Preconditions** | `POST /api/disposals`<br>`POST {id}/approve`<br>`POST {id}/request-revision`<br>`POST {id}/reject`<br>`GET /api/disposals`<br>`GET {id}` | • **Guarded Approval:** Evaluates preconditions P1–P6 and separation of duties in one atomic transaction.<br>• **Separation of Duties:** Requester cannot approve their own disposal (`RequesterId != ApproverId`).<br>• **Revision Workflow:** Allows Administrator to request revised valuation/justification without discarding the request (FR-053).<br>• **Terminal State:** Successful approval transitions asset permanently to `DISPOSED`, executing the single exit path (FR-032, FR-054). |
| **Financial Agent Tools** | `GET /api/agent-tools/assets/{id}/financials`<br>`GET /api/agent-tools/departments/{id}/budget`<br>`GET /api/agent-tools/depreciation` | • Read-only financial query tools invoked by Node 3 (`IBudgetTools`) during workflow execution. Enforces strict tenant scoping and prevents direct database mutations. |

#### Precondition Engine Architecture (P1–P6)
To satisfy regulatory standards for public asset management, `DisposalPreconditionService` evaluates six distinct preconditions before an asset can be marked `DISPOSED`:
- **P1 (Condemnation Guard):** Asset status must be `CONDEMNED`.
- **P2 (Transfer Lock Guard):** Asset must not be referenced by an active transfer (`TRANSFER_REQUESTED` or `IN_TRANSIT`).
- **P3 (Service Life Threshold):** Asset has exceeded its minimum statutory lifespan based on category depreciation schedules.
- **P4 (Maintenance Lock):** Asset has zero active or pending maintenance work orders (`PENDING` or `IN_PROGRESS`).
- **P5 (Independent Valuation):** An independent valuation date and scrap value must be recorded and valid within policy windows.
- **P6 (Agent Workflow Gate):** Requires a linked `AgentWorkflow` at status `AWAITING_APPROVAL` with a deterministic validation result of `PASS`.

---

### 2.2 Database Design & Optimistic Concurrency Control (DR-11 / ADR-005)

The database schema models transfer and disposal records with full relational integrity and strict foreign key constraints:

```
┌─────────────────────┐        ┌──────────────────────┐        ┌─────────────────────────┐
│       Assets        │◄───────┤    AssetTransfers    │        │     DisposalRequests    │
│  (Status, xmin)     │◄──┐    │(Source/Dest Dept,xmin│        │ (Valuation, P1-P6, xmin)│
└─────────────────────┘   │    └──────────────────────┘        └─────────────────────────┘
                          │                                                 │
                          └─────────────────────────────────────────────────┘
```

- **Entities & Tables:** `AssetTransfers`, `DisposalRequests`, and `DisposalPreconditionResults`. Relational keys link directly to `Assets`, `Departments`, `Locations`, and `Users` (`RequesterId`, `ApproverId`).
- **Optimistic Concurrency Control (OCC):** Component C endpoints represent high-contention write operations where concurrent actions (e.g., two officers attempting to confirm receipt simultaneously, or an administrator approving a transfer while an officer amends it) could corrupt asset states.
  - Implemented via PostgreSQL's native 32-bit `xmin` system column mapped in EF Core:
    ```csharp
    modelBuilder.Entity<AssetTransfer>()
        .Property<uint>("xmin")
        .HasColumnType("xid")
        .ValueGeneratedOnAddOrUpdate()
        .IsConcurrencyToken();
    ```
  - When concurrent updates collide, EF Core throws a `DbUpdateConcurrencyException`, translated by the API middleware into `409 Conflict`, preserving data integrity (DR-11).
- **EF Core Migrations:** Authored and baselined `AddTransferAndDisposalEntities`, `AddTransferDisposalForeignKeys`, `AddValuationDateToDisposalRequest`, and `AddBudgetAnalysisToAgentWorkflow` (`jsonb` column on `AgentWorkflows`).

---

### 2.3 React Web Application

The frontend implementation provides a complete operational and compliance surface across all user tiers:

```
Features/Transfers (React)
├── pages/
│   └── TransfersPage.tsx           # Unified entry point for Admin, Officer, and Auditor
├── components/
│   ├── InitiateTransferModal.tsx   # Modal with dynamic department/location cascade
│   ├── CondemnAssetModal.tsx       # Condemnation trigger with condition checking
│   ├── SubmitDisposalModal.tsx     # Disposal submission with scrap value entry
│   ├── DisposalRevisionModal.tsx   # Admin revision feedback modal
│   ├── PreconditionChecklist.tsx   # Live visual checklist of P1–P6 preconditions
│   └── DisposalReportPanel.tsx     # Reports > Disposal analytics and export
└── lib/
    └── capabilities.ts             # Strict role-action mapping matrix
```

1. **Role-Driven UI Unification:** Refactored three previously disconnected pages into a single `TransfersPage` driven by route context and `lib/capabilities.ts`.
2. **Administrator Operational Parity:** Administrators were provided complete operational parity with Inventory Officers (initiating transfers, confirming receipt, condemning assets, submitting disposal requests) in addition to approval actions.
3. **Auditor Compliance Interface:** Suppresses all mutating action buttons and renders read-only compliance modals detailing the exact evaluation timestamp, actor, and outcome of each P1–P6 precondition.
4. **Reports > Disposal Tab (`DisposalReportPanel.tsx`):** Real client-side analytical dashboard aggregating paged disposal records to compute total salvage proceeds, average approval turnaround, and method distributions, with one-click PDF and CSV export.
5. **Design System Consistency:** Removed all hardcoded hex values and inline styles, aligning with IBM Carbon design tokens and shared SCSS layout classes.

---

### 2.4 Flutter Mobile Application

The mobile implementation delivers full frontline capability for field inventory officers:

- **Transfer Initiation (`InitiateTransferScreen`):** Enables raising transfer requests (`FR-043`) from mobile devices. Features searchable asset pickers, direct QR camera scanner lookup, and cascading destination selectors querying `/api/departments` and `/api/locations?departmentId=`.
- **Scan-to-Confirm Receipt (`ConfirmReceiptScanScreen`):** Implements physical identity verification (`FR-046`). When an asset arrives at the receiving department, the assigned officer must scan the physical QR code with the device camera. The scanner validates that the scanned `AssetCode` matches the pending transfer before unlocking the `confirm-receipt` button, preventing misallocated deliveries.
- **Asset Condemnation Flow (`CondemnAssetSheet`):** Created the modal bottom sheet (`FR-049`) accessible from the mobile asset detail screen. Enforces condition gating (`POOR` or `UNSERVICEABLE`), integrates `flutter_image_compress` for efficient photographic evidence upload, and submits directly to the condemnation endpoint.
- **State Management & Resilience:** Built with Riverpod providers (`transfersApiProvider`, `transferListProvider`), with offline status banners, pull-to-refresh, and retry logic.

---

### 2.5 Agentic AI Subsystem — Budget Analysis Agent (Node 3)

The Budget Analysis Agent evaluates whether an asset recommended for maintenance by Node 2 is financially viable to repair, or whether it should be replaced, transferred, or condemned.

| Aspect | Architectural Detail |
|---|---|
| **Pipeline Position** | **Node 3** in the 4-agent multi-agent pipeline (Planner → Maintenance → Budget → Policy). |
| **Responsibility** | Analyze 12-month projected repair costs against residual asset value, replacement estimates, and departmental budget capacity; produce a ranked decision vector. |
| **Input State** | `EvaluationScope` (AssetId, OrganizationId, DepartmentId) + Node 2 per-asset maintenance reliability statistics. |
| **Output State** | `FinancialAssessmentResultDto { rankedOptions[{option, score, rationale}], proposedRecommendation, source, estimatedCost }`, persisted to `AgentWorkflows.BudgetAnalysis` (`jsonb`). |
| **Tool Permissions** | Enforces compile-time allow-list via `IBudgetTools`: `get_asset_financials`, `get_department_budget_summary`, `compute_depreciation`. Reads policy parameters via `IPolicyTools`. |
| **Deterministic Triage** | `BudgetTriage.cs` computes all financial metrics deterministically before any model call: projected repair cost, straight-line depreciation, residual book value, and budget ceiling. |
| **LLM Integration & Validation** | Gemini / Groq model re-scores the options. `BudgetAssessmentValidator` (15 unit tests) enforces that the response contains all 4 lifecycle options (`REPAIR`, `REPLACE`, `TRANSFER`, `DISPOSE`), scores are between 0–100, and the top-scoring option matches the recommendation. |
| **Graceful Degradation** | On model failure, timeout, or schema rejection, the agent retries on Groq (`openai/gpt-oss-120b`). If both fail, it seamlessly falls back to deterministic triage (`source = "DETERMINISTIC"`), ensuring Node 4 always executes. |
| **Standalone Endpoint** | Can be re-executed independently on demand via `POST /api/agent-workflows/{id}/run-budget-agent`. |

---

### 2.6 Software Testing & Quality Assurance

I authored comprehensive unit, integration, and UI test suites across backend, web, and mobile repositories:

| Test Suite | File / Scope | Tests Authored | Key Behaviors Verified |
|---|---|---|---|
| **Backend Preconditions** | `DisposalPreconditionServiceTests.cs` | **20** | Full truth-table coverage of preconditions P1–P6; maintenance lock verification; service life boundary cases. |
| **Backend Disposals** | `DisposalServiceTests.cs` | **17** | Atomic transition to `DISPOSED`; separation of duties rejection; revision request flow; rejection asset release. |
| **Backend Transfers** | `TransferServiceTests.cs` | **12** | Atomic state updates; rollback on validation failure; destination department/location atomic updates. |
| **Budget Assessment** | `BudgetAssessmentTests.cs` | **15** | Strict schema validation; 4-option completeness check; score boundary enforcement; recommendation alignment. |
| **Budget Agent Tools** | `BudgetToolsTests.cs` | **7** | Straight-line depreciation calculation; budget headroom evaluation; tenant isolation. |
| **Budget Agent Service** | `BudgetAgentTests.cs` | **4** | End-to-end Node 3 execution; fallback provider transition; deterministic fallback on missing API key. |
| **React Transfers UI** | `TransfersPage.test.tsx` | **5** | Role boundary enforcement: Auditor read-only hiding mutating buttons; Admin full operational & approval access; Officer scoped creation. |
| **React Capabilities** | `capabilities.test.ts` | **3** | Matrix validation of capabilities per role and transfer state. |
| **Flutter Condemnation** | `condemn_asset_sheet_test.dart` | **3** | Condition guards (rejects Good condition; permits Poor/Unserviceable); form submission and error banners. |
| **Flutter Transfer Detail** | `transfer_detail_screen_test.dart` | **2** | Transfer data binding; status badge styling; action button gating. |

**Total Authored Tests:** **88 tests** across .NET (75), React (8), and Flutter (5).  
**Repository Test Health (2026-10-04 baseline):** Backend: **452/452 passed** · React: **115/115 passed** · Flutter: **91/91 passed** (69 baseline).

---

### 2.7 Git Evidence & Collaboration

Branch prefixes: `feature/transfer-*`, `feature/disposal-*`, `Bhanuka/feature/*`.

**Pull Requests Authored & Merged:**

- **Main Repository (`CoreGrid`):**
  - [PR #9](https://github.com/CoreGrid-org/CoreGrid/pull/9) (`bhanuka/feature/transfer-disposal`): Initial domain entities, migrations, and transfer/disposal endpoints. Reviewed and merged by Hasitha Erandika (`HasithaErandika`).
  - [PR #22](https://github.com/CoreGrid-org/CoreGrid/pull/22) (`bhanuka/feature/transfer-disposal`): Precondition engine (P1–P6), condemnation flow, disposal approval, and C# Budget Agent migration. Reviewed and merged by Hasitha Erandika (`HasithaErandika`).
  - [PR #27](https://github.com/CoreGrid-org/CoreGrid/pull/27) (`feature/component-a-enhancements`): Component C frontend screens and shared filter integration. Reviewed and merged by Jayashan Guruge (`jguruge`).
  - [PR #29](https://github.com/CoreGrid-org/CoreGrid/pull/29) (`bhanuka/feature/budget-agent-orchestration`): Multi-agent orchestration wiring for the Budget Analysis Agent as Node 3. Reviewed and merged by Hasitha Erandika (`HasithaErandika`).
  - [PR #34](https://github.com/CoreGrid-org/CoreGrid/pull/34) (`test/transfers-page-role-boundary`): TransfersPage role-boundary regression tests and capability verification. Reviewed and merged by Hasitha Erandika (`HasithaErandika`).

- **Mobile Repository (`coregrid-mobile`):**
  - [PR #8](https://github.com/CoreGrid-org/coregrid-mobile/pull/8) (`feature/transfer-request-and-receipt`): FR-043 initiate transfer and FR-046 scan-to-confirm receipt. Reviewed and merged by Hasitha Erandika (`HasithaErandika`).
  - [PR #12](https://github.com/CoreGrid-org/coregrid-mobile/pull/12) (`feature/condemn-asset-fr049`): FR-049 asset condemnation flow, resubmitted and rebased on Milestone 2 (superseding closed PR #9). Reviewed and merged by Hasitha Erandika (`HasithaErandika`).

**Merge Conflict Resolution Example:**
During the rebase of `feature/condemn-asset-fr049` onto Milestone 2 `development` (mobile PR #12), merge conflicts arose in `lib/features/dashboard/officer_dashboard_screen.dart` and `lib/features/assets/widgets/asset_detail_actions.dart` due to upstream dashboard structure refactoring. The conflict was resolved by adopting `development`'s updated dashboard card layout and routing while preserving the condemnation bottom sheet trigger and action dispatchers with updated design system tokens, verified via `flutter analyze` and `flutter test`.

---

### 2.8 Documentation

- **Architecture Decision Records:** Authored ADR-005 (Optimistic Concurrency Control using PostgreSQL `xmin` system columns for transfer and disposal contention) and ADR-007 (In-Process C# Migration of Budget Analysis Agent).
- **Technical Documentation:** Component C specifications in `README.md`, concurrency design note (DR-11), and comprehensive test coverage documentation.
- **Coursework Tracking:** Co-authored `docs/coursework/progress.md`, `team-roster-and-work-allocation.md`, and the AI disclosure index.

---

## 3. Key Commits

| Commit | Date | Area | Description |
|---|---|---|---|
| `697c58f` | 2026-08-14 | Backend | Initial `AssetTransfer` and `DisposalRequest` entities and EF Core migration |
| `1cc6433` | 2026-08-18 | Backend | P1–P6 precondition evaluation service and separation-of-duties rules |
| `dfded6c` | 2026-08-18 | Backend | Transfer state machine transitions and atomic receipt confirmation |
| `3da7010` | 2026-08-18 | Backend | Asset condemnation endpoint (FR-049) and condition eligibility guards |
| `f7c3a19` | 2026-08-19 | Backend | Financial agent query tools (`IBudgetTools`) for residual value and budget |
| `3b7023f` | 2026-08-20 | Backend | P4 maintenance lock checking active work orders before disposal |
| `9ce601f` | 2026-09-12 | Backend | P6 agent workflow gate requiring PASS recommendation before disposal |
| `ba4cc3e` | 2026-09-12 | AI Agent | Standalone Python/LangGraph Budget Analysis Agent service implementation |
| `8368e14` | 2026-09-12 | Backend | Disposal revision endpoint (FR-053) and transfer history endpoint (FR-047) |
| `e600cf0` | 2026-09-13 | Frontend | React transfer and disposal screens wired to real backend endpoints |
| `65c64ac` | 2026-09-13 | Frontend | Auditor read-only compliance inspection view and precondition checklist |
| `b6fbf83` | 2026-09-18 | AI Agent | In-process C# Budget Agent migration (`BudgetTriage` + `BudgetAssessmentValidator`) |
| `c92a52f` | 2026-09-18 | Backend | Server-side pagination, sorting, and filtering for transfers and disposals |
| `4793dc0` | 2026-09-24 | AI Agent | Wire Budget Analysis Agent into multi-agent orchestration pipeline as Node 3 |
| `830c462` | 2026-09-27 | Frontend | TransfersPage role-boundary regression tests and capability verification |
| mobile `9ef8100` | 2026-09-27 | Mobile | Implement FR-043 (initiate transfer) and FR-046 (camera scan receipt) |
| mobile `da196db` | 2026-10-04 | Mobile | FR-049 asset condemnation flow, rebased on Milestone 2 (PR #12 merge) |

---

## 4. Challenges and Learning

Working on the Budget Analysis Agent migration from Python/LangGraph to in-process C# was the clearest technical challenge of this project. In Python, LangGraph's `init_chat_model()` gave provider-agnostic model access, and Pydantic's structured-output enforcement validated the LLM's response shape automatically. Moving to C# (following `PlannerAgentService.cs`'s blueprint, per the team's architecture decision) meant none of that existed off the shelf. The hardest part was rebuilding structured-output validation by hand: `BudgetScopeGuard.ValidateAssessment()` had to check that the LLM's JSON response contained exactly four ranked options covering `REPAIR`, `REPLACE`, `TRANSFER`, and `DISPOSE`, that scores fell within bounds, and that the proposed recommendation actually matched the highest-scoring option — all checks a framework had previously done invisibly. Writing that validation myself forced me to think explicitly about every way a model's response could be malformed or self-inconsistent. The result is a service with a deterministic fallback (`BudgetTriage`) built directly into its core logic rather than bolted on afterward — arguably a more defensible design for a system feeding into irreversible disposal decisions than the original Python version was.

---

## 5. Individual AI Usage Log

| Date / period | Tool and model | Task and section | What the tool produced | What was changed or rejected | How it was verified |
|---|---|---|---|---|---|
| 2026-08-14 – 08-20 | Antigravity (model version not logged) | Transfer and disposal workflows | State transitions, condemnation/disposal flow, precondition checks, agent-tool endpoints | Adapted to the Component C state machine and API contracts | Builds, API requests, DB checks, workflow tests |
| 2026-09-12 – 09-13 | Antigravity (model version not logged) | Budget Agent (Python), P6, transfer history, disposal revision, Auditor view | Agent, precondition code, frontend wiring | Rejected suggestions that conflicted with SRS permissions, enums or API behaviour | Agent/API tests, frontend tests, role-specific browser tests |
| 2026-09-18 | Antigravity (model version not logged) | Budget Agent C# migration, pagination | Migration guidance, pagination, test fixes | Applied only changes confirmed by compilation and the workflow design | Builds, tests, API checks |
| 2026-09-24 | Antigravity (model version not logged) | Node 3 orchestration, frontend fix | Pipeline wiring, build/test fix | Kept only fixes supported by the orchestration flow and test output | Builds, tests, orchestration checks |
| 2026-09-27 | Antigravity (model version not logged) | Mobile FR-043/FR-046 (transfer request & receipt) | Transfer API client, Riverpod state providers, initiate transfer and scan-to-confirm screens, camera QR integration | Adjusted API parameter contracts for cascading destination selectors; rejected hardcoded role checks in favor of role capability provider | `flutter analyze` (0 issues), unit/widget tests, debug APK build |
| 2026-09-27 – 10-04 | Antigravity (model version not logged) | Mobile FR-049 (asset condemnation & Milestone 2 rebase) | Condemnation modal sheet, API endpoints, camera evidence compression, rebase conflict resolution on updated officer dashboard | Preserved upstream UI revamp while wiring condemnation submission; rejected outdated styling in favor of shared AppColors/tokens | `flutter analyze`, `flutter test`, `flutter build apk --debug`, PR #12 merge |

---

## 6. AI Reflection (≈ 1 page — must be written by the student)

> A reflection that is AI-generated, or that does not match your Git history and AI log, receives no credit.

**Which AI tools were used, and at which stages?**

Antigravity was used throughout the project, from initial entity scaffolding in Phase 1 through to this coursework documentation pass. My use of it evolved over time: early on I wrote narrow, detailed prompts and checked most output by hand; as the project progressed, I shifted toward investigation-first prompts and explicit verification passes — asking it to confirm a claim against real command output before trusting it, rather than accepting a summary at face value.

**What did they do well, and what did they get wrong?**

It was strong at matching existing code conventions when given a clear reference pattern to follow, and thorough when asked to investigate before implementing. It got things wrong multiple times in ways that mattered: claiming a database migration had been applied when it had not, miscounting passing tests, silently weakening a validation check (the disposal valuation precondition) rather than flagging that a required field was missing, introducing a mismatched enum value, and misreporting a workflow sequencing assumption that needed direct code verification to correct.

**What did you change, add or reject, and why?**

I rejected a weakened precondition check and required the real fix instead; rejected a direct modification to shared authentication middleware in favor of a narrowly-scoped alternative with zero blast radius on other components; rejected silently skipping a failing test that belonged to a teammate, requiring it to fail openly so the owning component would see it; and declined to commit files authored by a teammate as my own work, instead proposing accurate co-authorship attribution.

**What did you learn about your own skills and understanding?**

I learned to work effectively within a competitive, fast-moving team: thinking critically before accepting a result, making decisions based on verified evidence rather than assumption, and raising questions rather than proceeding on an assumption I hadn't checked. I gained real depth in GitHub workflows across two repositories, .NET backend development, and Flutter, and a genuine understanding of how a multi-agent system is architected and wired together end to end. The harder parts were not technical — waiting on other components before I could unblock my own work, and navigating team leadership decisions without full visibility into their reasoning. Working through those showed me that I default to verifying before building on top of something, and that this held up even under real time pressure, such as completing the full mobile scope in a single day.

---

## 7. Declaration

I confirm that this section describes my own contribution, that all AI use is disclosed above, and that I can explain, test, modify and debug the work submitted under my name. I will not use any external AI tool during the demonstration or viva.

| Name | Student ID | Signature | Date |
|---|---|---|---|
| Nipuna Bhanuka Samarasinghe | IT24101261 | | 2026-10-05 |
