# 16. Traceability

## 16.1 Requirements to Components

| Requirement range | Component | Maintainer (§12) | Primary API surface | Principal entities |
|---|---|---|---|---|
| FR-001 – FR-009 | Identity and access (cross-cutting) | Shared platform (§12.1) | `/api/me`, authentication middleware | Organizations, Users |
| FR-010 – FR-015 | Organisation configuration | D | `/api/departments`, `/api/locations`, `/api/users`, `/api/organization-policies` | Departments, Locations, Users, OrganizationPolicies |
| FR-016 – FR-020 | Type and attribute configuration | A | `/api/asset-categories`, `/api/asset-types` | AssetCategories, AssetTypes, AssetAttributeDefinitions |
| FR-021 – FR-032 | Asset registry and QR | A | `/api/assets` | Assets, AssetAttributeValues, AssetHistory |
| FR-033 – FR-042 | Maintenance | B | `/api/maintenance` | MaintenanceRecords, MaintenanceAttachments |
| FR-043 – FR-048 | Transfer | C | `/api/transfers` | AssetTransfers |
| FR-049 – FR-055 | Disposal | C | `/api/disposals` | DisposalRequests |
| FR-056 – FR-066 | Audit and compliance | D | `/api/verification-campaigns`, `/api/verification-tasks`, `/api/discrepancies`, `/api/audit-log`, `/api/reports/audit` | VerificationCampaigns, AuditVerifications, Discrepancies, AuditLogs |
| FR-067 – FR-076 | Agentic decision support | All four components; checkpoint owned by D | `/api/agent-workflows` | AgentWorkflows, AgentExecutionSteps, AgentApprovals |
| FR-077 – FR-080 | Notification | B | Internal `INotificationService` | Notifications |
| FR-081 – FR-086 | Dashboard and reporting | D, with per-component metrics from A–C | `/api/dashboard`, `/api/reports` | All |

## 16.2 Requirement to Verification Method

| Requirement group | Automated test | Inspection | Demonstration | Measurement |
|---|---|---|---|---|
| FR-001 – FR-009 identity and access | Authorisation matrix, isolation test | Middleware configuration | Role sign-in and denial | — |
| FR-010 – FR-020 configuration | Attribute validation tests | Schema and migration review | New asset type created live | — |
| FR-021 – FR-032 assets | Service and integration tests | Append-only history | QR scan to detail | NFR-03 |
| FR-033 – FR-042 maintenance | State machine and transaction tests | Notification isolation | Report from Flutter, manage in React | — |
| FR-043 – FR-055 transfer and disposal | Precondition and separation-of-duties tests | Terminal-state enforcement | Approval with evidence | — |
| FR-056 – FR-066 audit | Resolution and append-only tests | Audit log immutability | Campaign to discrepancy to report | — |
| FR-067 – FR-076 agentic | Golden cases GC-01 to GC-12 | Tool allow-list, state schema | The golden workflow | NFR-05 |
| FR-077 – FR-080 notification | Failure isolation test | Content minimisation | Notification received | — |
| FR-081 – FR-086 reporting | Scoping tests | Query filter application | Dashboard and export | NFR-06 |
| NFR-01 – NFR-08 performance | — | — | — | Performance report |
| NFR-09 – NFR-18 security | Authorisation and validation tests | Secret scan, dependency scan, OWASP review | Denied access shown | — |
| NFR-37 – NFR-41 auditability | Append-only and correlation tests | Log configuration | Trace one action across components | — |
