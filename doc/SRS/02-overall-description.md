# 2. Overall Description

## 2.1 Product Perspective

CoreGrid is a new, self-contained product rather than a replacement component within an existing system. It comprises five cooperating parts: an ASP.NET Core Web API that is the sole authoritative application layer; a PostgreSQL relational database; a React single-page application used as the management and control centre; a Flutter mobile application used for field operations; and a Python LangGraph service that executes the agentic workflow as an internal, non-public service.

Two external services sit outside the product boundary but are essential to it. ThunderID provides identity: it authenticates every human user, holds the user directory, and issues the OpenID Connect tokens that the API validates. A transactional email provider delivers notifications triggered by business events. Both are reached over HTTPS, and the email provider is reached exclusively through the backend so that credentials never leave the server boundary.

The architectural invariant of the product — the constraint from which most other design decisions follow — is that both client applications communicate only with the ASP.NET Core Web API, and that the agentic-AI service is reachable only from that API on a private network path. No client holds a database connection string, an AI service address or a third-party API key. This is what allows a single, consistent set of business rules, authorisation checks and audit records to apply regardless of which client initiated an action.

```
                          ┌──────────────────────────────┐
                          │      THUNDERID  (external)   │
                          │  Users · Roles · OIDC        │
                          │  tokens · SCIM                │
                          └───────┬──────────────┬───────┘
            OIDC auth-code + PKCE │              │ JWKS / SCIM 2.0
        ┌─────────────────────────┘              └──────────────┐
        │                                                       │
  ┌─────┴──────────┐        ┌──────────────────┐          ┌─────┴──────────────────┐
  │  REACT SPA     │        │ FLUTTER MOBILE   │          │                        │
  │  Management &  │        │ Field operations │          │   ASP.NET CORE         │
  │  control centre│        │ QR · verify ·    │          │   WEB API              │
  │  Admin·Auditor │        │ report · tasks   │          │                        │
  └───────┬────────┘        └────────┬─────────┘          │  AuthN/AuthZ           │
          │  HTTPS / REST / JWT      │  HTTPS / REST/JWT  │  Validation            │
          └──────────────┬───────────┘                    │  Business rules        │
                         └───────────────────────────────▶│  Persistence           │
                                                          │  AI gateway            │
                                                          │  Audit logging         │
                                                          └───┬────────────┬───────┘
                                  private network             │            │
                        ┌─────────────────────────────────────┘            │
                        ▼                                                  ▼
        ┌───────────────────────────────┐                    ┌──────────────────────────┐
        │  LANGGRAPH AGENT SERVICE      │   tool calls via   │      POSTGRESQL          │
        │  Planner · Maintenance ·      │◀──── API only ────▶│  Assets · Lifecycle ·    │
        │  Budget · Policy · HITL       │                    │  Workflow state (JSONB)  │
        └───────────────────────────────┘                    └──────────────────────────┘
                                                                          ▲
                          ┌───────────────────────────────┐               │
                          │ EMAIL PROVIDER (external)     │◀──────────────┘
                          │ transactional notifications   │  backend-mediated only
                          └───────────────────────────────┘
```

Figure 1 — CoreGrid system context. Neither client application reaches PostgreSQL, the agent service or any third-party service directly.

## 2.2 Product Functions

At the highest level of abstraction CoreGrid provides nine function groups. Each is decomposed into numbered functional requirements in Section 6.

| # | Function group | Summary |
|---|---|---|
| F1 | Identity and access | Organisation-scoped authentication through ThunderID, role-based authorisation, protected routes and screens, session and token lifecycle management, and a local user mirror for referential integrity. |
| F2 | Platform configuration | Administration of departments, locations, asset categories, asset types and the custom attribute definitions that determine what data each asset type captures. |
| F3 | Asset registry and identification | Registration, amendment, search and lifecycle-status tracking of assets, with QR code generation and scan-based lookup from the field. |
| F4 | Maintenance management | Fault reporting from the field with photographic evidence, maintenance record creation, assignment, progress tracking, cost capture and completion. |
| F5 | Transfer management | Requesting, approving and confirming the movement of an asset between departments or locations, including physical confirmation of receipt. |
| F6 | Disposal management | Condemnation of unserviceable assets, submission and evidenced approval of disposal requests, and recording of the disposal outcome. |
| F7 | Audit and compliance | Verification campaigns, field verification of individual assets, discrepancy recording and resolution, immutable audit logging and compliance reporting. |
| F8 | Agentic decision support | A four-agent workflow that evaluates an asset's lifecycle position, validates its recommendation deterministically, pauses for authorised human approval and produces an auditable result or a safe failure. |
| F9 | Analytics, reporting and notification | Role-appropriate dashboards, exportable operational reports, and transactional email notification of the events that require a person to act. |

## 2.3 User Classes and Characteristics

CoreGrid recognises four human user classes. The assignment requires at least three roles with genuinely different responsibilities and permissions; CoreGrid defines four because the separation between the officer who records physical facts and the auditor who independently verifies them is the control that makes the audit trail meaningful.

| User class | Characteristics and context of use | Primary client | Representative privileges |
|---|---|---|---|
| Department Staff | Ordinary employees who use the assets day to day. Low system-usage frequency, minimal training, often reporting a problem in the moment they encounter it. Technical proficiency is not assumed. | Flutter (primary), React (read-only) | View assets assigned to their department; report a fault with a photograph; view the status of requests they raised. |
| Inventory Officer | Custodians of the register for one or more departments. Frequent users, mobile for a substantial part of the working day, moving between stores, workshops and offices. Comfortable with the domain vocabulary. | Flutter and React | Register and amend assets; scan and verify; record condition; raise maintenance, transfer and disposal requests; confirm physical transfer; initiate an agentic evaluation. |
| Auditor | Independent reviewers who confirm that the register reflects physical reality. Work in campaigns; must not be able to alter the records they audit. Report-oriented. | React (primary), Flutter (field verification) | Create and run verification campaigns; record and classify discrepancies; read all lifecycle history; generate and export audit and compliance reports. No write access to asset master data. |
| Administrator | Configure the platform for their organisation and hold approval authority for irreversible actions. Small population, high privilege, desk-based. | React only | Manage departments, locations, categories, asset types and attribute definitions; manage users and role assignments; approve transfers and disposals; approve, reject or request revision of agentic recommendations; view system-wide analytics. |

A fifth, non-human actor is recognised for authorisation purposes: the Agent Service Principal. It authenticates to the API using a confidential client credential rather than a user identity, holds a narrowly scoped read-and-report permission set, and is explicitly denied every permission that changes business state. Section 4.6 defines its permissions and Section 7.9 the controls applied to it.

## 2.4 Operating Environment

| Component | Target environment |
|---|---|
| ASP.NET Core Web API | .NET 10 LTS, deployed to a managed container or app-service platform on a no-cost or institution-provided tier; Linux runtime; HTTPS enforced; Swagger/OpenAPI and a health endpoint exposed. |
| PostgreSQL | PostgreSQL 15 or later, managed instance with restricted network access and credentials supplied only through environment configuration; schema managed exclusively by EF Core migrations. |
| React web application | Modern evergreen browsers (Chrome, Edge, Firefox, Safari — current and previous major version). Built with Vite and served as static assets from a hosting platform configured to call the deployed API. |
| Flutter mobile application | Android 8.0 (API 26) and above; release APK produced for evaluation. Requires camera permission for QR scanning and photo capture, and network connectivity for all business operations. |
| LangGraph agent service | Python 3.11 or later, containerised, reachable only from the API over a private network path or a shared-secret-authenticated internal endpoint; no public ingress. |
| Identity provider | ThunderID, self-hosted alongside the API and database as part of each customer organisation's own deployment (M0: one ThunderID instance per deployment, single organisation unit — Section 4.2). |
| Email provider | Transactional email API on a free tier, invoked only from the backend, with credentials held in server-side configuration. |

## 2.5 Design and Implementation Constraints

| ID | Constraint | Origin |
|---|---|---|
| C-01 | The public backend shall be implemented in C# with ASP.NET Core Web API. No alternative public backend is permitted. | SE3090 §2 |
| C-02 | Data access shall use Entity Framework Core with the PostgreSQL provider; the relational store shall be PostgreSQL. | SE3090 §2 |
| C-03 | The web client shall be React using functional components, hooks and routing, with a justified state-management approach. | SE3090 §2, §7 |
| C-04 | The mobile client shall be Flutter and Dart with a justified state-management approach and at least one meaningful device feature. | SE3090 §2, §8 |
| C-05 | React and Flutter shall communicate only with the ASP.NET Core Web API. Neither client may call the agentic-AI service, the database or a third-party service directly. | SE3090 §2 mandatory backend rule |
| C-06 | The agentic-AI service shall run as an internal service invoked by ASP.NET Core, and shall implement at least four distinct agents with controlled tools, persisted state, deterministic validation and human approval. | SE3090 §9 |
| C-07 | Both clients shall share one user identity, one permission model and one set of business rules. | SE3090 §1 integrated-system rule |
| C-08 | Authentication and user management shall be delegated to ThunderID using OpenID Connect; CoreGrid shall not store user passwords or password hashes. | Project decision ADR-002 |
| C-09 | The system shall be deliverable using institution-provided or no-cost services; no paid subscription may be required to build, deploy or evaluate it. | SE3090 §14 |
| C-10 | The database schema shall be created and evolved only through EF Core migrations committed to the repository; no manual schema change is permitted in any environment. | Project decision |
| C-11 | Secrets — client secrets, database credentials, email API keys, agent-service shared secrets — shall never be committed to the repository and shall be supplied through environment variables. | SE3090 §18.2, OWASP |
| C-12 | The implementation window is seven development weeks plus one stabilisation week. Any requirement that cannot be completed and evidenced within that window shall be descoped to Section 17 rather than partially delivered. | R3 delivery plan |
| C-13 | Every submitted artefact must be explainable, modifiable and debuggable by its named owner; AI-assisted generation is permitted only under the disclosure regime of SE3090 §18. | SE3090 §3, §18 |

## 2.6 User Documentation

- A repository README covering the business problem, roles, features, technology justification, architecture, environment variables, database setup, startup order for all five components, live URLs and evaluation test accounts.
- An Architecture Decision Record set of between three and six one-page decisions, covering at minimum the React state-management approach, the Flutter state-management approach, the agentic-AI framework and orchestration method, the identity-provider decision, the database strategy for workflow state and custom attributes, and the deployment platform.
- An API reference generated from Swagger/OpenAPI annotations, published at the deployed API and accessible without credentials for the schema itself.
- A short operator guide describing the configuration sequence a new organisation follows: departments, locations, categories, asset types, attribute definitions, users and role assignment.
- A demonstration script that walks the complete cross-platform golden workflow from the Flutter scan to the returned status, for use in the ten-minute evaluation.

## 2.7 Assumptions and Dependencies

| ID | Assumption or dependency | Impact if invalid |
|---|---|---|
| A-01 | ThunderID's self-hosted quick-start distribution remains available to deploy and supports application roles and the authorisation-code-with-PKCE flow for public clients. | Identity must fall back to the contingency in Section 4.10; ADR-002 would be revised and the fallback path implemented within the stabilisation week. |
| A-02 | Every asset in scope can carry a durable, scannable QR label affixed at registration. | Field verification would require manual code entry; FR-041 provides this fallback so the workflow degrades rather than fails. |
| A-03 | Field officers have network connectivity at the point of scanning for the demonstrated scenarios. | Offline capture and deferred synchronisation would be required; this is explicitly a roadmap item (Section 17) and not baseline scope. |
| A-04 | Depreciation for residual value may be computed on a straight-line basis from acquisition cost, acquisition date and a per-asset-type useful life. | A more elaborate depreciation model would be needed; the calculation is isolated behind a single service so the change is local. |
| A-05 | An LLM endpoint is reachable from the agent service during development and evaluation, within free-tier limits. | Golden-case evaluation would rely on the recorded fixtures in Section 13.4 and the demonstration would use the deterministic path only. |
| A-06 | The organisation policies used by the Policy Agent can be expressed as declarative thresholds and predicates rather than free-text rules. | Policy evaluation could not remain deterministic; the rule set would have to be narrowed rather than made LLM-interpreted. |
| A-07 | All four component owners are available throughout the seven-week implementation window. | Scope is reduced by removing Could-priority requirements first, then Should, in the order recorded in Section 15. |

## 2.8 System Scope Boundary Statement

CoreGrid is an intelligent asset lifecycle management platform consisting of a React-based web application, a Flutter-based mobile application, an ASP.NET Core RESTful API, a PostgreSQL relational database, an agentic-AI subsystem and an external identity provider.

The React application is responsible for administrative, configuration, management, reporting, auditing, dashboard, business-data management and agentic-AI monitoring and approval activities. The Flutter application is responsible for field and operational activities, including QR-based asset identification, physical verification, fault reporting with photographic evidence, task execution, transfer confirmation and mobile status tracking. Both applications communicate exclusively with the ASP.NET Core API, which is responsible for authentication enforcement, authorisation, validation, business rules, persistence, workflow initiation, approval handling and audit logging. The agentic-AI subsystem is accessed only through the backend and is never addressed directly by a client.

The agentic-AI subsystem provides decision support and workflow orchestration. It may analyse asset information, create structured plans, delegate tasks to specialised agents, invoke allow-listed tools, generate recommendations and perform controlled validation. High-impact business actions — asset disposal and any other irreversible lifecycle change — require deterministic validation followed by authorised human approval before the system updates the corresponding business state.

### 2.8.1 In Scope

| Capability | Capability |
|---|---|
| Organisation, department and location configuration | Asset categories, asset types and custom attribute definitions |
| Asset registration, amendment and lifecycle status tracking | QR code generation and QR-based field identification |
| Maintenance request, assignment, progress and completion | Photographic evidence capture from the mobile device |
| Asset transfer request, approval and physical confirmation | Condemnation and evidenced disposal approval |
| Verification campaigns and field verification | Discrepancy recording, classification and resolution |
| Immutable audit logging of state-changing operations | Role-based access control across both clients |
| Four-agent lifecycle decision workflow with persisted state | Deterministic policy validation and human approval |
| Auditable execution summaries and safe-failure recording | Transactional email notification of actionable events |
| Role-appropriate dashboards and exportable reports | Search, filtering, sorting and pagination across list views |

### 2.8.2 Out of Scope for the Baseline Release

| Excluded capability | Rationale |
|---|---|
| Autonomous execution of high-impact actions by the AI | Contradicts the human-approval control that is central to the system's trustworthiness and to the assignment's acceptance criteria. |
| Shared multi-tenant SaaS delivery and billing | This is M1 of the product's planned two-stage delivery (Section 17), not the M0 baseline this SRS specifies. CoreGrid's M0 deployment model is one self-hosted instance per customer organisation (Section 2.4) — there is no cross-tenant boundary to bill or manage yet because `Organizations` is currently restricted to one row per deployment and there is no self-service signup or billing flow. Unlike an earlier assumption, M1 does *not* require reintroducing per-tenant identity-provider organisations: ThunderID has no organisation construct in either stage (Section 4.2), and the existing `OrganizationId` global query filter (Section 4.5) already isolates any number of tenants — M1 only needs the row-count restriction lifted and a signup/billing layer added. |
| Integration with enterprise resource-planning or national financial systems | Requires credentials, contracts and interface specifications that cannot be obtained within the delivery window; introduces unbounded schedule risk. |
| Trained predictive machine-learning models for failure forecasting | The Maintenance Analysis Agent derives its projections from recorded history using deterministic statistics; model training and validation is a separate research effort. |
| Computer-vision assessment of damage from captured photographs | Photographs are stored as evidence only. Automated condition inference is a roadmap item. |
| Comprehensive offline synchronisation with conflict resolution | Requires a local store, a synchronisation protocol and a merge strategy — a substantial subsystem in its own right. |
| Payment, procurement or auction execution | Disposal produces an authorised, evidenced decision; the financial transaction that follows is executed outside CoreGrid. |
| Full enterprise identity governance (access reviews, delegated administration hierarchies, custom identity federation) | ThunderID provides authentication, the organisation directory and role assignment; governance workflows beyond this are not required by the baseline. |
| Native iOS release | The Flutter codebase is cross-platform, but only an Android APK is produced and evidenced for the baseline. |
| Localisation into languages other than English | Interface strings are externalised to permit later translation, but no additional locale is delivered. |
