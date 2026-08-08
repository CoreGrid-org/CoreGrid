# 16. Traceability

## 16.1 Requirements to Components and Owners

| Requirement range | Component | Owner | Primary API surface | Principal entities |
|---|---|---|---|---|
| FR-001 – FR-009 | Identity and access (cross-cutting) | Shared, led by Student 4 | `/api/me`, authentication middleware | Organizations, Users |
| FR-010 – FR-015 | Organisation configuration | Student 4 | `/api/departments`, `/api/locations`, `/api/users`, `/api/policies` | Departments, Locations, Users, OrganizationPolicies |
| FR-016 – FR-020 | Type and attribute configuration | Student 1 | `/api/asset-categories`, `/api/asset-types` | AssetCategories, AssetTypes, AssetAttributeDefinitions |
| FR-021 – FR-032 | Asset registry and QR | Student 1 | `/api/assets` | Assets, AssetAttributeValues, AssetHistory |
| FR-033 – FR-042 | Maintenance | Student 2 | `/api/maintenance` | MaintenanceRecords, MaintenanceAttachments |
| FR-043 – FR-048 | Transfer | Student 3 | `/api/transfers` | AssetTransfers |
| FR-049 – FR-055 | Disposal | Student 3 | `/api/disposals` | DisposalRequests |
| FR-056 – FR-066 | Audit and compliance | Student 4 | `/api/campaigns`, `/api/discrepancies`, `/api/audit-logs`, `/api/reports` | VerificationCampaigns, AuditVerifications, Discrepancies, AuditLogs |
| FR-067 – FR-076 | Agentic decision support | All four; checkpoint owned by Student 4 | `/api/workflows` | AgentWorkflows, AgentExecutionSteps, AgentApprovals |
| FR-077 – FR-080 | Notification | Student 2 | Internal `INotificationService` | Notifications |
| FR-081 – FR-086 | Dashboard and reporting | Student 4, with per-component metrics from each owner | `/api/dashboard`, `/api/reports` | All |

## 16.2 Assignment Rubric to Specification

| Rubric criterion (marks) | Where specified | Evidence at evaluation |
|---|---|---|
| Component Design and Business Logic (10) | Section 6 in full; state machines in Figures 6 and 7; business-specific operations FR-031, FR-038, FR-051, FR-062. | All four components functional; guarded transitions demonstrated; the golden workflow completes end to end. |
| Integrated Architecture, Agent Orchestration and State Management (10) | Sections 3.1 to 3.3, Section 7 in full, Section 8.4. | Four distinct agents, persisted state inspected live, allow-listed tools, deterministic validation, safe failure, authorised approval. |
| Documentation and Deployment (10) | Sections 2.6, 14, Appendix E; this SRS. | Consolidated report, ADRs, AI usage logs, live URLs, health and Swagger, working APK, reproducible setup. |
| ASP.NET Core RESTful API Development (10) | Sections 3.2, 5.4, 9, 10.2. | DTOs, validation, policy authorisation, async operations, correct status codes, global exception handling, Swagger. |
| PostgreSQL Integration and Data Modelling (10) | Section 8 in full; DR-01 to DR-15. | ER diagram, constraints, indexes, migrations, seed data, transactions, concurrency conflict demonstrated. |
| React Web Application (10) | Sections 3.4, 5.1, FR-010 to FR-020, FR-081 to FR-086, FR-070 to FR-075. | Reusable components, routing, state management, protected routes, validation, loading and error states, approval panel. |
| Flutter Mobile Application (10) | Sections 3.4, 5.2, FR-024, FR-031, FR-033, FR-046, FR-059, FR-076. | Widgets, routing, Riverpod, secure token storage, QR scanning as the meaningful device feature, field workflows. |
| Individual Agentic AI Contribution (12) | Section 7.3 agent specifications; Section 12 allocation. | Each owner explains their agent's contract, tools, state, validation and approval interaction, and modifies it on request. |
| API Integration, Security and Cross-Platform Functionality (10) | Sections 4, 5.3, 5.4, 10.2; FR-001 to FR-009. | Both clients on one API and one identity; token handling; role denial demonstrated; the cross-platform loop traced. |
| Testing, CI and Git Workflow (8) | Section 13 in full; Section 12.1. | Passing CI run, test suite across all layers, golden cases, performance report, reviewed pull requests per owner. |

## 16.3 Requirement to Verification Method

| Requirement group | Automated test | Inspection | Demonstration | Measurement |
|---|---|---|---|---|
| FR-001 – FR-009 identity and access | Authorisation matrix, isolation test | Middleware configuration | Role sign-in and denial | — |
| FR-010 – FR-020 configuration | Attribute validation tests | Schema and migration review | New asset type created live | — |
| FR-021 – FR-032 assets | Service and integration tests | Append-only history | QR scan to detail | NFR-03 |
| FR-033 – FR-042 maintenance | State machine and transaction tests | Notification isolation | Report from Flutter, manage in React | — |
| FR-043 – FR-055 transfer and disposal | Precondition and separation-of-duties tests | Terminal-state enforcement | Approval with evidence | — |
| FR-056 – FR-066 audit | Resolution and append-only tests | Audit log immutability | Campaign to discrepancy to report | — |
| FR-067 – FR-076 agentic | Golden cases GC-01 to GC-12 | Tool allow-list, state schema | The golden workflow | NFR-05 |
| FR-077 – FR-080 notification | Failure isolation test | Content minimisation | Approval email received | — |
| FR-081 – FR-086 reporting | Scoping tests | Query filter application | Dashboard and export | NFR-06 |
| NFR-01 – NFR-08 performance | — | — | — | Performance report |
| NFR-09 – NFR-18 security | Authorisation and validation tests | Secret scan, dependency scan, OWASP review | Denied access shown | — |
| NFR-37 – NFR-41 auditability | Append-only and correlation tests | Log configuration | Trace one action across components | — |
