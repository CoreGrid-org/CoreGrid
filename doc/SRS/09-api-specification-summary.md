# 9. API Specification Summary

The complete, authoritative contract is published as Swagger/OpenAPI at the deployed API. The tables below establish the required surface and the authorisation policy governing each operation. All list endpoints accept the standard paging, sorting, search and filter parameters described in Section 5.4.

## 9.1 Identity, Configuration and Users

| Method and route | Purpose | Policy |
|---|---|---|
| `GET /api/me` | Resolved profile: user, organisation, department, roles, effective permissions. | Authenticated |
| `GET /api/departments` | List departments. | Authenticated |
| `POST /api/departments` | Create a department. | `config:manage` |
| `PUT /api/departments/{id}` | Amend a department. | `config:manage` |
| `POST /api/departments/{id}/deactivate` | Deactivate, refused while active assets reference it. | `config:manage` |
| `GET, POST, PUT /api/locations` | Manage locations within a department. | read: authenticated; write: `config:manage` |
| `GET, POST, PUT /api/asset-categories` | Manage categories. | read: authenticated; write: `config:manage` |
| `GET, POST, PUT /api/asset-types` | Manage asset types. | read: authenticated; write: `config:manage` |
| `GET, POST, PUT, DELETE /api/asset-types/{id}/attributes` | Manage attribute definitions for a type. | `config:manage` |
| `GET, PUT /api/policies` | Read and set organisation policy thresholds. | `config:manage` |
| `GET /api/users` | List users in the organisation. | `user:manage` |
| `POST /api/users/invite` | Provision a user through ThunderID SCIM and create the mirror. | `user:manage` |
| `PUT /api/users/{id}` | Change department or role assignment. | `user:manage` |
| `POST /api/users/{id}/deactivate` | Deactivate a user locally and in the directory. | `user:manage` |

## 9.2 Component A — Assets

| Method and route | Purpose | Policy |
|---|---|---|
| `GET /api/assets` | List with search, filter, sort, page. | `asset:read` |
| `GET /api/assets/{id}` | Full record with type, attributes, computed residual value. | `asset:read` |
| `POST /api/assets` | Register an asset; validates custom attributes; generates code and QR payload. | `asset:create` |
| `PUT /api/assets/{id}` | Amend; writes history. | `asset:update` |
| `GET /api/assets/qr/{code}` | Resolve a scanned or entered code. | `asset:read` |
| `GET /api/assets/{id}/qr-label` | Printable label payload. | `asset:read` |
| `GET /api/assets/{id}/history` | Ordered lifecycle chronology. | `asset:read` |
| `POST /api/assets/{id}/verify` | Business operation — record a physical verification and reconcile. | `asset:verify` |
| `POST /api/assets/{id}/condition` | Record a condition change. | `asset:update` |

## 9.3 Component B — Maintenance

| Method and route | Purpose | Policy |
|---|---|---|
| `GET /api/maintenance` | List with filters by status, priority, assignee, asset, date range. | authenticated (scoped) |
| `GET /api/maintenance/{id}` | Record detail with attachments. | authenticated (scoped) |
| `POST /api/maintenance` | Report a fault or create a record. | `maintenance:request` |
| `PUT /api/maintenance/{id}` | Amend classification, priority, description. | `maintenance:manage` |
| `POST /api/maintenance/{id}/assign` | Approve and assign with an estimate. | `maintenance:manage` |
| `POST /api/maintenance/{id}/start` | Begin work; sets asset UNDER_MAINTENANCE. | `maintenance:manage` |
| `POST /api/maintenance/{id}/complete` | Business operation — close with actual cost and resulting condition. | `maintenance:manage` |
| `POST /api/maintenance/{id}/cancel` | Cancel with a recorded reason. | `maintenance:manage` |
| `POST /api/maintenance/{id}/attachments` | Upload photographic evidence. | `maintenance:request` |

## 9.4 Component C — Transfers and Disposals

| Method and route | Purpose | Policy |
|---|---|---|
| `GET, GET /{id}, POST /api/transfers` | List, read and raise transfer requests. | read: scoped; create: `transfer:request` |
| `POST /api/transfers/{id}/approve` | Approve; sets IN_TRANSIT. | `transfer:approve` |
| `POST /api/transfers/{id}/reject` | Reject with a reason. | `transfer:approve` |
| `POST /api/transfers/{id}/confirm-receipt` | Confirm physical receipt by scan; completes ownership change. | `transfer:confirm-receipt` |
| `POST /api/assets/{id}/condemn` | Condemn an asset with reason and evidence. | `disposal:request` |
| `GET, GET /{id}, POST /api/disposals` | List, read and raise disposal requests. | read: scoped; create: `disposal:request` |
| `POST /api/disposals/{id}/approve` | Business operation — validate preconditions and authorise disposal. | `disposal:approve` |
| `POST /api/disposals/{id}/reject` | Reject with a reason. | `disposal:approve` |
| `POST /api/disposals/{id}/request-revision` | Return for revision with comments. | `disposal:approve` |

## 9.5 Component D — Audit, Compliance and Reporting

| Method and route | Purpose | Policy |
|---|---|---|
| `GET, POST /api/campaigns` | List and create verification campaigns. | `audit:campaign-manage` |
| `GET /api/campaigns/{id}/tasks` | Verification task list, filterable to the caller. | `asset:verify` or `audit:campaign-manage` |
| `GET /api/campaigns/{id}/progress` | Verified, outstanding and discrepancy counts. | `audit:campaign-manage` |
| `GET, POST /api/discrepancies` | List and raise discrepancies. | read: `audit:log-read`; create: `asset:verify` |
| `POST /api/discrepancies/{id}/resolve` | Business operation — classify, evidence and close, correcting the register where required. | `audit:discrepancy-resolve` |
| `GET /api/audit-logs` | Filterable, read-only audit trail. | `audit:log-read` |
| `GET /api/reports/{type}` | Generate inventory, maintenance, disposal or audit reports with filters. | `report:generate` |
| `GET /api/reports/{type}/export` | Export the filtered report as PDF or CSV. | `report:generate` |
| `GET /api/dashboard` | Role-appropriate indicators and chart series. | authenticated (scoped) |

## 9.6 Agentic Workflow and System

| Method and route | Purpose | Policy |
|---|---|---|
| `POST /api/workflows/asset-evaluation` | Initiate an evaluation; returns a workflow identifier immediately. | `workflow:initiate` |
| `GET /api/workflows` | List workflows with status filters. | `workflow:read` |
| `GET /api/workflows/{id}` | Status, current step and outcome. | `workflow:read` |
| `GET /api/workflows/{id}/execution-summary` | Full auditable trace: plan, agent outputs, tool calls, validation, decision. | `workflow:read` |
| `POST /api/workflows/{id}/approve` | Approve a paused workflow; resumes and executes the authorised action. | `workflow:approve` |
| `POST /api/workflows/{id}/reject` | Reject with a mandatory reason; terminal. | `workflow:approve` |
| `POST /api/workflows/{id}/request-revision` | Return to analysis with reviewer comments. | `workflow:approve` |
| `GET /api/agent-tools/*` (internal) | The eight read-only tool endpoints of Section 7.4. | Agent service principal only |
| `GET /health` | Liveness and dependency status for database, agent service and identity provider. | Anonymous |
| `GET /swagger` | OpenAPI documentation. | Anonymous |
