# CoreGrid — Architecture Decision Records

**Project:** CoreGrid — Intelligent Asset Lifecycle Management Platform  
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
| Maintainability for a small maintainer team | 20% | A comprehensible dependency structure is required. |
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

The API fits the chosen ASP.NET Core technology stack and ensures that web/client behaviour cannot diverge on important decisions such as disposal, transfer approval, and agent workflow execution. It also provides a single place to validate JWTs, resolve organisation scope, apply policies, start transactions, and write audit records.

### 6. Consequences

- Positive: one source of truth for business rules, policy authorisation, and auditability.
- Positive: client applications remain unprivileged and can evolve independently.
- Trade-off: API changes require careful backward-compatible DTO and endpoint management.
- Responsibility: maintain controller/service/domain boundaries and test each protected endpoint.

### 7. Review conditions

Review if sustained independent scaling needs, separate release cadences, or component ownership boundaries cannot be served by the modular API without harming reliability or delivery speed.

---

## ADR-002 — Delegate Authentication to ThunderID; Keep Authorisation in the API

| Field | Value |
|---|---|
| ADR ID | ADR-002 |
| Decision title | Use ThunderID (OIDC / OAuth 2.0) for authentication and the user directory; CoreGrid owns authorisation |
| Status | Accepted |
| Date / owner | 2026-08-07 / Hasitha Erandika |

### 1. Context

Both clients need sign-in, sessions, password recovery and an organisation user directory. Storing and hashing passwords inside CoreGrid would add the most security-sensitive code in the system to a product whose value lies elsewhere. At the same time, CoreGrid's permissions are domain-specific (who may approve a disposal, which department Staff may see), so they cannot live in a generic identity provider.

### 2. Decision drivers

| Driver | Weight | Reason |
|---|---:|---|
| No credential storage in CoreGrid | 30% | Removes password storage, hashing and recovery from the attack surface. |
| One identity for web and mobile | 25% | The same user, role and token contract on both clients. |
| Domain authorisation stays in code | 20% | Policies must be testable alongside the business rules they guard. |
| Self-hostable and open source | 15% | Each customer runs their own identity provider in the M0 model. |
| Standards compliance | 10% | OIDC with PKCE for SPAs and native apps (RFC 7636, RFC 8252). |

### 3. Options considered

| Option | Strengths | Limitations |
|---|---|---|
| ThunderID (OIDC, PKCE, SCIM) | Self-hostable, standards-based; hosted sign-in and recovery flows; SCIM for admin-driven provisioning. | Another service to deploy and configure. |
| ASP.NET Core Identity with local credentials | Everything in one process. | CoreGrid stores password hashes and must build recovery, lockout and MFA itself; mobile token flow is custom. |
| Custom JWT issuer | Full control. | Re-implements a security-critical protocol; highest risk. |
| Hosted commercial IdP | Least operational work. | Not self-hostable; conflicts with the per-customer deployment model and adds cost. |

### 4. Decision

ThunderID authenticates users through OIDC with PKCE: the web SPA uses the ThunderID React SDK, and mobile uses AppAuth with a custom-scheme redirect. The API validates RS256 JWTs against JWKS (issuer, audience, lifetime). `RoleEnrichmentMiddleware` then resolves the token's `sub` to the local `Users` mirror once per request. Roles and departments are managed from CoreGrid's Users page and written to ThunderID through SCIM (`IIdentityDirectory`). Every endpoint is guarded by a named CoreGrid policy, with a fail-closed fallback.

### 5. Rationale

Delegation removes credential handling entirely, while the local mirror keeps every authorisation decision, department scope and organisation filter inside CoreGrid's own tested code.

### 6. Consequences

- Positive: no passwords or hashes in CoreGrid; standards-based tokens on both clients.
- Positive: deactivated users are denied on their next request even with a valid token (FR-009).
- Trade-off: ThunderID must be deployed, configured (`scripts/thunderid/`) and kept reachable; `/health` reports its status.
- Deviation: Setup and the Administrator password reset accept a password and forward it to ThunderID without storing it.

### 7. Review conditions

Review if a customer mandates a different OIDC provider (only the directory adapter and configuration should change) or if the multi-tenant M1 edition needs per-tenant identity realms.

---

## ADR-003 — React State Management with Feature-Scoped Hooks

| Field | Value |
|---|---|
| ADR ID | ADR-003 |
| Decision title | Keep React server state in feature-scoped custom hooks; session state in the ThunderID SDK context |
| Status | Accepted (revised 2026-10-04: replaces the original TanStack Query + Zustand decision, which was never adopted in code) |
| Date / owner | 2026-08-06 / Jayashan Guruge; revised 2026-10-04 by Hasitha Erandika |

### 1. Context

Most of the React application's state belongs to the server: paginated assets, maintenance records, transfers, campaigns, reports and agent workflows. Each page loads its own filtered, server-paginated slice and refetches after a mutation. Very little state is shared between pages; the main exception is the signed-in user's session and access token, which the ThunderID React SDK already provides through its own context. Permissions come from `/api/me` and are read through `usePermissions()`.

### 2. Decision drivers

| Driver | Weight | Reason |
|---|---:|---|
| Predictable loading / empty / error / success states | 30% | Every page must show all four states consistently (NFR-28). |
| Low dependency and conceptual overhead | 25% | A small maintainer team; every dependency must be understood and maintained. |
| Fit with the actual data shape | 20% | Server data is page-scoped and short-lived; little of it is shared across pages. |
| Testability | 15% | Hooks must be easy to drive from React Testing Library with mocked API modules. |
| Bundle size | 10% | Carbon is already large. |

### 3. Options considered

| Option | Strengths | Limitations |
|---|---|---|
| Feature-scoped custom hooks (`useState`/`useEffect`) + ThunderID SDK context | No extra dependency; the hook owns loading/error/retry; easy to mock in tests; mirrors the backend's feature folders | No shared cache: two pages showing the same list fetch it twice; each hook must handle cancellation itself |
| TanStack Query + Zustand | Shared cache, invalidation and background refetch | Two more libraries and query-key discipline, for caching benefits the page-scoped data barely uses |
| Redux Toolkit (+ RTK Query) | Mature, explicit | Most boilerplate; global store for state that is not global |
| React Context for everything | Built in | Re-renders every consumer; no loading/error/retry semantics |

### 4. Decision

Server state lives in feature-scoped custom hooks (`features/<name>/hooks/`), each returning `{ data, error, isError, isLoading, refetch }` and guarding against updates after unmount. Mutations go through the shared `useStubMutation` helper (`shared/hooks/`), which returns `{ mutate, isPending, isError, data }`; on success, the calling page calls the relevant list hook's `refetch`. Session and access token come from `useThunderID()`. Route state belongs to React Router. Role-based permissions are derived in `usePermissions()` from the `/api/me` profile.

### 5. Rationale

This matches how the data is actually used: one page, one filtered server query, refetched after a mutation. The hook interface intentionally mirrors TanStack Query's (`isPending`, `isError`, `data`), so the code can move to a cache library later without changing any page if cross-page caching ever becomes necessary.

### 6. Consequences

- Positive: no state library to learn or configure; every hook is a short, readable unit with its own tests.
- Positive: loading, error and retry behaviour is uniform because each hook follows the same template.
- Trade-off: no cross-page cache, so navigating back refetches.
- Risk: a page that forgets to refetch after a mutation shows stale data. Mitigated by the "mutation `onSuccess` → `refetch()`" convention.
- Responsibility: every new list hook must handle cancellation and expose `refetch`; tests cover loading, error and post-mutation refresh.

### 7. Review conditions

Review if several pages start sharing the same server data, if optimistic updates or background refetching become requirements, or if offline support is introduced. Any of these would justify adopting TanStack Query behind the existing hook interface.

---

## ADR-004 — Flutter State Management with Riverpod

| Field | Value |
|---|---|
| ADR ID | ADR-004 |
| Decision title | Use Riverpod for state management and dependency injection in the Flutter client |
| Status | Accepted |
| Date / owner | 2026-08-18 / Hasitha Erandika (mobile shell), with Jayashan Guruge |

### 1. Context

The mobile app is a field client: QR scanning, asset lookup, verification, fault reporting with photos, transfers, notifications and agent-workflow status. Its state is mostly asynchronous API results, plus authentication state that every screen and the router depend on. It must be testable at widget level without a real backend or identity provider.

### 2. Decision drivers

| Driver | Weight | Reason |
|---|---:|---|
| First-class async state (loading / data / error) | 30% | Every screen renders API results with loading, empty and error views. |
| Testability through dependency overrides | 25% | Widget tests replace the API client and auth controller with fakes. |
| Router integration | 20% | `go_router` redirects depend on auth and role state. |
| Compile-time safety | 15% | Providers are type-checked; no runtime lookup failures. |
| Boilerplate | 10% | A small team, with mobile work done alongside web and API work. |

### 3. Options considered

| Option | Strengths | Limitations |
|---|---|---|
| Riverpod (`flutter_riverpod` + `riverpod_generator`) | `AsyncValue` models loading/data/error; `ProviderScope(overrides:)` makes widget tests trivial; no `BuildContext` needed in the API layer | Learning curve for the provider types |
| BLoC | Explicit events and states; well documented | Much more boilerplate for request/response screens |
| Provider | Simple, official | Relies on `BuildContext`; weaker async and override story |
| `setState` + service locator | No dependency | Scatters loading/error handling; hard to test and to share auth state |

### 4. Decision

Use Riverpod 3 throughout. Each feature has a `<feature>_providers.dart` exposing `FutureProvider`/`AsyncNotifier` providers over a `<feature>_api.dart` built on a shared Dio `ApiClient`. Authentication state (`authControllerProvider`, `meProvider`) is read by the `go_router` redirect for role-gated navigation. Tokens are stored with `flutter_secure_storage`.

### 5. Rationale

Riverpod's `AsyncValue` maps directly onto the required loading / empty / error states (`shared/widgets/state_views.dart`), and provider overrides let widget tests run against fakes without network access or ThunderID.

### 6. Consequences

- Positive: uniform async handling, and widget tests that need no mocking framework.
- Positive: auth and role state drive navigation from a single source.
- Trade-off: code generation (`build_runner`) is part of the workflow.
- Responsibility: each feature keeps providers at feature level, not per screen, and invalidates the relevant provider after a mutation.

### 7. Review conditions

Review if offline synchronisation is introduced, which would need a persisted local store, or if the app grows complex event-driven flows where BLoC's explicit event model would pay off.

---

## ADR-005 — LangGraph as the Agentic Framework (Superseded)

| Field | Value |
|---|---|
| ADR ID | ADR-005 |
| Status | **Superseded by ADR-010 (2026-09-15)** |

The Planner and Budget Analysis agents were first built as Python/LangGraph services. The typed state, persisted checkpoints, conditional edges and human-approval interrupt that LangGraph provided turned out to be simple to implement directly in C#. Keeping a second runtime would have doubled deployment, secret handling and security review for every self-hosted customer, so the agents were moved in-process. This record is retained for traceability only.

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

An attribute definition belongs to an asset type and records the attribute name, data type, required status, validation rule, select options, and display order. Each asset value is stored in a separate row linked to both the asset and its definition. The value row has typed columns for text, number, date, and boolean values; a select value is stored as text. A database check constraint requires exactly one value column to be populated, and a unique index allows at most one value for each asset-definition pair. Application validation checks that the value matches the definition's data type and rules.

### 5. Rationale

Relational storage was chosen for asset attributes because FR-019 requires values to be validated against their definitions and FR-028 requires searching by custom value. Foreign keys keep each value connected to a valid asset and attribute definition; typed columns and indexes support validation and filtering. New asset types and attributes can be configured without adding columns to the asset table. Storing them as JSONB would make definition-level referential integrity and typed filtering harder to enforce.

Workflow state has a different shape: plans, tool traces, findings, and validation artefacts vary between runs and are usually read as one record. JSONB keeps those workflow records flexible without weakening the relational guarantees required for asset attributes. The trade-off for relational attributes is that rendering an asset's full set of values requires joins.

### 6. Consequences

- Positive: asset attributes retain integrity and are searchable/indexable.
- Positive: workflow state can evolve without frequent schema migrations.
- Trade-off: asset reads require joins; workflow JSONB requires careful schema validation at application boundaries.
- Risk: unbounded JSONB growth can affect storage and query performance.
- Responsibility: index common relational filters, validate workflow schemas, and persist only structured artefacts—not prompts, tokens, or chain-of-thought.

### 7. Review conditions

Review if workflow artefacts require frequent field-level reporting/querying, or if custom-attribute queries show unacceptable performance at the target dataset size.

---

## ADR-007 — Deployment Platform (Superseded)

| Field | Value |
|---|---|
| ADR ID | ADR-007 |
| Status | **Superseded by ADR-011** |

The original record proposed a single hosting platform. It was replaced by ADR-011's platform-neutral, container-friendly deployment, which fits a self-hosted open-source product better. Retained for traceability only.

---

## ADR-008 — IBM Carbon Design System for the Web Client

| Field | Value |
|---|---|
| ADR ID | ADR-008 |
| Decision title | Build the React client from IBM Carbon components and tokens |
| Status | Accepted |
| Date / owner | 2026-08-09 / Hasitha Erandika |

### 1. Context

CoreGrid's web client is a dense, data-heavy administrative application used by public-sector staff: tables, filters, forms, approval dialogs, dashboards. It needs accessible, consistent components without a dedicated design team.

### 2. Decision drivers

| Driver | Weight | Reason |
|---|---:|---|
| Accessibility (WCAG 2.1 AA) | 30% | Public-sector users; NFR-26. |
| Enterprise data components | 25% | Data tables, pagination, structured lists, inline notifications. |
| Consistency without a designer | 20% | Tokens and components remove per-page styling decisions. |
| Theming | 15% | White theme, plus `g100` for the AI-monitoring surface. |
| Maintenance | 10% | Actively maintained, typed React package. |

### 3. Options considered

| Option | Strengths | Limitations |
|---|---|---|
| IBM Carbon (`@carbon/react`) | Accessible enterprise components, design tokens, data tables, icons. | Large CSS bundle; opinionated look. |
| Material UI | Popular, flexible. | Consumer-app look; more customisation for dense admin screens. |
| Tailwind + hand-built components | Full control, small CSS. | Accessibility and consistency become the team's job. |
| Ant Design | Rich data components. | Weaker accessibility track record; heavier theming. |

### 4. Decision

Use `@carbon/react` and `@carbon/icons-react` throughout the web client. Colours, spacing and chart palettes are defined as SCSS tokens in `src/styles/index.scss`; features use no hard-coded colours and no inline styles.

### 5. Rationale

Carbon provides audited, accessible primitives for exactly the screens CoreGrid needs. Shared tokens keep every feature visually consistent.

### 6. Consequences

- Positive: accessible tables, forms and notifications out of the box; consistent look across the four components.
- Trade-off: a large CSS bundle (≈ 840 kB before gzip); route-level code splitting is a future optimisation (NFR-08).

### 7. Review conditions

Review if bundle size becomes a measured performance problem or if a customer requires its own design system.

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

Use Cloudflare R2 as the default CoreGrid-operated object store and permit any S3-compatible storage endpoint for self-hosted deployments. Access it only through `IFileStorageService` in the ASP.NET Core backend.

### 5. Rationale

The approach keeps storage credentials and access control on the server, maintains portability, and avoids coupling large binary files to PostgreSQL. The `IFileStorageService` abstraction aligns with the API's dependency-inversion approach and permits a provider replacement without changing business services.

### 6. Consequences

- Positive: better database hygiene and portable storage integration.
- Positive: no client receives bucket credentials or direct upload authority.
- Trade-off: object-store availability is a separate operational dependency.
- Risk: incorrectly configured bucket permissions could expose content.
- Responsibility: validate MIME type/size (clients compress before upload), persist only an opaque object key, issue authorised short-lived signed URLs, and keep buckets private.

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

CoreGrid's lifecycle decisions need a stateful workflow with four distinct specialised agents. It must evaluate an asset lifecycle decision, persist its execution history, use controlled read-only tools, validate recommendations deterministically, resist prompt injection, and pause for authorised human approval before any high-impact action. The original LangGraph-based approach introduced a separate Python runtime; the revised target architecture must be simpler for a self-hosted baseline.

### 2. Decision drivers

| Driver | Weight | Reason |
|---|---:|---|
| Safety, policy control, and human approval | 30% | Agents may advise but cannot independently change business data. |
| Durable state and restart recovery | 20% | Workflows must resume from persisted checkpoints. |
| Operational simplicity | 20% | M0 self-hosted deployments should minimise runtime/services. |
| Clear agent boundaries and ownership | 15% | Each component maintainer's agent must be visible, separately testable and independently re-runnable. |
| Model cost and provider portability | 15% | Model use should be limited and replaceable. |

### 3. Options considered

| Option | Strengths | Limitations |
|---|---|---|
| In-process .NET orchestrator with four agent services | One deployable, shared security/transaction boundary, explicit typed contracts, easy API integration. | Team must implement graph/checkpoint behaviour carefully. |
| Permanent Python/LangGraph service split | Mature graph primitives and separate runtime. | Adds deployment/security surface and cross-service operational complexity. |
| One centralised Python service for all agents | Centralises agent functionality. | Places all agent logic outside the primary API and increases service coupling. |
| Linear prompt chain | Simple to build initially. | Insufficient explicit delegation, state, validation, tool control, and approval semantics. |

### 4. Decision

Run a single in-process `AgentWorkflowService` in the ASP.NET Core API. It orchestrates four separate agent services, each behind its own interface: Planner (`IPlannerAgentClient`), Maintenance Analysis (`IMaintenanceAnalysisAgentService`), Budget Analysis (`IBudgetAgentClient`) and Policy Compliance (`IPolicyComplianceAgentService`). Agents read data only through the read-only `IAgentToolsService`. Model access goes through configuration (`LlmSettings`: endpoint, model and key, with per-agent overrides) to any OpenAI-compatible chat-completions endpoint, and only where a node meets the model-use criterion. In the baseline that is the Planner, plus the Budget Analysis node's option ranking. If the primary provider fails, both retry once against an optional fallback provider (`LlmFallback`, Groq `openai/gpt-oss-120b` by default) and then fall back deterministically.

### 5. Rationale

This option gives genuine delegation to distinct agents while preserving CoreGrid's rule that the API, rather than an agent, decides and executes state changes. It removes a separate runtime from the self-hosted baseline, limits inference cost, and makes tool allow-lists, organisation scoping, persistence, deterministic validation, and human approval part of one reviewed security boundary.

### 6. Consequences

- Positive: four auditable, independently testable agent responsibilities in one deployable.
- Positive: checkpoints, execution steps, validation results, and approvals persist alongside domain data.
- Positive: few model calls, provider portability through one OpenAI-compatible protocol (which also makes a second provider a configuration change), and no client access to model credentials.
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
| Reproducibility | 30% | Contributors and customers need a consistent, repeatable setup. |
| Portability | 25% | The system should not depend on a single cloud provider. |
| Security of configuration | 20% | Secrets must remain outside source control. |
| Operational simplicity | 15% | The baseline team and customer need manageable operations. |
| Future deployment flexibility | 10% | M1/SaaS and sovereign-cloud routes remain possible. |

### 3. Options considered

| Option | Strengths | Limitations |
|---|---|---|
| Container-friendly self-hosted deployment with environment configuration | Repeatable, portable, supports local/CI/deployed evaluation. | Customer must operate database/identity/storage dependencies. |
| One fixed cloud-provider PaaS deployment | Simple initial hosting. | Vendor lock-in and poorer self-hosted/sovereign portability. |
| Local-machine-only setup | Fastest demonstration setup. | Not reproducible or appropriate for customer access. |
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
- Responsibility: expose `/health`, publish `/swagger`, document startup/rollback, run migrations safely, use idempotent seeding, and verify public URLs in a private browser session before each release.

### 7. Review conditions

Review when moving from the M0 single-customer model to multi-tenant SaaS, when regulatory hosting constraints require a specific platform, or when workload characteristics require managed scaling services.
