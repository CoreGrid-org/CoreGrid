# 6. Functional Requirements

This section specifies the behaviour CoreGrid shall exhibit. Requirements are grouped by the business component that owns them, so that each of the four component owners can read a contiguous specification of their accountability. Requirements marked API are enforced by the backend irrespective of which client issues the request; where a capability is exposed through a particular client, that client is named.

Each business component satisfies the assignment's individual-component minimum: at least four meaningful API endpoints and at least one business-specific operation beyond basic create, read, update and delete. The business-specific operations are, respectively, asset verification, maintenance completion with cost reconciliation, disposal approval with evidence checks, and discrepancy resolution.

| Component | Owner | Requirement range | Business-specific operation beyond CRUD |
|---|---|---|---|
| A — Asset Registry & QR Identification | Student 1 | FR-021 to FR-032 | `POST /api/assets/{id}/verify` — records a physical verification event with condition and location assertion, and reconciles it against the register. |
| B — Maintenance Management | Student 2 | FR-033 to FR-042 | `POST /api/maintenance/{id}/complete` — closes a maintenance record with actual cost and resulting condition, and returns the asset to service. |
| C — Transfer & Disposal | Student 3 | FR-043 to FR-055 | `POST /api/disposals/{id}/approve` — validates evidence preconditions, authorises the irreversible disposal and transitions the asset to DISPOSED. |
| D — Audit & Compliance | Student 4 | FR-056 to FR-066 | `POST /api/discrepancies/{id}/resolve` — classifies, evidences and closes a discrepancy, updating the register where the resolution requires it. |

## 6.1 Identity, Access and Session (FR-001 – FR-009)

These requirements are cross-cutting: they are implemented once in the API and in the shared client infrastructure, and every other requirement in this section assumes them.

| ID | Requirement | Primary actor | Client | Priority |
|---|---|---|---|---|
| FR-001 | A user shall authenticate through ThunderID using the authorisation-code-with-PKCE flow initiated from the client they are using; CoreGrid shall never present a password entry field. | All users | React, Flutter | Must |
| FR-002 | The API shall validate every bearer token for signature, issuer and lifetime, and shall reject a request that fails any check with 401 without disclosing which check failed. | System | API | Must |
| FR-003 | The API shall resolve the authenticated user's `OrganizationId` from their local user-mirror record, keyed by the token's `sub` claim, and shall reject a request where `sub` does not resolve to an active local record. | System | API | Must |
| FR-004 | The API shall create or refresh a local user mirror record from token claims on the first authenticated request of a session, recording an audit event when a role changes. | System | API | Must |
| FR-005 | Every endpoint shall declare an authorisation policy; a request from a user without the required permission shall be denied with 403 and the denial recorded. | System | API | Must |
| FR-006 | Every query and command shall be scoped to the organisation of the authenticated user through a global query filter, so that no request can read or write another organisation's data. | System | API | Must |
| FR-007 | Both clients shall hide actions the signed-in user is not permitted to perform and shall additionally protect the corresponding routes and screens. | All users | React, Flutter | Must |
| FR-008 | Sign-out shall clear client state, revoke the refresh token and terminate the identity-provider session. | All users | React, Flutter | Must |
| FR-009 | A user whose local mirror record has been deactivated shall be denied access on their next request even if their access token remains valid. | Administrator | API | Must |

## 6.2 Organisation Structure Configuration (FR-010 – FR-015)

| ID | Requirement | Primary actor | Client | Priority |
|---|---|---|---|---|
| FR-010 | An Administrator shall create, amend and deactivate departments within their organisation, each with a unique code and a name. | Administrator | React | Must |
| FR-011 | An Administrator shall create, amend and deactivate locations, each belonging to a department and carrying a type such as store, workshop, office or ward. | Administrator | React | Must |
| FR-012 | A department or location that is referenced by an active asset shall not be deletable; it may only be deactivated, and deactivation shall be refused while active assets reference it. | System | API | Must |
| FR-013 | An Administrator shall invite a user into their organisation by email address and role, provisioning them through ThunderID, and shall assign them to a department. | Administrator | React | Must |
| FR-014 | An Administrator shall change a user's role or department assignment and deactivate a user; deactivated users shall be retained for historical reference and never hard-deleted. | Administrator | React | Must |
| FR-015 | An Administrator shall define organisation policy parameters used by lifecycle rules and by the Policy Agent — including the repair-cost-to-replacement-cost threshold, the minimum service life before disposal, and the maximum acceptable failure frequency. | Administrator | React | Must |

## 6.3 Asset Type and Attribute Configuration (FR-016 – FR-020)

These requirements implement the configurable platform model described in Section 3.5. They are the reason CoreGrid can serve a transport fleet and a hospital inventory from one codebase, and they are among the strongest engineering arguments the group has at the viva.

| ID | Requirement | Primary actor | Client | Priority |
|---|---|---|---|---|
| FR-016 | An Administrator shall create asset categories within their organisation as the top-level grouping for reporting. | Administrator | React | Must |
| FR-017 | An Administrator shall create asset types within a category, each with a name, code, a default useful-life in years for depreciation, and an optional default maintenance interval. | Administrator | React | Must |
| FR-018 | An Administrator shall define, for each asset type, an ordered set of custom attribute definitions specifying name, data type (text, number, date, boolean or single-select), required flag, validation rule and display order. | Administrator | React | Must |
| FR-019 | The API shall validate every asset's custom attribute values against the attribute definitions of its asset type on create and on update, rejecting missing required values, wrong data types and values failing the declared validation rule. | System | API | Must |
| FR-020 | Both clients shall render the asset detail form dynamically from the attribute definitions of the selected asset type, without any client-side knowledge of specific domains. | Officer | React, Flutter | Must |

**Why this matters at the viva**

FR-019 is the requirement that keeps configurability honest. If validation of custom attributes lived in the client, a new asset type would be a code change and the platform claim would be false. Because validation is derived at runtime from the attribute definitions in the database and enforced in the API, an administrator can introduce "Locomotive" with seven new attributes on a Tuesday afternoon and both clients handle it correctly without a deployment.

## 6.4 Component A — Asset Registry and QR Identification (FR-021 – FR-032)

Component A owns the asset master record and the physical identification mechanism that connects the register to reality. Every other component depends on it.

| ID | Requirement | Primary actor | Client | Priority |
|---|---|---|---|---|
| FR-021 | An Inventory Officer shall register an asset by selecting an asset type and supplying name, department, location, acquisition date, acquisition cost and all required custom attributes. | Officer | React | Must |
| FR-022 | The system shall generate a unique, human-readable asset code on registration using a configurable organisation prefix and a monotonic sequence, and shall guarantee uniqueness within the organisation by database constraint. | System | API | Must |
| FR-023 | The system shall generate a QR label payload encoding the asset code, and shall make a printable label available for download. | Officer | React | Must |
| FR-024 | An Inventory Officer or Department Staff member shall scan an asset QR label with the mobile application and shall be shown the authoritative asset record within three seconds of a successful decode. | Officer, Staff | Flutter | Must |
| FR-025 | The system shall accept manual entry of an asset code as an alternative to scanning, producing the identical result. | Officer, Staff | Flutter, React | Must |
| FR-026 | An Inventory Officer shall amend an asset's descriptive fields, custom attribute values, department and location, with every change recorded in the asset history. | Officer | React | Must |
| FR-027 | The system shall maintain an immutable, ordered history for each asset recording every state change, field amendment, verification, maintenance event, transfer, disposal action and agent recommendation, with actor and timestamp. | System | API | Must |
| FR-028 | Users shall search assets by code, name and custom attribute value, and filter by department, location, category, asset type, status and condition, with server-side sorting and pagination. | All users | React, Flutter | Must |
| FR-029 | An Inventory Officer shall record an asset's condition on a defined scale — New, Good, Fair, Poor, Unserviceable — with the change recorded in history. | Officer | React, Flutter | Must |
| FR-030 | The system shall compute residual value from acquisition cost, acquisition date and the useful life of the asset type using straight-line depreciation, exposing it on the asset record. | System | API | Must |
| FR-031 | An Inventory Officer shall perform a physical verification of an asset, asserting its presence, its actual location and its actual condition; the system shall compare the assertion against the register and raise a discrepancy where they differ. | Officer | Flutter | Must |
| FR-032 | The system shall prevent deletion of any asset that has lifecycle history; assets leave the active register only through the disposal workflow. | System | API | Must |

### Asset lifecycle state machine

```
                              ┌──────────────┐
        register ────────────▶│    ACTIVE    │◀──────────┐
                              └──┬───┬───┬───┘           │
                                 │   │   │               │ complete
            transfer requested   │   │   │ maintenance   │
                    ┌────────────┘   │   └───────────┐   │
                    ▼                │               ▼   │
         ┌────────────────────┐      │      ┌──────────────────┐
         │ TRANSFER_REQUESTED │      │      │ UNDER_MAINTENANCE│
         └─────────┬──────────┘      │      └──────────────────┘
            approve│  reject         │ condemn
                   ▼                 ▼
         ┌────────────────────┐   ┌──────────────┐
         │  IN_TRANSIT        │   │  CONDEMNED   │
         └─────────┬──────────┘   └──────┬───────┘
           confirm │                     │ disposal requested
            receipt│                     ▼
                   │            ┌─────────────────────┐
                   └───────────▶│ DISPOSAL_REQUESTED  │
                     back to    └──────┬──────────┬───┘
                      ACTIVE    approve│          │reject
                                       ▼          └────▶ back to CONDEMNED
                                ┌──────────────┐
                                │   DISPOSED   │   terminal — no further
                                └──────────────┘   transition permitted
```

Figure 6 — Asset lifecycle states. Every transition is guarded in the application layer; an invalid transition returns 409 and is never silently ignored.

### FR-024 — QR scan and asset lookup

| Attribute | Specification |
|---|---|
| Description | A field user opens the scanner from the mobile dashboard, points the device at an asset's QR label, and is taken directly to the asset detail screen with the actions permitted to their role. |
| Primary actor | Inventory Officer; Department Staff (read and report only) |
| Trigger | The user taps Scan Asset on the mobile dashboard. |
| Pre-conditions | The user is authenticated; camera permission has been granted or manual entry is used; the asset exists within the user's organisation. |
| Main flow | 1. The scanner opens with a live preview.<br>2. The device decodes a QR payload and extracts the asset code.<br>3. The client calls `GET /api/assets/qr/{code}`.<br>4. The API resolves the code within the caller's organisation and returns the asset with its type, custom attributes, current status, condition, location and recent history.<br>5. The detail screen renders, offering Verify, Report Damage, Request Maintenance and View History as permitted. |
| Alternative flows | A1 — Permission refused: the manual code entry field is offered instead.<br>A2 — Unreadable code after 10 seconds: guidance is shown and manual entry is offered.<br>A3 — Code not found in the caller's organisation: "Asset not found" is shown, with no indication of whether it exists elsewhere.<br>A4 — Network unavailable: an offline message is shown; no cached business data is displayed as though current. |
| Post-conditions | No business state changes. A read event is recorded in the access log. |
| Acceptance criteria | AC1 — A valid label decodes and resolves within 3 seconds under normal network conditions.<br>AC2 — A code belonging to another organisation returns 404, never 403 or a record.<br>AC3 — Manual entry of the same code produces a byte-identical response body.<br>AC4 — Department Staff see no Verify action, and a direct API call to verify returns 403. |
| Priority | Must |

## 6.5 Component B — Maintenance Management (FR-033 – FR-042)

Component B turns a fault observed in the field into a tracked, costed and closed piece of work, and produces the maintenance history on which the agentic evaluation depends.

| ID | Requirement | Primary actor | Client | Priority |
|---|---|---|---|---|
| FR-033 | A Department Staff member or Inventory Officer shall report a fault against an asset, supplying a description, an observed condition and an optional photograph. | Staff, Officer | Flutter, React | Must |
| FR-034 | The system shall attach the captured photograph to the maintenance record, compressing it and storing a reference; images shall be retrievable only by users permitted to read the record. | System | API | Should |
| FR-035 | An Inventory Officer shall create a maintenance record directly, classify it as corrective or preventive, and set its priority. | Officer | React | Must |
| FR-036 | An Inventory Officer or Administrator shall approve a requested maintenance record, assign it to a responsible officer and record an estimated cost. | Officer, Administrator | React | Must |
| FR-037 | The assigned officer shall progress a maintenance record through the defined status sequence, with each transition guarded so that only legal transitions are accepted. | Officer | React, Flutter | Must |
| FR-038 | An Inventory Officer shall complete a maintenance record by recording the actual cost, the work performed, the completion date and the resulting asset condition; the asset shall return to ACTIVE status. | Officer | React | Must |
| FR-039 | The system shall place an asset into UNDER_MAINTENANCE status when a maintenance record reaches IN_PROGRESS and shall prevent a transfer or disposal request while it remains there. | System | API | Must |
| FR-040 | The system shall maintain, per asset, a cumulative maintenance cost, a repair count and the date of the most recent repair, available to the register and to the agentic subsystem. | System | API | Must |
| FR-041 | The system shall schedule a preventive maintenance record when the maintenance interval configured on an asset type has elapsed since the last completed maintenance. | System | API | Should |
| FR-042 | Users shall list and filter maintenance records by status, priority, department, asset, assignee and date range, with sorting and pagination. | Officer, Auditor, Administrator | React, Flutter | Must |

```
   REQUESTED ──approve──▶ APPROVED ──start──▶ IN_PROGRESS ──complete──▶ COMPLETED
       │                     │                     │
       └──────cancel─────────┴─────────────────────┘──────▶ CANCELLED

   Guards:  approve   → requires maintenance:manage
            start     → requires an assignee and sets asset to UNDER_MAINTENANCE
            complete  → requires actual cost and resulting condition; returns asset to ACTIVE
            cancel    → permitted before COMPLETED only; requires a recorded reason
```

Figure 7 — Maintenance record states and transition guards.

### FR-038 — Complete a maintenance record

| Attribute | Specification |
|---|---|
| Description | The business-specific operation of Component B. Closing a maintenance record reconciles the estimate against actual expenditure, updates the asset's condition and cumulative cost history, and returns the asset to service. |
| Primary actor | Inventory Officer |
| Pre-conditions | The record is IN_PROGRESS; the caller holds `maintenance:manage`; the asset is UNDER_MAINTENANCE. |
| Inputs | Actual cost (non-negative decimal), work performed (free text, 10–2000 characters), completion date (not in the future, not before the start date), resulting condition (enumerated). |
| Processing | Within one database transaction: the record transitions to COMPLETED; the asset condition is updated; cumulative maintenance cost, repair count and last-repair date are recalculated; the asset returns to ACTIVE; an audit entry and an asset-history entry are written; a completion notification is queued. |
| Business rules | BR1 — Completion is rejected if actual cost exceeds the estimate by more than the organisation's configured variance tolerance without a recorded justification.<br>BR2 — If the resulting condition is Unserviceable, the asset is set to CONDEMNED rather than ACTIVE, and the disposal path becomes available.<br>BR3 — The transaction is atomic: a failure at any step leaves the record IN_PROGRESS and the asset UNDER_MAINTENANCE. |
| Acceptance criteria | AC1 — A completed record cannot be completed again; a second attempt returns 409.<br>AC2 — Cumulative cost after completion equals the previous cumulative cost plus the actual cost.<br>AC3 — Recording Unserviceable produces CONDEMNED, evidenced by an integration test.<br>AC4 — A forced failure of the notification step does not roll back the completion. |
| Priority | Must |

## 6.6 Component C — Transfer and Disposal (FR-043 – FR-055)

Component C governs the two ways an asset leaves its current custody: it moves to a different department, or it leaves the register entirely. Disposal is the system's only irreversible action and is therefore the action the agentic workflow pauses on.

### Transfer

| ID | Requirement | Primary actor | Client | Priority |
|---|---|---|---|---|
| FR-043 | An Inventory Officer shall raise a transfer request specifying the asset, the destination department, the destination location and a reason. | Officer | React, Flutter | Must |
| FR-044 | The system shall refuse a transfer request for an asset that is not ACTIVE, and shall state the blocking status. | System | API | Must |
| FR-045 | An Administrator shall approve or reject a transfer request, recording a decision reason; approval sets the asset to IN_TRANSIT. | Administrator | React | Must |
| FR-046 | An Inventory Officer at the destination shall confirm physical receipt by scanning the asset; confirmation moves ownership to the destination department and location and returns the asset to ACTIVE. | Officer | Flutter | Must |
| FR-047 | The system shall retain a complete transfer history per asset showing origin, destination, requester, approver, receiver and all timestamps. | System | API | Must |
| FR-048 | A transfer that has been approved but not confirmed within a configurable number of days shall be flagged on the administrator dashboard as outstanding. | System | API | Should |

### Disposal

| ID | Requirement | Primary actor | Client | Priority |
|---|---|---|---|---|
| FR-049 | An Inventory Officer shall condemn an asset, recording a reason and evidence; condemnation requires a recorded condition of Poor or Unserviceable. | Officer | React | Must |
| FR-050 | An Inventory Officer shall raise a disposal request against a condemned asset, specifying the proposed disposal method — transfer to another entity, auction, or destruction — and attaching supporting evidence. | Officer | React | Must |
| FR-051 | An Administrator shall approve or reject a disposal request; approval is permitted only when every evidence and policy precondition is satisfied and shall transition the asset to the terminal DISPOSED state. | Administrator | React | Must |
| FR-052 | The system shall require, before a disposal request may be approved, that the asset is CONDEMNED, that a valuation has been recorded, that the minimum service life configured for the asset type has elapsed, and that no maintenance record is open. | System | API | Must |
| FR-053 | A disposal request shall be capable of being returned for revision with recorded comments, without being rejected outright. | Administrator | React | Must |
| FR-054 | The system shall record the disposal outcome — method, date, proceeds where applicable and the authorising user — and shall retain the asset record permanently in the DISPOSED state. | System | API | Must |
| FR-055 | No operation shall transition an asset out of DISPOSED, and no user or agent shall be able to delete a disposed asset. | System | API | Must |

### FR-051 — Approve a disposal request

| Attribute | Specification |
|---|---|
| Description | The business-specific operation of Component C, and the high-impact action on which the agentic workflow pauses. It is the only operation in CoreGrid that produces an irreversible state. |
| Primary actor | Administrator (the only role holding `disposal:approve`). |
| Pre-conditions | A disposal request exists in PENDING_APPROVAL; the caller holds `disposal:approve`; the caller is not the user who raised the request. |
| Deterministic preconditions checked by the API | P1 — Asset status is CONDEMNED.<br>P2 — A valuation amount and valuation date are recorded.<br>P3 — Elapsed service life ≥ the minimum configured for the asset type.<br>P4 — No maintenance record for the asset is in REQUESTED, APPROVED or IN_PROGRESS.<br>P5 — No transfer for the asset is in TRANSFER_REQUESTED or IN_TRANSIT.<br>P6 — Where an agentic workflow is linked to the request, it has reached AWAITING_APPROVAL and its deterministic validation result is PASS. |
| Processing | Within one transaction: the request transitions to APPROVED; the asset transitions to DISPOSED; the disposal outcome record is written; an immutable audit entry captures the approver, the decision, the reason and every precondition value at the moment of approval; a notification is queued to the requesting officer and the department head. |
| Failure behaviour | Any unmet precondition returns 422 with a machine-readable code naming the specific precondition that failed. Nothing is written. The failure is recorded in the access log but not in the asset history, because no business event occurred. |
| Acceptance criteria | AC1 — Approval by a non-Administrator returns 403.<br>AC2 — Approval by the requesting officer returns 403 (separation of duties).<br>AC3 — Each of P1 to P6 has a dedicated negative test producing 422 and no state change.<br>AC4 — A second approval of the same request returns 409.<br>AC5 — After approval, every mutating endpoint for that asset returns 409.<br>AC6 — The audit entry contains the precondition snapshot and is not modifiable through any API. |
| Priority | Must |

## 6.7 Component D — Audit and Compliance (FR-056 – FR-066)

Component D provides the independent assurance layer: campaigns that verify the register against physical reality, discrepancies that record where the two diverged, and an immutable log that establishes who did what and when.

| ID | Requirement | Primary actor | Client | Priority |
|---|---|---|---|---|
| FR-056 | An Auditor or Administrator shall create a verification campaign with a name, a period, and a scope defined by department, location, category or asset type. | Auditor, Administrator | React | Must |
| FR-057 | The system shall generate the verification task list for a campaign from its scope, and shall assign tasks to the officers responsible for the in-scope locations. | System | API | Must |
| FR-058 | An assigned officer shall see their outstanding verification tasks on the mobile task list, ordered by due date. | Officer | Flutter | Must |
| FR-059 | An officer shall complete a verification task by scanning the asset and asserting presence, location and condition; the result shall be recorded against the campaign. | Officer | Flutter | Must |
| FR-060 | The system shall automatically raise a discrepancy when a verification assertion differs from the register, classifying it as Missing, Surplus, Location Mismatch, Condition Mismatch or Data Mismatch. | System | API | Must |
| FR-061 | An officer shall raise a discrepancy manually for a condition the automatic comparison cannot detect, with a description and a photograph. | Officer | Flutter | Should |
| FR-062 | An Auditor shall resolve a discrepancy by recording a resolution type, an explanation and any corrective action; where the resolution corrects the register, the system shall apply the correction and record it in asset history. | Auditor | React | Must |
| FR-063 | The system shall record an immutable audit log entry for every state-changing operation, capturing actor, organisation, entity, operation, before-and-after values for changed fields, timestamp and correlation identifier. | System | API | Must |
| FR-064 | Audit log entries shall be readable by Auditors and Administrators, filterable by entity, actor, operation and date range, and shall not be editable or deletable through any API. | Auditor, Administrator | React | Must |
| FR-065 | An Auditor shall generate a campaign completion report showing assets in scope, verified, outstanding, and discrepancies by classification and resolution status, exportable as PDF or CSV. | Auditor | React | Must |
| FR-066 | The system shall report campaign progress in real time — verified count, outstanding count and discrepancy count — on the audit dashboard. | Auditor, Administrator | React | Must |

### FR-062 — Resolve a discrepancy

| Attribute | Specification |
|---|---|
| Description | The business-specific operation of Component D. Resolution is where an audit finding is converted into either a correction of the register or a recorded, justified acceptance of the difference. |
| Primary actor | Auditor |
| Pre-conditions | The discrepancy is OPEN or UNDER_REVIEW; the caller holds `audit:discrepancy-resolve`. |
| Resolution types | REGISTER_CORRECTED — the register was wrong and is amended.<br>ASSET_RELOCATED — the asset was moved without a transfer record; ownership data is corrected and a retrospective note is added.<br>CONDITION_UPDATED — the recorded condition is amended to the verified condition.<br>WRITTEN_OFF — the asset cannot be located and is escalated to condemnation.<br>NO_ACTION — the difference is explained and accepted, with a mandatory justification of at least 20 characters. |
| Processing | Within one transaction: the discrepancy transitions to RESOLVED with type, explanation and resolver; where the type corrects the register, the corresponding asset fields are updated and an asset-history entry records the correction as audit-driven; an audit log entry is written; the campaign discrepancy counters are recalculated. |
| Business rules | BR1 — An Auditor may resolve a discrepancy but may not otherwise amend an asset; the correction is possible only as a resolution outcome, and it is recorded as such.<br>BR2 — WRITTEN_OFF requires the asset to have been verified Missing in at least one completed verification.<br>BR3 — A resolved discrepancy cannot be reopened; a new discrepancy must be raised. |
| Acceptance criteria | AC1 — Resolution by an Inventory Officer returns 403.<br>AC2 — REGISTER_CORRECTED updates the asset and writes exactly one asset-history entry attributed to the audit resolution.<br>AC3 — NO_ACTION without a justification of the required length returns 400.<br>AC4 — Campaign counters after resolution match a recomputation from the underlying records. |
| Priority | Must |

## 6.8 Agentic Decision Support — Functional View (FR-067 – FR-076)

These requirements state what a user experiences of the agentic subsystem. Section 7 specifies how the subsystem behaves internally, what each agent is accountable for, and how its state, validation, security and failure modes are controlled.

| ID | Requirement | Primary actor | Client | Priority |
|---|---|---|---|---|
| FR-067 | An Inventory Officer or Administrator shall initiate an asset lifecycle evaluation for a specific asset, stating the objective, and shall receive a workflow identifier immediately without waiting for completion. | Officer, Administrator | React, Flutter (Officer) · React only (Administrator) | Must |
| FR-068 | The system shall refuse to initiate an evaluation for an asset already in a terminal state, or where an evaluation for the same asset is already running. | System | API | Must |
| FR-069 | A user shall view the status of a workflow: its current step, the agents that have completed, and the outcome or failure. | Officer, Auditor, Administrator | React, Flutter (Officer) · React only (Auditor, Administrator) | Must |
| FR-070 | The React application shall display the full execution summary of a workflow — the plan, each agent's structured output, every tool call with its timing, the validation result and the recommendation with its supporting factors. | Administrator, Auditor | React | Must |
| FR-071 | Where a workflow recommends a high-impact action, it shall pause and shall present an approval request to an Administrator; no business state shall change while it is paused. | System | API | Must |
| FR-072 | An Administrator shall approve, reject or request revision of a paused workflow, recording a mandatory decision reason. | Administrator | React | Must |
| FR-073 | On approval, the system shall resume the workflow from its persisted checkpoint and shall execute the authorised action through the ordinary business service, subject to the same rules and audit logging as a manual action. | System | API | Must |
| FR-074 | On rejection, the workflow shall terminate as REJECTED with the reason recorded, and no business state shall change. | System | API | Must |
| FR-075 | On a revision request, the workflow shall re-enter the analysis phase with the reviewer's comments as additional context, and the revision count shall be capped at two before the workflow terminates for manual handling. | System | API | Must |
| FR-076 | The Flutter application shall display the outcome of an evaluation the officer initiated, including the recommendation and its approval status, closing the cross-platform loop. | Officer | Flutter | Must |

## 6.9 Notification (FR-077 – FR-080)

| ID | Requirement | Primary actor | Client | Priority |
|---|---|---|---|---|
| FR-077 | The system shall send a transactional email when a maintenance record is assigned, when a transfer requires approval, when a disposal requires approval, when an agentic workflow requires approval, and when an approval decision is made. | System | API | Must |
| FR-078 | Notification dispatch shall not participate in the business transaction; a delivery failure shall be logged and retried but shall never roll back or block the business operation. | System | API | Must |
| FR-079 | Notification content shall contain only the minimum personal data necessary — recipient name, asset code, action required and a link — and shall never contain tokens, credentials or full asset records. | System | API | Must |
| FR-080 | Users shall view their recent notifications in both clients, with unread state. | All users | React, Flutter | Should |

## 6.10 Dashboard, Analytics and Reporting (FR-081 – FR-086)

| ID | Requirement | Primary actor | Client | Priority |
|---|---|---|---|---|
| FR-081 | The React dashboard shall present role-appropriate indicators: total and active assets, assets under maintenance, pending transfers, pending disposals, open discrepancies and workflows awaiting approval. | All users | React | Must |
| FR-082 | The dashboard shall present at least three visualisations: assets by department, assets by condition, and maintenance cost by month. | Administrator, Auditor | React | Must |
| FR-083 | The Flutter dashboard shall present a task-focused summary: assets to verify, maintenance assigned to the user, and transfers awaiting their confirmation. | Officer, Staff | Flutter | Must |
| FR-084 | Users shall generate an asset inventory report, a maintenance report, a disposal report and an audit campaign report, each filterable by date, department, category, status and condition. | Officer, Auditor, Administrator | React | Must |
| FR-085 | Reports shall be exportable as PDF and CSV, and the export shall reflect the filters applied on screen. | Auditor, Administrator | React | Must |
| FR-086 | Every dashboard figure and report shall be computed within the caller's organisation and restricted to the departments their role permits them to see. | System | API | Must |
