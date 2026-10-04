# Individual Report — Seneja ⟦Ramanayaka / Thehansi⟧ (Student 2)

> Use one surname consistently. The SRS roster says *Ramanayaka*; Appendix E says *Thehansi*.

| Item | Detail |
|---|---|
| Student ID | ⟦ID⟧ |
| GitHub | `seneja` |
| Primary component | **B — Maintenance Management & Notifications** (FR-033 – FR-042, FR-077 – FR-080) |
| Agent owned | **Maintenance Analysis Agent** (Node 2) |
| Business-specific operation | `POST /api/maintenance/{id}/complete` (FR-038; BR1–BR3 in one transaction) |
| Golden cases owned (Team Roster §18.7) | GC-05 (insufficient data), GC-09 (tool timeout) |
| Git footprint | CoreGrid: 27 commits · coregrid-mobile: 4 commits |

## 1. Contribution Statement

I owned Component B: the maintenance lifecycle from fault report to completion, and the in-app notification system. I implemented:
- the maintenance state machine (`REQUESTED → APPROVED → IN_PROGRESS → COMPLETED/CANCELLED`);
- the guarded completion operation, which recalculates the asset's cumulative cost, repair count and condition in one transaction;
- the preventive-maintenance background scheduler;
- photo evidence through object storage;
- server-side filtered and paginated maintenance lists and reports.

On the web I built the maintenance pages and modals, the notification centre and the Maintenance report tab. On mobile I built the maintenance records view, the enhanced fault-reporting flow and the notifications feature. My agent, the Maintenance Analysis Agent, turns an asset's repair history into reliability evidence for the workflow.

## 2. Owned Work (Team Roster §18.4)

### 2.1 Backend

| Area | Work |
|---|---|
| Controllers | `MaintenanceController`, `NotificationsController` |
| Endpoints | `POST faults`, `POST photos`, `POST` (direct entry), `GET` (filters: status, priority, type, asset, department, assignee, date range; server-side sort + paging), `GET {id}`, `GET my-reports`, `PUT {id}`, `POST {id}/approve`, `{id}/start`, `{id}/complete`, `{id}/cancel`; notifications `GET`, `GET unread-count`, `PATCH {id}/read`, `PATCH read-all` |
| Business rules | BR1 cost-variance tolerance against organisation policy; BR2 resulting condition *Unserviceable* → asset `CONDEMNED`; BR3 atomic completion; re-completion blocked (409); asset locked `UNDER_MAINTENANCE` while active, blocking transfer and disposal (FR-039); cumulative cost and repair count (FR-040) |
| Services | `MaintenanceService`, `PreventiveMaintenanceScheduler` + `PreventiveMaintenanceBackgroundService` (FR-041), `NotificationService` (`INotificationService`) |

### 2.2 Database

Entities `MaintenanceRecords` and `Notifications`, with indexes for every list filter. Migrations: `AddMaintenanceRecord`, `AddNotifications`, `AddMaintenanceAnalysisToAgentWorkflow` (`jsonb` column for Node 2's output), `RenameMaintenancePhotoUrlToPhotoObjectKey` (stores an opaque object key, never a public URL).

### 2.3 React

Maintenance list with filters and paging, create-maintenance and report-fault modals, approve/assign/start/complete/cancel actions, the detail page, the notification bell with a live unread count in `RoleLayout`, and Reports → Maintenance tab with stats, a breakdown by asset type, and PDF/CSV export.

### 2.4 Flutter

`maintenance_records_view`, the enhanced `report_fault_screen` (photo evidence from camera or gallery, compressed), officer maintenance actions, `notifications_screen`, `notification_bell`, notification models/API/providers, and app routing updates.

### 2.5 Agentic AI — Maintenance Analysis Agent

| Aspect | Detail |
|---|---|
| Responsibility | Quantify the asset's reliability and maintenance-cost trajectory as evidence for the decision |
| Input | `workflowId`, `assetId` (organisation from the persisted workflow) |
| Output | Repair count, MTBF, cost trend and 12-month projection; persisted to `AgentWorkflows.MaintenanceAnalysis` (`jsonb`) and logged as an `AgentExecutionStep` |
| Tool permissions | `get_maintenance_history`, `compute_failure_statistics` (read-only) |
| Model use | None. Deterministic statistics make results reproducible and testable |
| Orchestration | Runs automatically after the Planner; re-runnable on its own via `POST /api/agent-workflows/{id}/run-maintenance-agent` |

### 2.6 Tests

| Suite | Tests |
|---|---|
| Flutter (authored) | `maintenance_records_test` (3), `notifications_test` (5) |
| Backend | Component B is covered by `MaintenanceServiceTests` (6). ⟦Add and list your own: BR1 variance rejection, BR2 condemnation, 409 on re-completion, preventive scheduler due date⟧ |
| React | ⟦Add and list a maintenance form/modal validation or error-state test⟧ |

Run of 2026-10-04: backend 437/437, React 113/113, Flutter 69/69.

### 2.7 Git evidence

Branch prefixes `feature/maintenance-*`, `Seneja/feature-*`. PRs: CoreGrid #8, #13, #14, #20, #32 · mobile #10 (*feature/notify_maintenance*). ⟦Add links and reviewers.⟧

### 2.8 Documentation

⟦README Component B section; notification-provider design note (Team Roster §18.4); Appendix E AI log.⟧

## 3. Key Commits

| Commit | Date | Description |
|---|---|---|
| `dd59ebb` | 2026-08-17 | Maintenance creation and approval endpoints |
| `d4b0e22`, `4f4b681` | 2026-08-18 | Start/complete; cancel/list; background service |
| `50de0ea`, `95d5e2a` | 2026-08-18 | Maintenance modals and hooks; table, indexes, migration |
| `dac9e48`, `2f4e94e`, `a37fdd4` | 2026-09-14 | Notifications schema and service; notification centre |
| `58f4b58` | 2026-09-14 | Maintenance analysis tools + notification integration |
| `fb77167` | 2026-09-17 | Maintenance logic and preventive scheduling |
| `fd7ddca`, `1167953` | 2026-09-17 | Storage configuration; photo upload and filtering |
| `f8b1bc9` | 2026-09-17 | Server-side pagination for reports |
| mobile `4c303fc`, `958572b` | 2026-09-27 | Fault reporting and maintenance records; notifications |

## 4. Challenges and Learning

⟦Write in your own words. Things from your history you could discuss:
- Making completion atomic: why cost, repair count, condition and history must commit together.
- Moving from storing a photo URL to storing an object key with short-lived signed URLs.
- Deciding to keep the Maintenance Analysis Agent deterministic.⟧

## 5. Individual AI Usage Log

| Date / period | Tool and model | Task and section | What the tool produced | What was changed or rejected | How it was verified |
|---|---|---|---|---|---|
| 2026-08-17 – 08-18 | Antigravity 3.6; ChatGPT ⟦model⟧ | Maintenance records, fault reporting, workflows | Entities, endpoints, DTOs, approve/start/complete/cancel flows, background processing, UI | Adapted to existing API contracts, DB model and frontend structure | Builds, API requests, DB checks, manual workflow tests |
| 2026-09-14 | Antigravity 3.6; ChatGPT ⟦model⟧ | Notifications, maintenance reports | Notification CRUD, notification centre, report panels | Kept only changes matching role permissions and domain model | Backend/frontend tests, API requests, browser tests |
| 2026-09-17 | Antigravity 3.6; ChatGPT ⟦model⟧ | Preventive maintenance, storage, photos, pagination | Scheduler, storage config, migrations, photo handling, pagination | Corrected configuration to the real environment | Builds, migrations, storage/API checks |
| 2026-09-27 | ⟦tool/model⟧ | Mobile notifications and maintenance (mobile PR #10) | ⟦⟧ | ⟦⟧ | ⟦⟧ |

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
| Seneja ⟦⟧ | ⟦⟧ | | |
