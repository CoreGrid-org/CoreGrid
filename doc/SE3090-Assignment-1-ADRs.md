# CoreGrid — Architecture Decision Records

**Project:** CoreGrid — Intelligent Asset Lifecycle Management Platform  
**Module:** SE3090 – Software Engineering Frameworks  
**Status convention:** Accepted decisions are approved for the baseline implementation. Superseded decisions are retained for traceability only.

---

## ADR-001 — Single Authoritative ASP.NET Core API

| Field | Value |
|---|---|
| ADR ID | ADR-001 |
| Decision title | Use one authoritative ASP.NET Core API |
| Status | Accepted |
| Date / owner | 2026-08-01 / Hasitha Erandika |

### 1. Context

CoreGrid has a React management application, a field-oriented mobile client, PostgreSQL storage, external identity, notifications, object storage, and agentic decision support. Both clients must apply identical lifecycle rules, validation, organisation isolation, and authorisation. Duplicating rules in clients or allowing direct database access would create inconsistent behaviour and security bypasses.

### 2. Decision drivers

| Driver | Weight | Reason |
|---|---:|---|
| Consistent business-rule enforcement | 30% | Asset lifecycle transitions must be identical for every client. |
| Security and organisation isolation | 25% | No client may bypass policy checks or global query filters. |
| Maintainability for a four-person team | 20% | A comprehensible dependency structure is required. |
| Testability and auditability | 15% | Rules, transitions, and audit emission must be verifiable centrally. |
| Integration simplicity | 10% | Third-party credentials must remain server-side. |

### 3. Options considered

| Option | Strengths | Limitations |
|---|---|---|
| Single layered ASP.NET Core API | Centralises security, validation, transactions, and audit logic; supports both clients consistently. | Requires disciplined layering and a broad but coherent backend. |
| Direct client-to-database access | Fast initial prototyping and fewer API endpoints. | Exposes data access, duplicates rules, prevents reliable authorisation/auditing, and is unsuitable for the required security model. |
| Separate backend-for-frontend services | Allows tailored client endpoints. | Duplicates policy/rule logic and increases deployment/coordination cost. |
| Microservices by business component | Independent scaling and deployment potential. | Excessive operational complexity for the baseline and difficult distributed transactions. |

### 4. Decision

CoreGrid will use one layered ASP.NET Core Web API as the only authoritative public backend. Controllers, DTOs, application services, domain entities, and infrastructure adapters will be separated by dependency direction.

### 5. Rationale

The API offers the strongest fit for the mandatory ASP.NET Core technology stack and ensures that web/client behaviour cannot diverge on important decisions such as disposal, transfer approval, and agent workflow execution. It also provides a single place to validate JWTs, resolve organisation scope, apply policies, start transactions, and write audit records.

### 6. Consequences

- Positive: one source of truth for business rules, policy authorisation, and auditability.
- Positive: client applications remain unprivileged and can evolve independently.
- Trade-off: API changes require careful backward-compatible DTO and endpoint management.
- Responsibility: maintain controller/service/domain boundaries and test each protected endpoint.

### 7. Review conditions

Review if sustained independent scaling needs, separate release cadences, or component ownership boundaries cannot be served by the modular API without harming reliability or delivery speed.

---

## ADR-003 — React State Management with TanStack Query and Zustand

| Field | Value |
|---|---|
| ADR ID | ADR-003 |
| Decision title | Separate React server state from client/UI state |
| Status | Accepted |
| Date / owner | 2026-08-06 / Jayashan Guruge |

### 1. Context

The React application displays paginated assets, reports, workflow execution details, configuration data, and role-sensitive views. Most state is API-owned data that needs caching, invalidation, retry/error handling, and refresh after mutations. A smaller amount of local state covers UI preferences and session-adjacent presentation concerns.

### 2. Decision drivers

| Driver | Weight | Reason |
|---|---:|---|
| Correct caching and invalidation of API data | 35% | Asset and workflow updates must appear reliably after mutations. |
| Low implementation complexity | 25% | The team needs an approach appropriate to the baseline schedule. |
| Clear separation of responsibilities | 20% | API data should not be confused with local UI state. |
| Testability | 10% | Loading, error, and mutation states need predictable tests. |
| Bundle and maintenance impact | 10% | The solution should remain lightweight and feature-oriented. |

### 3. Options considered

| Option | Strengths | Limitations |
|---|---|---|
| TanStack Query + Zustand | Purpose-built server cache plus lightweight local state; clear boundaries; mutation invalidation support. | Team must understand two complementary tools. |
| Redux Toolkit for all state | Mature ecosystem and explicit actions/reducers. | More boilerplate for API cache concerns and heavier for small UI state. |
| React Context only | Built-in and simple for static/global values. | Not a complete cache, mutation, retry, or invalidation solution. |
| Component-local state for server data | Minimal initial setup. | Duplicate requests, stale data, and inconsistent loading/error handling become likely. |

### 4. Decision

Use TanStack Query for all remote/server state and Zustand only for lightweight local client and UI state. React Router owns route state.

### 5. Rationale

This division matches CoreGrid's dominant state pattern: assets, reports, permissions, configuration, and workflow records belong to the server. TanStack Query directly handles caching, refetching, invalidation, and asynchronous UI states. Zustand keeps local concerns small without imposing reducer boilerplate.

### 6. Consequences

- Positive: consistent loading, error, cache, and mutation behaviour across feature pages.
- Positive: components can focus on rendering and user interaction.
- Trade-off: developers must decide deliberately whether a value is server state or local state.
- Risk: stale views may occur if a mutation misses its invalidation key.
- Responsibility: define query keys consistently and test loading, failure, and post-mutation refresh states.

### 7. Review conditions

Review if client-side workflows become substantially more complex, if offline conflict resolution is introduced, or if cross-feature local state becomes too large for a lightweight store.

---

## ADR-006 — Relational Attribute-Value Storage and JSONB Workflow State

| Field | Value |
|---|---|
| ADR ID | ADR-006 |
| Decision title | Use relational custom attributes and JSONB for variable workflow artefacts |
| Status | Accepted |
| Date / owner | 2026-08-07 / Jayashan Guruge and Hasitha Erandika |

### 1. Context

CoreGrid must support different asset domains without code changes. Asset types may define required text, number, date, boolean, or select attributes, and users must search/filter by their values. At the same time, agent workflow plans, structured findings, tool traces, and validation artefacts have variable shape and are typically written/read as one execution record.

### 2. Decision drivers

| Driver | Weight | Reason |
|---|---:|---|
| Referential integrity and validation | 30% | Attribute values must correspond to valid definitions. |
| Dynamic search/query support | 25% | The system must filter assets by custom values. |
| Flexibility for workflow artefacts | 20% | Agent output shape legitimately varies between runs. |
| PostgreSQL compatibility and performance | 15% | The mandated database supports relational indexes and JSONB. |
| Migration/operational simplicity | 10% | The design must be maintainable by the project team. |

### 3. Options considered

| Option | Strengths | Limitations |
|---|---|---|
| Attribute-value tables for assets; JSONB for workflow state | Strong foreign keys and typed filtering for assets; flexible whole-record workflow artefacts. | Requires joins/pivoting when rendering asset attributes. |
| JSONB for both | Flexible schema and simple single-row asset storage. | Weak database integrity between values and definitions; harder typed filtering/validation. |
| Fixed typed columns per asset domain | Strong typing and straightforward queries. | Every new domain/attribute requires schema and code change, violating configurability. |
| Separate document database for workflows | Flexible documents and potential independent scaling. | Adds infrastructure and consistency complexity without baseline benefit. |

### 4. Decision

Store configurable asset data in `AssetAttributeDefinitions` and `AssetAttributeValues` relational tables. Store variable agent plan/output/tool/validation artefacts as JSONB within the persisted workflow model.

### 5. Rationale

Relational storage directly supports FR-019 validation and FR-028 filtering through foreign keys and indexes. Workflow state has no equally meaningful fixed relational shape and is read primarily as a complete execution trace; JSONB is better suited to it. Using each model where it fits is more defensible than forcing all data into one representation.

### 6. Consequences

- Positive: asset attributes retain integrity and are searchable/indexable.
- Positive: workflow state can evolve without frequent schema migrations.
- Trade-off: asset reads require joins; workflow JSONB requires careful schema validation at application boundaries.
- Risk: unbounded JSONB growth can affect storage and query performance.
- Responsibility: index common relational filters, validate workflow schemas, and persist only structured artefacts—not prompts, tokens, or chain-of-thought.

### 7. Review conditions

Review if workflow artefacts require frequent field-level reporting/querying, or if custom-attribute queries show unacceptable performance at the target dataset size.

---

## ADR-009 — S3-Compatible Object Storage for Maintenance Evidence

| Field | Value |
|---|---|
| ADR ID | ADR-009 |
| Decision title | Store maintenance photographs in S3-compatible object storage behind an abstraction |
| Status | Accepted |
| Date / owner | 2026-08-17 / Seneja Ramanayaka |

### 1. Context

Maintenance fault reports may include photographic evidence. Images must be retained with the maintenance record, restricted to authorised users, and protected from public bucket access. Storing binary files directly in PostgreSQL would inflate operational database size and complicate delivery, while the baseline must support both CoreGrid-operated and self-hosted deployments.

### 2. Decision drivers

| Driver | Weight | Reason |
|---|---:|---|
| Security and controlled retrieval | 30% | Images may expose operationally sensitive assets/locations. |
| Portability for self-hosted deployments | 25% | Customers may use their own compatible storage. |
| Database performance and backup size | 20% | Binaries should not overload transactional data storage. |
| Simplicity of integration | 15% | The baseline needs one backend-controlled integration pattern. |
| Cost and operational suitability | 10% | Storage should be economical for evidence files. |

### 3. Options considered

| Option | Strengths | Limitations |
|---|---|---|
| Cloudflare R2 by default / any S3-compatible endpoint for self-hosting | S3-compatible, portable, controlled through one abstraction. | Requires bucket configuration and credentials. |
| AWS S3 only | Mature service and ecosystem. | Imposes a provider dependency on self-hosted customers. |
| Azure Blob Storage only | Strong Azure integration. | Same portability/vendor-lock-in concern. |
| PostgreSQL `bytea` | Transactionally adjacent to records and simple at small scale. | Increases database size, backup cost, and application serving burden. |

### 4. Decision

Use Cloudflare R2 as the default CoreGrid-operated object store and permit any S3-compatible storage endpoint for self-hosted deployments. Access it only through `IBlobStorageService` in the ASP.NET Core backend.

### 5. Rationale

The approach keeps storage credentials and access control on the server, maintains portability, and avoids coupling large binary files to PostgreSQL. The `IBlobStorageService` abstraction aligns with the API's dependency-inversion approach and permits a provider replacement without changing business services.

### 6. Consequences

- Positive: better database hygiene and portable storage integration.
- Positive: no client receives bucket credentials or direct upload authority.
- Trade-off: object-store availability is a separate operational dependency.
- Risk: incorrectly configured bucket permissions could expose content.
- Responsibility: validate MIME type/size, re-encode uploads, persist only an opaque `StorageKey`, issue authorised short-lived signed URLs, and keep buckets private.

### 7. Review conditions

Review if evidence volume, retention requirements, sovereign-hosting requirements, or provider costs require another S3-compatible provider or a self-hosted object-store deployment.

---

## ADR-010 — In-Process .NET Agent Orchestrator with Four Specialised Nodes

| Field | Value |
|---|---|
| ADR ID | ADR-010 |
| Decision title | Run the agentic workflow in the ASP.NET Core API with four specialised agents |
| Status | Accepted; supersedes ADR-005 (LangGraph) |
| Date / owner | 2026-09-15 / Hasitha Erandika |

### 1. Context

The assignment requires a stateful workflow with at least four distinct specialised agents. CoreGrid must evaluate an asset lifecycle decision, persist its execution history, use controlled read-only tools, validate recommendations deterministically, resist prompt injection, and pause for authorised human approval before any high-impact action. The original LangGraph-based approach introduced a separate Python runtime; the revised target architecture must be simpler for a self-hosted baseline.

### 2. Decision drivers

| Driver | Weight | Reason |
|---|---:|---|
| Safety, policy control, and human approval | 30% | Agents may advise but cannot independently change business data. |
| Durable state and restart recovery | 20% | Workflows must resume from persisted checkpoints. |
| Operational simplicity | 20% | M0 self-hosted deployments should minimise runtime/services. |
| Clear agent boundaries and assessment evidence | 15% | Each student's specialised contribution must be visible and testable. |
| Model cost and provider portability | 15% | Model use should be limited and replaceable. |

### 3. Options considered

| Option | Strengths | Limitations |
|---|---|---|
| In-process .NET orchestrator with `IAgentNode` implementations | One deployable, shared security/transaction boundary, explicit typed contracts, easy API integration. | Team must implement graph/checkpoint behaviour carefully. |
| Permanent Python/LangGraph service split | Mature graph primitives and separate runtime. | Adds deployment/security surface and cross-service operational complexity. |
| One centralised Python service for all agents | Centralises agent functionality. | Places all agent logic outside the primary API and increases service coupling. |
| Linear prompt chain | Simple to build initially. | Insufficient explicit delegation, state, validation, tool control, and approval semantics. |

### 4. Decision

Run a single in-process `AgentWorkflowService` in the ASP.NET Core API. It orchestrates four separate `IAgentNode` implementations—Planner, Maintenance Analysis, Budget Analysis, and Policy. Use a provider-neutral `IModelClient` only where the node meets the model-use criterion; in the baseline this is the Planner node only.

### 5. Rationale

This option satisfies the assignment's agent-delegation requirement while preserving CoreGrid's rule that the API, rather than an agent, decides and executes state changes. It removes a separate runtime from the self-hosted baseline, limits inference cost, and makes tool allow-lists, organisation scoping, persistence, deterministic validation, and human approval part of one reviewed security boundary.

### 6. Consequences

- Positive: four auditable, independently testable agent responsibilities in one deployable.
- Positive: checkpoints, execution steps, validation results, and approvals persist alongside domain data.
- Positive: fewer model calls and no client access to model credentials.
- Trade-off: the API contains workflow orchestration complexity and must remain non-blocking/resilient.
- Risk: a model/tool failure can delay a workflow; therefore every run is time-bounded and failure-safe.
- Responsibility: enforce per-agent allow-lists, read-only tools, JSON-schema validation, organisation scope, timeout/retry limits, 120-second total bound, approval gate, and GC-01 to GC-12 evaluation.

### 7. Review conditions

Review if workflow volume requires independent scaling, if multiple model-calling agents become necessary, or if a proven orchestration framework offers material safety/reliability benefits without undermining the single-deployable operational goal.

---

## ADR-011 — Container-Friendly Self-Hosted Baseline Deployment

| Field | Value |
|---|---|
| ADR ID | ADR-011 |
| Decision title | Deploy the baseline as container-friendly self-hosted services |
| Status | Accepted |
| Date / owner | 2026-08-08 / Hasitha Erandika |

### 1. Context

CoreGrid's M0 release is intended for a customer organisation to deploy and administer. It needs a reproducible stack containing PostgreSQL, ASP.NET Core API, React static assets, ThunderID configuration, object storage, and model-provider configuration. The project must demonstrate deployment, health checks, environment-based configuration, and a rollback path without committing secrets.

### 2. Decision drivers

| Driver | Weight | Reason |
|---|---:|---|
| Reproducibility | 30% | Evaluators and customers need consistent setup. |
| Portability | 25% | The system should not depend on a single cloud provider. |
| Security of configuration | 20% | Secrets must remain outside source control. |
| Operational simplicity | 15% | The baseline team and customer need manageable operations. |
| Future deployment flexibility | 10% | M1/SaaS and sovereign-cloud routes remain possible. |

### 3. Options considered

| Option | Strengths | Limitations |
|---|---|---|
| Container-friendly self-hosted deployment with environment configuration | Repeatable, portable, supports local/CI/deployed evaluation. | Customer must operate database/identity/storage dependencies. |
| One fixed cloud-provider PaaS deployment | Simple initial hosting. | Vendor lock-in and poorer self-hosted/sovereign portability. |
| Local-machine-only setup | Fastest demonstration setup. | Not reproducible or appropriate for evaluator/customer access. |
| One combined process for all components | Fewer units to deploy. | Poor separation of concerns and difficult static frontend/database management. |

### 4. Decision

Use container-friendly components with environment-variable configuration. Deploy PostgreSQL, the ASP.NET Core API, and the React build as separately manageable components; apply EF Core migrations and seed data idempotently. Treat identity, object storage, email, and model provider as external configured dependencies.

### 5. Rationale

This fits the self-hosted M0 product position, lets CI use an ephemeral PostgreSQL container, and avoids provider-specific lock-in. It also creates a clear startup order and makes configuration auditable without exposing secret values.

### 6. Consequences

- Positive: reproducible local, CI, and evaluation environments.
- Positive: components can be replaced or moved to another platform with limited change.
- Trade-off: deployment documentation and environment management are required.
- Risk: misconfigured external dependencies can cause partial availability.
- Responsibility: expose `/health`, publish `/swagger`, document startup/rollback, run migrations safely, use idempotent seeding, and verify evaluator URLs before submission.

### 7. Review conditions

Review when moving from the M0 single-customer model to multi-tenant SaaS, when regulatory hosting constraints require a specific platform, or when workload characteristics require managed scaling services.
