# 12. Component Ownership

CoreGrid is split into four business components plus a shared platform. Each component has one maintainer, who reviews every change to its requirements, entities, endpoints and agent. A component's code lives in its own `backend/Features/<Name>/` and `frontend/src/features/<name>/` folders, and in the matching feature folders of the mobile repository (see `CONTRIBUTING.md` § Project Structure).

| | Component A | Component B | Component C | Component D |
|---|---|---|---|---|
| Name | Asset Registry & QR Identification | Maintenance Management | Transfer & Disposal | Audit & Compliance, Organisation Configuration, User Administration |
| Maintainer | Jayashan Guruge (`jguruge`) | Seneja (`seneja`) | Nipuna Bhanuka Samarasinghe (`NipunaBhanuka18`) | Hasitha Erandika (`HasithaErandika`), project lead |
| Requirements | FR-016 – FR-032 | FR-033 – FR-042, FR-077 – FR-080 | FR-043 – FR-055 | FR-010 – FR-015, FR-056 – FR-066, FR-081 – FR-086 |
| API | Asset categories, asset types and attribute definitions, assets, QR lookup, history, condition, verification | Maintenance lifecycle, fault reports, photos, preventive scheduling, notifications | Transfers, condemnation, disposal requests, precondition engine | Setup, users (SCIM), departments, locations, policies, verification campaigns and tasks, discrepancies, audit log, dashboards, reports |
| Entities (§8.2) | `AssetCategories`, `AssetTypes`, `AssetAttributeDefinitions`, `AssetAttributeValues`, `Assets`, `AssetHistory` | `MaintenanceRecords`, `Notifications` | `AssetTransfers`, `DisposalRequests` | `Organizations`, `Users`, `Departments`, `Locations`, `OrganizationPolicies`, `VerificationCampaigns`, `VerificationTasks`, `Discrepancies`, `AuditLogEntries`, `AgentWorkflows`, `AgentExecutionSteps`, `AgentApprovals` |
| Business-specific operation | `POST /api/assets/{id}/verify` | `POST /api/maintenance/{id}/complete` | `POST /api/disposals/{id}/approve` | `PATCH /api/discrepancies/{id}/resolve` |
| Web (React) | Asset list/detail/register, dynamic attribute forms, asset configuration | Maintenance pages, notification centre | Transfer and disposal queues, precondition checklist | Application shell, Setup, administration, audit, dashboards, reports, workflow approval |
| Mobile (Flutter) | QR scan, asset lookup/detail, condition update, asset verification | Fault reporting with photos, maintenance records, notifications | Transfer initiation, scan-to-confirm receipt | Authentication and app shell, dashboards, verification campaigns and tasks, workflows |
| Agent (§7.3) | Planner | Maintenance Analysis | Budget Analysis | Policy Compliance, orchestrator, human-approval checkpoint |
| Golden cases (§13.4) | GC-06, GC-07 | GC-05, GC-09 | GC-02 | GC-01, GC-03, GC-04, GC-08, GC-10, GC-11, GC-12 |

## 12.1 Shared Platform

The shared platform is maintained by the project lead and is a dependency of every component. Changes to it are reviewed with extra care because they affect every component at once:
- identity and access (FR-001 – FR-009);
- the organisation query filter;
- the audit interceptor;
- authorisation policies;
- error handling, health checks and rate limiting;
- CI and build tooling.

## 12.2 Cross-Component Changes

A change that touches another component's entities, endpoints or agent contract needs a review from that component's maintainer as well as from the author's own. The agent input/output contracts in §7.3 are shared by all four maintainers; see §18.3.
