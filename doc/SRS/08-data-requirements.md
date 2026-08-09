# 8. Data Requirements

## 8.1 Conceptual Data Model

```
                        ┌───────────────┐
                        │ Organizations │  (mirrors an ThunderID sub-org)
                        └───┬───────┬───┘
           ┌────────────────┘       └────────────────┐
           ▼                                         ▼
    ┌─────────────┐                            ┌───────────┐
    │ Departments │───────────┐                │   Users   │ (mirror; no
    └──────┬──────┘           │                └─────┬─────┘  credentials)
           ▼                  │                      │
    ┌─────────────┐           │                      │ actor on every
    │  Locations  │           │                      │ lifecycle record
    └──────┬──────┘           │                      │
           │   ┌──────────────────────┐              │
           │   │   AssetCategories    │              │
           │   └──────────┬───────────┘              │
           │              ▼                          │
           │   ┌──────────────────────┐              │
           │   │     AssetTypes       │              │
           │   └──────────┬───────────┘              │
           │              ▼                          │
           │   ┌────────────────────────────────┐    │
           │   │  AssetAttributeDefinitions     │    │
           │   └────────────────┬───────────────┘    │
           │                    │                    │
           └────────┬───────────┘                    │
                    ▼                                │
            ┌───────────────┐   1:N   ┌──────────────────────────┐
            │    Assets     │────────▶│  AssetAttributeValues    │
            └───┬─┬─┬─┬─┬─┬─┘         └──────────────────────────┘
                │ │ │ │ │ │
   ┌────────────┘ │ │ │ │ └──────────────┐
   ▼              ▼ │ │ ▼                ▼
 Maintenance  Transfers│ AssetHistory  AgentWorkflows
 Records          │    │        │              │
                  │    ▼        │              ├──▶ AgentExecutionSteps
                  │ Disposals   │              └──▶ AgentApprovals
                  │             │
                  ▼             ▼
        AuditVerifications   Discrepancies ◀── VerificationCampaigns

        AuditLogs   (append-only; references organisation, user, entity)
        Notifications (queued dispatch records)
        OrganizationPolicies (thresholds consumed by rules and the Policy Agent)
```

Figure 9 — Conceptual entity relationships. The full ER diagram with attributes and cardinalities accompanies the technical report.

## 8.2 Entity Inventory

| Entity | Purpose | Key relationships | Owner |
|---|---|---|---|
| Organizations | Tenant record mirroring an ThunderID sub-organisation; the root of every query filter. | 1:N Departments, Users, AssetCategories, OrganizationPolicies | Shared |
| Users | Local mirror of an ThunderID identity; holds no credentials. | N:1 Organization, N:1 Department; referenced by every lifecycle record | Shared |
| Departments | Business unit that owns assets and holds budget. | N:1 Organization; 1:N Locations, Assets, Users | Shared |
| Locations | Physical place where an asset is held. | N:1 Department; 1:N Assets | Student 1 |
| AssetCategories | Top-level grouping for reporting. | N:1 Organization; 1:N AssetTypes | Student 1 |
| AssetTypes | Configurable classification carrying useful life and maintenance interval. | N:1 AssetCategory; 1:N AssetAttributeDefinitions, Assets | Student 1 |
| AssetAttributeDefinitions | Declares a custom field for an asset type. | N:1 AssetType; 1:N AssetAttributeValues | Student 1 |
| Assets | The asset master record and lifecycle status. | N:1 AssetType, Department, Location; 1:N all lifecycle entities | Student 1 |
| AssetAttributeValues | The value an asset holds for one attribute definition. | N:1 Asset, N:1 AssetAttributeDefinition | Student 1 |
| AssetHistory | Append-only chronology of everything that happened to an asset. | N:1 Asset, N:1 User | Student 1 |
| MaintenanceRecords | A unit of maintenance work with status, cost and outcome. | N:1 Asset, N:1 User (reporter, assignee) | Student 2 |
| MaintenanceAttachments | Photographic evidence attached to a maintenance record. | N:1 MaintenanceRecord | Student 2 |
| AssetTransfers | A movement of an asset between departments or locations. | N:1 Asset, Department (from, to), User (requester, approver, receiver) | Student 3 |
| DisposalRequests | A proposal to remove an asset from the register. | N:1 Asset, N:1 User (requester, approver) | Student 3 |
| VerificationCampaigns | A scoped, time-bound verification exercise. | N:1 Organization; 1:N AuditVerifications | Student 4 |
| AuditVerifications | One officer's assertion about one asset during a campaign. | N:1 Campaign, Asset, User | Student 4 |
| Discrepancies | A recorded divergence between register and reality. | N:1 AuditVerification, Asset, User (raiser, resolver) | Student 4 |
| AuditLogs | Append-only record of every state-changing operation. | N:1 Organization, User; polymorphic entity reference | Student 4 |
| OrganizationPolicies | Configured thresholds consumed by rules and by the Policy Agent. | N:1 Organization, optional N:1 AssetType | Student 4 |
| AgentWorkflows | Durable workflow state. | N:1 Asset, User; 1:N AgentExecutionSteps, AgentApprovals | Shared |
| AgentExecutionSteps | One node execution within a workflow. | N:1 AgentWorkflow | Shared |
| AgentApprovals | A human decision on a paused workflow. | N:1 AgentWorkflow, N:1 User | Student 4 |
| Notifications | A queued or dispatched notification. | N:1 Organization, N:1 User | Student 2 |

## 8.3 Data Requirements

| ID | Requirement | Priority |
|---|---|---|
| DR-01 | The schema shall be normalised to third normal form except where denormalisation is deliberately applied for a documented performance reason. | Must |
| DR-02 | Every table shall have a primary key; UUID keys shall be used for entities exposed through the API so that identifiers are not enumerable. | Must |
| DR-03 | Every relationship shall be enforced by a foreign key with an explicit delete behaviour; cascade delete shall not be used on any entity carrying history. | Must |
| DR-04 | Every organisation-scoped table shall carry `OrganizationId` with a non-clustered index and a global query filter. | Must |
| DR-05 | Asset codes shall be unique within an organisation, enforced by a composite unique constraint on `(OrganizationId, AssetCode)`. | Must |
| DR-06 | Status columns shall be constrained to their permitted values by check constraints in addition to application-layer enforcement. | Must |
| DR-07 | Monetary values shall use `numeric(18,2)`; timestamps shall use `timestamptz` and shall be stored in UTC. | Must |
| DR-08 | Indexes shall exist on all foreign keys and on the columns used by the standard list filters: asset status, condition, department, asset type, maintenance status, campaign, workflow status and audit timestamp. | Must |
| DR-09 | Every table shall carry `CreatedAt`, `UpdatedAt`, `CreatedBy` and `UpdatedBy`, populated automatically by the persistence layer rather than by callers. | Must |
| DR-10 | Multi-entity operations — maintenance completion, transfer confirmation, disposal approval, discrepancy resolution and workflow resumption — shall execute within a single database transaction. | Must |
| DR-11 | Concurrent modification of the same asset shall be detected using PostgreSQL's `xmin` system column as an EF Core concurrency token, returning 409 with the current server state rather than silently overwriting. | Must |
| DR-12 | Audit log rows and asset history rows shall be append-only; no API path shall update or delete them, and this shall be covered by an automated test. | Must |
| DR-13 | Schema evolution shall occur only through EF Core migrations committed to the repository. | Must |
| DR-14 | Seed data shall create one demonstration organisation, four users covering all roles, three departments, six locations, three categories, five asset types with attribute definitions, at least forty assets with populated custom attributes, and enough maintenance history for the agentic evaluation to produce a meaningful result. | Must |
| DR-15 | Disposed assets and deactivated users shall be retained permanently; no business entity carrying history shall be hard-deleted. | Must |

## 8.4 Custom Attribute Storage Strategy

Two approaches were considered for the configurable attributes described in Section 3.5, and the decision is recorded in ADR-006.

| Option | Advantages | Disadvantages |
|---|---|---|
| A — Single JSONB column on Assets | One row per asset; no join on read; trivially flexible; PostgreSQL GIN indexing supports containment queries. | Referential integrity between a value and its definition cannot be enforced by the database; a definition rename requires a data migration; typed filtering is more awkward. |
| B — AssetAttributeValues table (attribute-value model) | Every value is bound by foreign key to its definition; typed querying and filtering are natural; validation state is inspectable in SQL. | One row per attribute per asset; a join or pivot is required to render an asset; more rows to index. |

CoreGrid adopts Option B. The decisive consideration is that FR-019 requires server-side validation of every custom value against its definition, and FR-028 requires search by custom attribute value. A foreign key from every value to its definition makes both natural and makes an orphaned or mistyped value impossible at the storage layer. The row count is not a concern at the scale the platform targets: a hundred thousand assets with a dozen attributes each is well within PostgreSQL's comfortable range with a composite index on `(AssetId, AttributeDefinitionId)` and a secondary index on `(AttributeDefinitionId, Value)`.

JSONB is nevertheless used, deliberately, for workflow state — the plan, agent outputs, tool call traces and validation results in Section 7.5 — where the shape genuinely varies between runs, where no referential integrity is meaningful, and where the data is written once and read whole. Using both mechanisms, each where its properties fit, is a stronger engineering position than applying one uniformly.

## 8.5 Data Protection and Retention

- The only personal data CoreGrid holds is the user mirror: subject identifier, name, email address, organisation, department and role. No national identifier, address, telephone number or financial detail is collected.
- Personal data is collected for one declared purpose — attributing actions and routing notifications — and is not used for any other purpose. This satisfies the purpose-limitation and data-minimisation obligations of the Personal Data Protection Act No. 9 of 2022.
- Audit records referencing a user are retained for the life of the deployment because they constitute the evidentiary trail. Where a deletion request must be honoured, the user record is anonymised — name and email replaced with a tombstone — while the subject reference and the audit trail remain intact.
- Photographic attachments are retained for the life of the maintenance record and are accessible only to users permitted to read that record.
- Data shared with the email provider is limited to recipient address, recipient name, asset code and the action required.
- No personal data is sent to the model provider. Agent tool responses carry asset and financial data; user names and email addresses are excluded from every tool response schema.
