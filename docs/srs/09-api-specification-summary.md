# 9. API Specification Summary

The complete, authoritative contract is the Swagger/OpenAPI document served by the API at `/swagger`. The tables below summarise the implemented surface and the authorisation that governs each operation. Policies are the named ASP.NET Core policies of Appendix B (`Features/Shared/Auth/Policies.cs`). Where a route is guarded by an explicit role list instead, the roles are shown. Any route without an explicit policy or role list is denied by the fail-closed fallback policy unless it is marked anonymous. All list endpoints accept the standard paging, sorting, search and filter parameters described in Section 5.4.

## 9.1 Identity, Setup, Configuration and Users

| Method and route | Purpose | Authorisation |
|---|---|---|
| `GET /api/setup/status` | Whether first-run Setup has been completed. | Anonymous |
| `POST /api/setup/complete` | Create the organisation and its first Administrator (in ThunderID and in CoreGrid). Refused once an organisation exists. | Anonymous, rate-limited |
| `GET /api/me` | Resolved profile: user, organisation, department, role. | Authenticated |
| `GET /api/departments` | List departments. | Read roles |
| `POST /api/departments` · `PUT /api/departments/{id}` | Create / amend a department. | `CanManageConfiguration` |
| `PATCH /api/departments/{id}/deactivate` · `/activate` | Deactivate (refused while a non-disposed asset references it) / reactivate. | `CanManageConfiguration` |
| `GET, POST /api/locations` · `PUT /api/locations/{id}` · `PATCH …/deactivate`, `…/activate` | Manage locations within a department. | read: read roles; write: `CanManageConfiguration` |
| `GET /api/organization-policies` · `GET /{id}` · `POST` · `PUT /{id}` | Read and set policy thresholds (one per asset type plus an org-wide default). | `CanManageConfiguration` |
| `GET /api/users` | List users (search, paging). | Administrator, Inventory Officer, Auditor |
| `POST /api/users` | Provision a user through ThunderID SCIM and create the local mirror. | `CanManageUsers` |
| `PATCH /api/users/{id}` | Change role or department. | `CanManageUsers` |
| `PATCH /api/users/{id}/deactivate` · `/activate` | Deactivate (refused for the last active Administrator) / reactivate. | `CanManageUsers` |
| `POST /api/users/{id}/reset-password` | Set a new password through ThunderID. | `CanManageUsers` |

## 9.2 Component A — Asset Configuration and Registry

| Method and route | Purpose | Authorisation |
|---|---|---|
| `GET /api/asset-categories` · `GET /{id}` | List / read categories. | Read roles |
| `POST`, `PUT /{id}`, `DELETE /{id}`, `PATCH /{id}/activate` on `/api/asset-categories` | Manage categories (delete is a soft deactivation). | `CanManageConfiguration` |
| `GET /api/asset-types` · `GET /{id}` · `GET /{id}/attributes` | List / read types and their attribute definitions. | Read roles |
| `POST`, `PUT /{id}`, `DELETE /{id}`, `PATCH /{id}/activate` on `/api/asset-types` | Manage asset types. | `CanManageConfiguration` |
| `POST /api/asset-types/{id}/attributes` · `PUT`, `DELETE`, `PATCH …/activate` on `/{attributeId}` | Manage ordered attribute definitions. | `CanManageConfiguration` |
| `GET /api/assets` | List with search (incl. type, category and attribute values), filter, sort, page. | `CanReadAssets` |
| `GET /api/assets/{id}` | Full record with type, attributes and computed residual value. | `CanReadAssets` |
| `POST /api/assets` | Register an asset; validates custom attributes; generates code and QR payload. | `CanManageAssets` |
| `PUT /api/assets/{id}` | Amend; writes one history entry per changed field. | `CanManageAssets` |
| `PATCH /api/assets/{id}/condition` | Record a condition change. | `CanManageAssets` |
| `GET /api/assets/qr/{code}` | Resolve a scanned or entered code (404 for other organisations' codes). | `CanReadAssets` |
| `GET /api/assets/organization-code` | Organisation code prefix used in asset codes and QR labels. | `CanReadAssets` |
| `GET /api/assets/{id}/history` | Ordered, append-only lifecycle chronology. | `CanReadAssets` |
| `POST /api/assets/{id}/verify` | **Business operation:** record a physical verification and reconcile it against the register. | `CanVerifyAssets` |

## 9.3 Component B — Maintenance and Notifications

| Method and route | Purpose | Authorisation |
|---|---|---|
| `GET /api/maintenance` | List with filters by status, priority, type, asset, department, assignee, date range. | Read roles (Staff department-scoped) |
| `GET /api/maintenance/{id}` | Record detail with a short-lived signed photo URL. | Read roles |
| `GET /api/maintenance/my-reports` | The caller's own fault reports. | Staff, Inventory Officer |
| `POST /api/maintenance/faults` | Report a fault. | `CanRequestMaintenance` |
| `POST /api/maintenance/photos` | Upload a photo (MIME/size checked, stored privately). | `CanRequestMaintenance`, rate-limited |
| `POST /api/maintenance` | Create a maintenance record directly. | Inventory Officer, Administrator |
| `PUT /api/maintenance/{id}` | Amend type, priority, description. | `CanManageMaintenance` |
| `POST /api/maintenance/{id}/approve` | Approve and assign with an estimate. | `CanManageMaintenance` |
| `POST /api/maintenance/{id}/start` | Begin work; sets the asset to `UNDER_MAINTENANCE`. | `CanManageMaintenance` |
| `POST /api/maintenance/{id}/complete` | **Business operation:** close with actual cost and resulting condition (BR1–BR3, one transaction). | Inventory Officer |
| `POST /api/maintenance/{id}/cancel` | Cancel with a recorded reason. | `CanManageMaintenance` |
| `GET /api/notifications` · `GET unread-count` · `PATCH {id}/read` · `PATCH read-all` | The caller's in-app notifications. | `CanReadNotifications` |

## 9.4 Component C — Transfers and Disposals

| Method and route | Purpose | Authorisation |
|---|---|---|
| `POST /api/transfers` | Raise a transfer request. | `CanRequestTransfer` |
| `GET /api/transfers` · `GET /{id}` | List / read transfers. | `CanReadAssets` (Staff department-scoped) |
| `GET /api/assets/{assetId}/transfers` | Transfer history for one asset. | `CanReadAssets` |
| `POST /api/transfers/{id}/approve` | Approve; sets the asset `IN_TRANSIT`. | `CanApproveTransfer` |
| `POST /api/transfers/{id}/reject` | Reject with a reason; releases the asset. | `CanApproveTransfer` |
| `POST /api/transfers/{id}/confirm-receipt` | Confirm physical receipt; completes the ownership change. | `CanConfirmReceipt` |
| `POST /api/assets/{id}/condemn` | Condemn an asset with reason and evidence. | `CanRequestDisposal` |
| `POST /api/disposals` | Raise a disposal request for a condemned asset. | `CanRequestDisposal` |
| `GET /api/disposals` · `GET /{id}` | List / read disposal requests with precondition results. | `CanReadAssets` |
| `POST /api/disposals/{id}/approve` | **Business operation:** evaluate P1–P6 and separation of duties; dispose the asset. | `CanApproveDisposal` |
| `POST /api/disposals/{id}/reject` | Reject with a reason; asset returns to `CONDEMNED`. | `CanApproveDisposal` |
| `POST /api/disposals/{id}/request-revision` | Return for revision with comments. | `CanApproveDisposal` |

## 9.5 Component D — Verification, Audit and Reporting

| Method and route | Purpose | Authorisation |
|---|---|---|
| `GET /api/verification-campaigns` · `GET /{id}` | List / read campaigns. | `CanReadCampaigns` |
| `POST`, `PUT /{id}`, `DELETE /{id}` on `/api/verification-campaigns` | Manage campaigns; creation generates officer tasks. | `CanManageCampaigns` |
| `GET /api/verification-campaigns/{id}/report` · `/report/export` | Campaign report; PDF or CSV export. | `CanManageCampaigns` |
| `GET /api/verification-tasks` | The caller's verification tasks, ordered by due date. | `CanVerifyAssets` |
| `PATCH /api/verification-tasks/{id}/complete` | Complete a task with presence, location and condition assertions; mismatches raise discrepancies automatically. | `CanVerifyAssets` |
| `POST /api/verification-tasks/photos` | Upload discrepancy photo evidence (≤ 5 MB). | Auditor, Administrator, Inventory Officer |
| `POST /api/verification-tasks/{taskId}/discrepancies` | Raise a discrepancy manually. | Auditor, Administrator, Inventory Officer |
| `GET /api/discrepancies` | List discrepancies. | Auditor, Administrator |
| `PATCH /api/discrepancies/{id}/resolve` | **Business operation:** classify, evidence and close, correcting the register where required. | `CanResolveDiscrepancy` |
| `GET /api/audit-log` | Filterable, read-only audit trail. | `CanReadAuditLog` |
| `GET /api/reports/audit` · `/export` | Organisation-wide audit report; PDF or CSV export. | Auditor, Administrator |
| `GET /api/dashboard/summary` | Role-appropriate indicators (Staff department-scoped). | `CanReadAssets` |
| `GET /api/dashboard/charts` | Chart series. | Auditor, Administrator |

Inventory, maintenance and disposal reports are assembled in the web client from the paged list endpoints above, with the same filters.

## 9.6 Agentic Workflow

| Method and route | Purpose | Authorisation |
|---|---|---|
| `POST /api/agent-workflows` | Initiate an evaluation for an asset with an objective; runs Planner → Maintenance Analysis → Budget Analysis → Policy Compliance. | `CanInitiateWorkflow`, rate-limited |
| `GET /api/agent-workflows` · `GET /{id}` | List (status filter) / read workflows. | Inventory Officer, Auditor, Administrator |
| `GET /api/agent-workflows/{id}/execution-summary` | Auditable trace: plan, agent outputs, steps, validation, decision. | Inventory Officer, Auditor, Administrator |
| `POST /api/agent-workflows/{id}/run-maintenance-agent` · `run-budget-agent` · `run-policy-agent` | Re-run one node independently. | `CanInitiateWorkflow` |
| `POST /api/agent-workflows/{id}/evaluate` | Evaluate a proposed recommendation against policy. | `CanInitiateWorkflow` |
| `PATCH /api/agent-workflows/{id}/decide` | Approve, reject or request revision of a paused workflow, with a mandatory reason. | `CanApproveWorkflow` (Administrator only) |
| `GET /api/agent-tools/*`, `POST /api/agent-tools/compute-depreciation` | The eight read-only agent tools of Section 7.4 (also called in-process by the agents). | `CanReadAssets` |

## 9.7 System

| Method and route | Purpose | Authorisation |
|---|---|---|
| `GET /health` | Liveness and per-dependency status (database, identity provider). | Anonymous |
| `GET /swagger` | OpenAPI documentation. | Anonymous |
