# 4. Identity and Access Management with ThunderID

## 4.1 Decision and Rationale

CoreGrid delegates authentication and user directory management to ThunderID and retains authorisation within the ASP.NET Core API. This is recorded as ADR-002 and is the single most consequential architectural decision in the identity domain, so its reasoning is set out here in full.

- Credential risk is removed rather than mitigated. CoreGrid stores no passwords and no password hashes. The most damaging class of breach for a system of record — credential disclosure — is structurally impossible because the credentials never enter the application boundary.
- Tenant isolation is honoured by the deployment model itself, not built as a boundary inside a shared system. CoreGrid's M0 platform model requires that one customer organisation's data can never be presented to another. Rather than run one shared system for every customer and rely on a boundary within it, CoreGrid is deployed once per customer organisation (Section 2.4) — its own ASP.NET Core API, its own PostgreSQL database, its own ThunderID instance. There is no cross-tenant boundary to get wrong, because no two customers ever share infrastructure. Section 4.2 sets out what this means for identity specifically, and Section 17 sets out how M1 changes it.
- Standards, not proprietary integration. Authentication uses OpenID Connect over OAuth 2.0. The API validates tokens against a published JWKS endpoint. Nothing in the API depends on ThunderID-specific behaviour beyond claim names, which are isolated in a single mapping component — so the contingency in Section 4.10 is a small change, not a rewrite.
- Capabilities that would otherwise consume schedule. Multi-factor authentication, password policy, account recovery, account lockout and session termination are configuration in ThunderID. Building even acceptable versions of these would consume a substantial fraction of a seven-week window and would still be weaker than a managed provider's.
- Separation of concerns matches the trust model. ThunderID answers "who is this person, and which organisation do they belong to". CoreGrid answers "may this person perform this operation on this record". The second question depends on CoreGrid domain state — department ownership, asset status, workflow position — and therefore cannot be delegated.

## 4.2 Organisation and User Model

In M0, CoreGrid is deployed once per customer organisation (Section 2.4): each customer's ThunderID instance, PostgreSQL database and API instance are its own, self-hosted, and never shared with any other customer. A customer receives the code, deploys their own stack, and runs Setup themselves to create their own `Organizations` row and Administrator account. Within one deployment there is exactly one CoreGrid `Organizations` row — Setup creates it once and refuses to create a second (Section 4.7) — and one ThunderID organisation unit. Every user who ever signs into a given deployment belongs to that one customer, so there is nothing for either ThunderID or CoreGrid's database to isolate *between* — isolation between customers is a property of them never sharing a running system, not of a boundary enforced within one. Section 17 describes M1, where a single shared deployment serves many customer organisations instead.

```
   ONE DEPLOYMENT (M0) — self-hosted per customer organisation: its own
   API, its own PostgreSQL, its own ThunderID instance, never shared
   with another customer.

   THUNDERID ORGANISATION UNIT  (single — this customer only)
   │   application registrations · shared branding
   │
   └── Users:  a.silva · j.fernando · n.perera · …
         Role assignments (Section 4.6): Administrator · InventoryOfficer · Auditor · Staff

   COREGRID DATABASE
   Organizations  (exactly one row — this customer)
        │
        ├──1:N── Departments ──1:N── Locations
        └──1:N── Users   (Users.ExternalSubjectId = ThunderID "sub")
                   │
                   └── UserRoles (effective role snapshot, refreshed at sign-in)

   A second customer organisation ("Acme Logistics Pte Ltd", "Northern
   Fleet Services", …) is a second, independent deployment of this same
   diagram — not a second Organizations row inside this one.
```

Figure 5 — One self-hosted deployment per customer organisation (M0); each has exactly one `Organizations` row and one ThunderID organisation unit. A second customer means a second deployment, not a second row.

Departments (the business unit — owns assets, holds budgets, appears in transfer and approval rules) are deliberately not modelled as organisation structure inside ThunderID; they change far more often than a deployment's single tenant does, and keeping them as CoreGrid data lets an Administrator reconfigure them without any identity-provider operation. `Organizations` itself is not a mechanism for separating this customer's data from another's — no other customer's data is ever in this database. It exists so the schema has a stable root for the global query filter (Section 4.5, step 9), and so a customer could model internal sub-units under it later if it ever wanted to — the same root that lets M1 (Section 17) hold many customers' rows side by side without changing this filter at all.

| Concept | Owned by | Reason |
|---|---|---|
| Organisation (this deployment's customer) | CoreGrid database exclusively | Retained as the schema's root and the query filter's scope (Section 4.5); in M0 not a boundary between different tenants, since one deployment never holds more than one — in M1 (Section 17) this same column is what does hold that boundary. |
| User identity and credentials | ThunderID exclusively | Credentials must not enter the application boundary. |
| Role assignment | ThunderID, mirrored at sign-in | Roles are identity facts that must be consistent across both clients and available at token-validation time. |
| Department and location | CoreGrid database | Business structure, frequently reconfigured, referenced by business rules and foreign keys. |
| Department membership of a user | CoreGrid database | It determines data scope and approval routing — a business rule, not an identity fact. |
| Effective permission for an operation | CoreGrid API policy layer | Depends on domain state (asset status, workflow position, ownership) that ThunderID does not hold. |

## 4.3 Application Registration and Grant Types

| Client | ThunderID application type | Grant / flow | Token storage |
|---|---|---|---|
| React SPA | Single-page application (public client) | Authorisation Code with PKCE; refresh token rotation enabled | Access token in memory only; refresh handled by the OIDC SDK. No token in localStorage. |
| Flutter application | Mobile / native application (public client) | Authorisation Code with PKCE via an external user agent, per RFC 8252; custom scheme redirect | Refresh token in flutter_secure_storage (Android Keystore); access token in memory. |
| ASP.NET Core API | Protected resource (API resource with defined scopes) | Validates bearer tokens by issuer and JWKS signature, not audience (Section 4.4); no user-facing flow. The resource/audience identifier is used only by the backend's own outbound `client_credentials` calls for user provisioning (Section 4.7), to satisfy ThunderID's resource-indicator requirement — not for inbound token checks. | Not applicable — holds no user tokens. |
| Agent service principal | Machine-to-machine (confidential client) | Client credentials, or an equivalently protected internal shared secret over the private network path | Secret supplied by environment variable; never logged, never committed. |

Both public clients use PKCE without a client secret, because a secret embedded in a browser bundle or an APK is not a secret. The Flutter client performs authentication in an external user agent rather than an embedded web view, as required by RFC 8252, so that the user can see the identity provider's address bar and the application never has access to the credential entry surface.

## 4.4 Token Model and Claim Contract

The API depends on a defined set of claims. This is the contract between ThunderID configuration and application code; if a claim is renamed in the identity provider, exactly one class in the API changes.

| Claim | Purpose in CoreGrid | Handling |
|---|---|---|
| `iss` | Issuer. Must exactly match the configured ThunderID issuer. | Validated on every request. Mismatch rejects with 401. |
| `exp` / `nbf` / `iat` | Token lifetime. | Validated with a clock skew tolerance of no more than 60 seconds. |
| `sub` | Stable subject identifier of the authenticated user. | Primary key for the local user mirror (`Users.ExternalSubjectId`). Never reassigned. The user's `OrganizationId` — CoreGrid's tenant boundary — is read from that mirror record, not from any token claim (Section 4.2, 4.5). |
| `roles` (or `groups`) | The application roles assigned to the user (Section 4.6). ThunderID assigns roles instance-wide, not per organisation, since it has no organisation construct to scope them to (Section 4.2). | Mapped to CoreGrid role constants and projected into `ClaimsPrincipal` role claims for policy evaluation. |
| `email`, `given_name`, `family_name` | Display identity and notification addressing. | Mirrored to the local user record; treated as personal data under Section 10.8. |
| `scope` | Coarse-grained API scopes granted to the client application. | Checked as a precondition; fine-grained authorisation remains policy-based. |
| `jti` | Token identifier. | Recorded in the audit log for correlation. The token itself is never persisted. |

**No audience validation.** `aud` is deliberately not part of this contract or of token validation (Section 4.5). ThunderID's protected-resource/audience registration (Section 4.3) is used only for the backend's own outbound `client_credentials` requests during user provisioning — never for validating inbound user tokens.

**Never persisted**

CoreGrid stores no access token, no refresh token, no ID token and no password material in the database or in any log. Audit records reference the subject identifier and, where useful for correlation, the token identifier — never the token.

## 4.5 Token Validation in ASP.NET Core

Validation is configured once in the composition root and applies to every protected endpoint. The following sequence executes on each authenticated request.

```
  1  Extract bearer token from the Authorization header.
  2  Verify RS256 signature against the JWKS published by the ThunderID
     issuer (keys cached, refreshed on unknown kid).
  3  Validate issuer, expiry, not-before, and clock skew ≤ 60s.
     Audience is deliberately not checked here (Section 4.4).
  4  Resolve sub to the local user mirror; create or refresh the mirror
     record (email, name, roles) on first request of a session.
     → user deactivated locally  ⇒  403 Forbidden.
  5  Read OrganizationId from that same local mirror record — the sole
     source of tenant scope, since no token claim carries it (Section 4.2).
  6  Project roles claim into ClaimsPrincipal role claims.
  7  Populate the request-scoped ITenantContext with OrganizationId,
     UserId, DepartmentId and effective roles.
  8  Evaluate the endpoint authorisation policy.
  9  Every repository query applies a global query filter on OrganizationId
     drawn from ITenantContext — isolation is not left to the caller.
```

Step 9 is the control that makes cross-organisation data disclosure a structural impossibility rather than a matter of developer discipline. The global query filter is applied in the EF Core model configuration, so a developer who forgets a where-clause still cannot read another organisation's rows. Unlike an org-claim-based design, the root of trust for `OrganizationId` is CoreGrid's own database record (step 5) rather than a value ThunderID itself asserts — correct by construction as long as `Users.OrganizationId` is only ever written by CoreGrid's own provisioning code (Section 4.7), never derived from request input.

## 4.6 Role and Permission Model

CoreGrid uses four application roles, defined in ThunderID and assigned per user (instance-wide — Section 4.2 — since ThunderID has no per-organisation scope to assign them within). Authorisation in the API is policy-based rather than role-attribute-based: endpoints declare a required policy, and the policy expresses the permission in domain terms. This keeps the mapping from role to capability in one auditable place and permits a permission to depend on record state where necessary.

| Permission | Staff | Officer | Auditor | Admin | Agent principal |
|---|---|---|---|---|---|
| asset:read (own department) | Yes | Yes | Yes | Yes | Yes |
| asset:read (organisation-wide) | No | Yes | Yes | Yes | Yes |
| asset:create / asset:update | No | Yes | No | Yes | No |
| asset:verify | No | Yes | Yes | No | No |
| maintenance:request | Yes | Yes | No | Yes | No |
| maintenance:manage | No | Yes | No | Yes | No |
| transfer:request | No | Yes | No | Yes | No |
| transfer:approve | No | No | No | Yes | No |
| transfer:confirm-receipt | No | Yes | No | No | No |
| disposal:request | No | Yes | No | Yes | No |
| disposal:approve | No | No | No | Yes | No |
| audit:campaign-manage | No | No | Yes | Yes | No |
| audit:discrepancy-resolve | No | No | Yes | Yes | No |
| audit:log-read | No | No | Yes | Yes | No |
| config:manage | No | No | No | Yes | No |
| user:manage | No | No | No | Yes | No |
| workflow:initiate | No | Yes | No | Yes | No |
| workflow:read | Status only | Yes | Yes | Yes | Own run |
| workflow:approve | No | No | No | Yes | No |
| report:generate | No | Yes | Yes | Yes | No |
| tool:read-asset-history | No | No | No | No | Yes |
| tool:read-budget-summary | No | No | No | No | Yes |
| tool:read-policy-set | No | No | No | No | Yes |

Three properties of this matrix are load-bearing. The Auditor cannot create or amend an asset, which is what makes an audit finding independent evidence. The Administrator cannot confirm physical receipt of a transfer, because that is an assertion about the physical world that only the receiving officer can truthfully make. The agent principal holds three read-only tool permissions and nothing else — no create, no update, no approve — which is the enforcement point behind the architectural rule that agents advise but never decide.

## 4.7 User Provisioning and the Local Mirror

CoreGrid maintains a local `Users` table. It is not a second identity store: it holds no credentials and is never authoritative for authentication. It exists for three reasons — foreign-key integrity (every asset, maintenance record, approval and audit entry references a user), query performance (a list of two hundred maintenance records must not produce two hundred directory lookups), and historical accuracy (an audit record from March must still show who acted even if that person has since left the organisation).

| Scenario | Behaviour |
|---|---|
| First sign-in of a new user | The API creates the mirror record from the token claims on the first authenticated request, assigns the default department if one is configured, and records a `UserProvisioned` audit event. |
| Subsequent sign-in | Email, display name and roles are refreshed from the token if they differ. A role change is recorded as a `RoleChanged` audit event. |
| Administrator invites a user | The API calls ThunderID's management API using a confidential service credential to create the user within CoreGrid's single shared organisation unit (Section 4.2) and assign the requested role; ThunderID sends the invitation. The mirror record is created immediately in a Pending state. |
| Administrator deactivates a user | The local mirror is marked inactive and the corresponding SCIM record is disabled. Deactivation takes effect at the API on the next request regardless of token validity, because step 5 of Section 4.5 checks local status. |
| User is deleted in ThunderID | The mirror record is retained and marked inactive. It is never hard-deleted, because audit and lifecycle history reference it. |
| Department assignment | Held only in CoreGrid and changed by an Administrator; never sourced from the identity provider. |

## 4.8 Session and Token Lifecycle

- Access token lifetime is short — fifteen minutes is the configured target — so that a revoked or changed authorisation takes effect quickly without the API maintaining session state.
- Refresh tokens are rotated on use. A replayed refresh token invalidates the family, which limits the value of a stolen token on a lost device.
- The React client holds the access token in memory only. It is never written to localStorage or sessionStorage, so a cross-site scripting defect cannot exfiltrate a durable credential.
- The Flutter client holds the refresh token in platform secure storage backed by the Android Keystore, and never writes tokens to shared preferences or application logs.
- Sign-out clears local state, revokes the refresh token at ThunderID and terminates the identity-provider session, so that a subsequent sign-in genuinely re-authenticates.
- The API is stateless with respect to sessions. It maintains no server-side session store, which is what allows both clients and any number of API instances to share one identity without affinity.
- Expiry during an in-flight request produces a single transparent refresh-and-retry in both clients; a second failure returns the user to the sign-in screen with their unsaved input preserved where practical.

## 4.9 Identity-Related Security Requirements

| ID | Requirement | Priority |
|---|---|---|
| SEC-ID-01 | The API shall reject any request whose token fails signature, issuer or lifetime validation, returning 401 without disclosing which check failed. | Must |
| SEC-ID-02 | The API shall resolve organisation membership only from the local user mirror record keyed by the token's `sub` claim (Section 4.5, 4.7), and shall never infer organisation from any token claim, request content, header or path parameter. | Must |
| SEC-ID-03 | Every entity that belongs to an organisation shall carry `OrganizationId` and shall be subject to an EF Core global query filter derived from the request context. | Must |
| SEC-ID-04 | JWKS keys shall be cached with automatic refresh on an unrecognised key identifier, and shall be retrieved only over HTTPS from the configured issuer. | Must |
| SEC-ID-05 | No token, credential or secret shall be written to any log, error message, audit record, telemetry payload or repository file. | Must |
| SEC-ID-06 | The Flutter client shall use an external user agent for authentication and store refresh tokens only in platform secure storage. | Must |
| SEC-ID-07 | The React client shall not persist access tokens to web storage. | Must |
| SEC-ID-08 | CORS shall permit only the configured origins of the deployed React application. | Must |
| SEC-ID-09 | All authentication and authorisation outcomes — success, failure and denial — shall be recorded with subject, organisation, endpoint and timestamp. | Must |
| SEC-ID-10 | The agent service principal shall be denied every write permission by policy, and this denial shall be covered by an automated authorisation test. | Must |
| SEC-ID-11 | Multi-factor authentication shall be available for the Administrator role through ThunderID configuration. | Should |
| SEC-ID-12 | A deactivated local user shall be denied access on their next request even if their token remains within its validity period. | Must |

## 4.10 Compliance Note and Contingency

The SE3090 specification lists "JWT authentication, role-based authorization, protected endpoints, password hashing and secure configuration" among the backend security requirements. CoreGrid satisfies JWT authentication, role-based authorisation, protected endpoints and secure configuration directly. Password hashing is satisfied by delegation: CoreGrid handles no passwords, and ThunderID applies its own credential-protection regime. Delegating credential handling to a standards-based identity provider is the stronger engineering position, and it is the position the group will defend at the viva.

Two provisions guard against the risk that this delegation is judged not to evidence the requirement, or that the identity provider is unavailable during evaluation.

- **Evidence provision.** ADR-002 records the decision, the alternatives considered (ASP.NET Core Identity with local credential storage; a custom JWT issuer) and the consequences, including this exact compliance consideration. The group can demonstrate the full token lifecycle end to end during the viva and explain each validation step in Section 4.5.
- **Contingency provision.** The API defines an `IIdentityDirectory` abstraction and validates tokens through the standard ASP.NET Core JWT bearer pipeline. A fallback local identity module — ASP.NET Core Identity with an appropriately configured password hasher, issuing tokens with the same claim set — can therefore be enabled by configuration without changing any controller, policy, service or client. This fallback is implemented and covered by tests during the stabilisation week, and is used if ThunderID is unavailable at evaluation time. Any outage will be evidenced to the evaluator as permitted by SE3090 §14.

The contingency is not a parallel implementation maintained indefinitely; it is a configuration-selectable authentication handler behind a stable claims contract, and it exists so that a dependency outside the group's control cannot prevent the system from being demonstrated.
