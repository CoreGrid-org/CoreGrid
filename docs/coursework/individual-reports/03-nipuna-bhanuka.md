# Individual Report — Nipuna Bhanuka (Student 3)

> Team Roster §18.5 and the older report call this member "Bhanuka Samarasinghe"; the roster says "Nipuna Bhanuka". Confirm the official name and use it everywhere.

| Item | Detail |
|---|---|
| Student ID | ⟦ID⟧ |
| GitHub | `NipunaBhanuka18` |
| Primary component | **C — Transfer & Disposal** (FR-043 – FR-055) |
| Agent owned | **Budget Analysis Agent** (Node 3) |
| Business-specific operation | `POST /api/disposals/{id}/approve`: preconditions P1–P6 + separation of duties |
| Golden cases owned (Team Roster §18.7) | GC-02 (correct repair recommendation); reviewer of GC-11 |
| Git footprint | CoreGrid: 18 commits · coregrid-mobile: 1 commit |

## 1. Contribution Statement

I owned Component C: moving assets between departments and taking them out of service. My work covered:
- the `AssetTransfer` and `DisposalRequest` entities;
- the transfer state machine;
- condemnation, and disposal submission, revision and approval;
- the disposal precondition engine, which checks six preconditions (P1–P6) plus separation of duties before an asset may become `DISPOSED`;
- the read-only financial agent tools;
- the Budget Analysis Agent: first a standalone Python/LangGraph service, then migrated in-process to C# and wired into the pipeline as Node 3.

On the web I connected the transfer and disposal screens to the backend for Administrators, Inventory Officers and Auditors. On mobile I implemented transfer initiation and scan-to-confirm receipt.

## 2. Owned Work (Team Roster §18.5)

### 2.1 Backend

| Area | Work |
|---|---|
| Controllers | `TransfersController`, `DisposalsController`, financial endpoints of `AgentToolsController` |
| Transfer endpoints | `POST /api/transfers`, `POST {id}/approve`, `POST {id}/reject`, `POST {id}/confirm-receipt`, `GET` (paged), `GET {id}`, `GET /api/assets/{assetId}/transfers` (FR-047) |
| Disposal endpoints | `POST /api/assets/{id}/condemn`, `POST /api/disposals`, `POST {id}/approve`, `POST {id}/request-revision`, `POST {id}/reject`, `GET` (paged), `GET {id}` |
| Business rules | Transfer `REQUESTED → APPROVED → COMPLETED` with asset `TRANSFER_REQUESTED → IN_TRANSIT → ACTIVE`; receipt updates department and location atomically. Disposal `PENDING → APPROVED` (asset `DISPOSED`) with preconditions P1–P6: P4 is the maintenance lock; P6 requires a linked agent workflow at `AWAITING_APPROVAL` with a PASS validation result. Separation of duties (requester ≠ approver). Reject releases the asset (transfer → `ACTIVE`, disposal → `CONDEMNED`) |

### 2.2 Database

Entities `AssetTransfers` and `DisposalRequests` with real FK constraints, plus `DisposalPreconditionResult`. Migrations: `AddTransferAndDisposalEntities`, `AddTransferDisposalForeignKeys`, `AddValuationDateToDisposalRequest`, `AddBudgetAnalysisToAgentWorkflow` (`jsonb`). These endpoints are the highest-contention write paths, so optimistic concurrency (`xmin` → 409, DR-11) matters most here.

### 2.3 React

Transfers & Disposals for three roles, wired to the real backend: a live P1–P6 precondition checklist, approve/reject/request-revision, initiate transfer, confirm receipt, condemn, submit disposal, the Auditor read-only view, and server-side pagination.

### 2.4 Flutter

`initiate_transfer_screen`, `confirm_receipt_scan_screen` (camera scan of the arriving asset's QR to match identity before confirming), `transfer_list_screen`, `transfer_detail_screen`, `transfer_status_chip`, `transfer_summary_card`, `transfers_api`, `transfers_providers`.

### 2.5 Agentic AI — Budget Analysis Agent

| Aspect | Detail |
|---|---|
| Responsibility | Weigh residual value against repair and replacement cost and the departmental budget; rank options |
| Input | `workflowId`, `assetId` + Node 2 maintenance evidence |
| Output | `FinancialAssessmentResultDto { rankedOptions[{option, score, rationale}], proposedRecommendation, … }`, persisted to `AgentWorkflows.BudgetAnalysis` and consumed by the Policy Compliance Agent |
| Tool permissions | `get_asset_financials`, `get_department_budget_summary`, `compute_depreciation` (read-only, in-process via `IAgentToolsService`) |
| Validation / safety | `BudgetScopeGuard` (15 tests) validates model output and scope; deterministic fallback on missing key, timeout, error or invalid output; graceful degradation so Node 4 still runs |
| Orchestration | Node 3; re-runnable on its own via `POST /api/agent-workflows/{id}/run-budget-agent` |

### 2.6 Tests

| Suite | Tests authored |
|---|---|
| Backend | `DisposalPreconditionServiceTests` (20), `DisposalServiceTests` (17), `TransferServiceTests` (12), `BudgetScopeGuardTests` (15), `AgentToolsServiceTests` (7), `BudgetAgentServiceTests` (4) |
| React | `TransfersPage.test.tsx`, `capabilities.test.ts` (role-boundary regression coverage) |
| Flutter | ⟦None yet; `test/features/transfers/` is empty. Add widget tests for initiate-transfer validation and receipt asset-identity mismatch⟧ |

Run of 2026-10-04: backend 437/437, React 113/113, Flutter 69/69.

### 2.7 Git evidence

Branch prefixes `feature/transfer-*`, `Bhanuka/feature/*`. PRs: CoreGrid #9, #22, #27, #29, #34 · mobile #8 (*feature/transfer-request-and-receipt*). ⟦Add links, reviewers and a merge-conflict-resolution example (Team Roster §18.5).⟧

### 2.8 Documentation

⟦README Component C section; concurrency-control design note (DR-11); Appendix E AI log.⟧

## 3. Key Commits

| Commit | Date | Description |
|---|---|---|
| `697c58f` | 2026-08-14 | `AssetTransfer` and `DisposalRequest` entities + migration |
| `1cc6433`, `dfded6c`, `3da7010` | 2026-08-18 | Precondition engine; transfer state machine; condemnation and disposal |
| `f7c3a19`, `3b7023f` | 2026-08-19/20 | Financial agent tools; P4 maintenance precondition |
| `9ce601f`, `ba4cc3e`, `8368e14` | 2026-09-12 | P6 precondition; Python Budget Agent; FR-053 and FR-047 |
| `e600cf0`, `65c64ac` | 2026-09-13 | Frontend wired to backend; Auditor read-only view |
| `b6fbf83`, `c92a52f` | 2026-09-18 | Budget Agent in C#; transfer/disposal pagination |
| `4793dc0` | 2026-09-24 | Budget Agent wired as Node 3 |
| `830c462` | 2026-09-27 | TransfersPage role-boundary tests |
| mobile `9ef8100` | 2026-09-27 | FR-043 initiate transfer + FR-046 scan-to-confirm receipt |

## 4. Challenges and Learning

⟦Write in your own words. Things from your history you could discuss:
- Modelling P1–P6 so each precondition reports pass/fail independently and the UI can show a live checklist.
- Migrating the Budget Agent from Python/LangGraph to C#: what LangGraph gave you, and what you had to rebuild.
- Two officers acting on the same transfer, and how `xmin` returns 409.⟧

## 5. Individual AI Usage Log

| Date / period | Tool and model | Task and section | What the tool produced | What was changed or rejected | How it was verified |
|---|---|---|---|---|---|
| 2026-08-14 – 08-20 | Antigravity — ⟦model/version⟧ | Transfer and disposal workflows | State transitions, condemnation/disposal flow, precondition checks, agent-tool endpoints | Adapted to the Component C state machine and API contracts | Builds, API requests, DB checks, workflow tests |
| 2026-09-12 – 09-13 | Antigravity — ⟦model/version⟧ | Budget Agent (Python), P6, transfer history, disposal revision, Auditor view | Agent, precondition code, frontend wiring | Rejected suggestions that conflicted with SRS permissions, enums or API behaviour | Agent/API tests, frontend tests, role-specific browser tests |
| 2026-09-18 | Antigravity — ⟦model/version⟧ | Budget Agent C# migration, pagination | Migration guidance, pagination, test fixes | Applied only changes confirmed by compilation and the workflow design | Builds, tests, API checks |
| 2026-09-24 | Antigravity — ⟦model/version⟧ | Node 3 orchestration, frontend fix | Pipeline wiring, build/test fix | Kept only fixes supported by the orchestration flow and test output | Builds, tests, orchestration checks |
| 2026-09-27 | ⟦tool/model⟧ | Mobile FR-043/FR-046 | ⟦⟧ | ⟦⟧ | ⟦⟧ |

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
| Nipuna Bhanuka | ⟦⟧ | | |
