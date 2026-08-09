# Set Up ThunderID

CoreGrid delegates authentication and user-directory management to ThunderID (ADR-002, [SRS §4](../SRS/04-identity-and-access-management.md)). This guide walks through starting it, creating the roles and organisations CoreGrid needs, and connecting the frontend and backend applications to it.

**Most of this guide is one-time infrastructure setup**, done once per instance in the ThunderID console: starting the containers, the `CoreGridUser` type, the four roles, the frontend application, CORS, and the backend's service credential. The first Administrator account and the first organisation are **not** created by hand here — that's what the app's own Setup wizard (`/setup`, [`frontend/src/pages/Setup.tsx`](../../frontend/src/pages/Setup.tsx)) is for, backed by `POST /api/setup/complete` ([`backend/Features/Setup`](../../backend/Features/Setup)). See [CONTRIBUTING.md](../../CONTRIBUTING.md) for that end-to-end flow.

> **One section below (the API's audience/scope registration) describes ThunderID configuration that CoreGrid's SRS specifies but that hasn't been walked through end-to-end against a running console.** Everything else in this guide — starting the containers, user types, roles, the frontend application, CORS, the backend service application — mirrors a configuration that's been run and confirmed working.
>
> **The Setup wizard itself is real and reachable, but organisation creation doesn't actually work yet.** `GET /api/setup/status` genuinely checks the database; `POST /api/setup/complete` genuinely writes the local `Organizations`/`Users` rows — but the ThunderID-provisioning call it depends on (`backend/Identity/ThunderIdIdentityDirectory.cs`) throws `NotImplementedException` until someone confirms ThunderID's actual organisation/user-management API contract and implements it there. That's the next piece of this integration to build, not a bug in the wizard.

### Start ThunderID and PostgreSQL

**If this is a brand-new machine — nothing for this project has been pulled or started before — do it in two steps.** Pull and bootstrap ThunderID on its own first:

```bash
docker compose -f oci://ghcr.io/thunder-id/thunderid-quick-start:latest -p coregrid up -d
```

This is the one path that's actually been run end-to-end: it pulls the (large — expect several minutes, not a hang) ThunderID image, runs its one-shot `thunderid-db-init` and `thunderid-setup` containers (these exit after running, that's normal, not an error), and starts the server, all under the `coregrid` project name. The container ends up named `coregrid-thunderid-1` (Compose's default `<project>-<service>-<index>` naming).

Then, from the repo root, bring up CoreGrid's own database alongside it:

```bash
docker compose up -d
```

Because [`docker-compose.yml`](../../docker-compose.yml) pins the same project name (`coregrid`) and includes the same ThunderID bundle, Compose recognises the containers that already exist from the step above and only creates what's missing — `coregrid-postgres`, CoreGrid's own application database, on host port `5433`.

**If ThunderID is genuinely not running anywhere yet**, `docker compose up -d` alone, from the repo root, should be sufficient — its `include:` pulls the same ThunderID bundle and, per `docker compose config`, correctly renames the container to `coregrid-thunderid` via this file's `container_name` override. That single-command path hasn't been run start-to-finish in practice, though, only validated statically — if it doesn't behave as expected, fall back to the two-step sequence above, which has.

**To restart later, don't re-run either `up -d` command** — use `docker compose start` instead (or `docker start coregrid-thunderid-1` / `coregrid-postgres` individually). Resuming existing containers is safe; re-running `up -d` re-executes the one-shot `thunderid-setup` container against the already-initialized volume, and its bootstrap step isn't idempotent — it fails with a user-type name conflict, and since `thunderid` won't start until `thunderid-setup` completes successfully, the server never comes up either.

**Important: the admin password is not `admin`.** It's randomly generated the first time setup runs, and printed once to the setup container's own logs:

```
Admin credentials:
  Username: admin
  Password: <random string>
```

You can change this later inside the console. It's shown exactly once.

Access the console at `https://localhost:8090/console` using that username and password.

### Create the CoreGrid User Type

Go to **User Types** in the left sidebar and create one type covering all four CoreGrid roles — the SRS's claim contract ([§4.4](../SRS/04-identity-and-access-management.md#44-token-model-and-claim-contract)) is uniform across Staff, Officer, Auditor and Administrator, so unlike a product with per-role attribute schemas, CoreGrid doesn't need a separate user type per role.

| Field | Value |
|---|---|
| Name | CoreGridUser |
| Self-Registration | Disabled |

Self-registration is disabled because FR-013 makes an Administrator invite the only path a user enters CoreGrid by — there is no public sign-up.

Attributes — this covers exactly the identity claims CoreGrid's claim contract needs (`email`, `given_name`, `family_name`), plus a credential:

| Property Name | Display Name | Type | Required | Unique | Credential |
|---|---|---|---|---|---|
| email | Email Address | String | Yes | Yes | No |
| given_name | First Name | String | Yes | No | No |
| family_name | Last Name | String | Yes | No | No |
| password | Password | String | Yes | No | Yes |

Don't reuse the console's built-in `Person` type for real CoreGrid accounts — `Person` accounts can't be added to an application's Allowed User Types and will never pick up app roles.

### Organisations Are Created By the App, Not Here

CoreGrid isolates tenant institutions using ThunderID organisations ([SRS §4.2](../SRS/04-identity-and-access-management.md#42-organisation-and-user-model)): one root organisation for the CoreGrid platform itself, and one sub-organisation per tenant institution. Users are created inside their institution's sub-organisation, never in the root.

Unlike the platform-level setup elsewhere in this guide, **sub-organisations are not created by hand in the console.** The root organisation is implicit in the ThunderID instance itself — everything registered below (the user type, the roles, the two applications) is registered against it once. Each tenant institution's sub-organisation, and its first Administrator's account inside it, gets created when someone completes the app's Setup wizard at `/setup`, which calls `POST /api/setup/complete` on the backend, which — once `ThunderIdIdentityDirectory` is implemented — calls ThunderID using the backend's own service credential from "Create the Backend Service Application" below. That's also why the React SPA doesn't need a fixed `organizationHandle` in its config: it's registered once, serves every tenant, and ThunderID resolves each signed-in user's own sub-organisation from their account rather than from anything the frontend sends.

If you want to create an additional organisation or a test user without going through the wizard — e.g. to test multi-tenant isolation locally — that's still possible directly in the console under **Organisations**, it just isn't the normal path once the app is running.

### Create Roles

Go to **Roles** in the left sidebar and create:

- Administrator
- InventoryOfficer
- Auditor
- Staff

These are plain business roles used by CoreGrid's own policy layer ([SRS §4.6](../SRS/04-identity-and-access-management.md#46-role-and-permission-model), [Appendix B](../SRS/appendix-b-route-level-authorisation-map.md)) — the role name must exactly match what the API's claim-mapping component expects, since it's compared against the literal `roles` claim value on every request. A role named anything else will never satisfy a policy.

**These are separate from ThunderID's own built-in `Administrator` role**, which is dealt with separately below. CoreGrid happens to also name one of its business roles "Administrator" — don't confuse the two. ThunderID's built-in `Administrator` role controls who can call ThunderID's own management API (creating/managing ThunderID accounts); CoreGrid's custom `Administrator` role controls who can approve transfers, manage configuration, etc. inside CoreGrid itself.

There is no ThunderID role for the agent service principal — see "The Agent Service Doesn't Register With ThunderID" below.

### Create the Frontend Application

Go to **Applications** and create a new application for the React frontend.

- Choose a web/browser application type
- Note down the Application ID
- Set the Application URL to `http://localhost:5173`
- Set the redirect URI to `http://localhost:5173`
- Also set the **post-logout redirect URI** to `http://localhost:5173`. This is a separate whitelist from the sign-in redirect URI above — if it's missing, `/oauth2/logout` rejects the request with `invalid post_logout_redirect_uri` and the ThunderID SDK's `signOut()` fails to send the user back to the app.

**Set Allowed User Types.** In the **Access** section of the application, add `CoreGridUser` to Allowed User Types. This step is easy to miss, but without it, no user attributes or roles will be added to tokens for anyone signing into this app, no matter what you configure elsewhere. This is also why you can't test with the built-in console admin — that account is type `Person`, which isn't and can't be added to this list.

Go to **Token Attributes and Response** for this application and add the following attributes to the **Access Token**, matching CoreGrid's claim contract ([SRS §4.4](../SRS/04-identity-and-access-management.md#44-token-model-and-claim-contract)):

- email
- given_name
- family_name
- roles
- org_id
- org_name

Go to **Available Scopes** and activate: `roles` (along with the default `openid`, `profile`, `email`). CoreGrid's claim contract doesn't call for a phone number, so there's no need to activate `phone` here the way a product with SMS notifications would.

Go to **Flows** and assign the default authentication flow to the application.

### Allow the Frontend Origin (CORS)

The React app calls ThunderID's `/oauth2/token`, `/flow/meta`, and related endpoints **directly from the browser** (that's how PKCE token exchange works for a public SPA client) — this is separate from the backend API's own `Cors__AllowedOrigins` setting ([SRS §14.2](../SRS/14-deployment-and-operations.md), satisfying SEC-ID-08), which only covers requests to the ASP.NET Core API. Without this step ThunderID has no allowed origins by default, so every one of those browser requests is blocked by CORS and sign-in silently fails.

Update the `cors` server-config section (there's no console page for this yet — use the API with an admin token):

```bash
curl -k -X PUT "https://localhost:8090/server-config/cors" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"allowedOrigins": ["http://localhost:5173"]}'
```

This takes effect immediately — no restart needed. Add any other origin the frontend is served from (a deployed domain, a different dev port, etc.) to the same array.

### Create the Backend Service Application

This is the "confidential service credential" [SRS §4.7](../SRS/04-identity-and-access-management.md#47-user-provisioning-and-the-local-mirror) refers to — the credential the ASP.NET Core API uses to create/manage ThunderID accounts when an Administrator invites a user.

Go to **Applications** and create a new Backend Service application.

- Name: CoreGrid Backend
- Grant Type: `client_credentials`
- Token Endpoint Auth Method: `client_secret_post`
- Note down the Client ID and Client Secret

Go to **Available Scopes**, add a `user-management` custom scope, and activate it.

### Assign Administrator Role to the Backend Application

Go to **Roles**, open the built-in **Administrator** role, go to the **Assignments** tab, and assign the CoreGrid Backend application to it.

This allows the backend to create and manage users in ThunderID programmatically. Without this step, the backend app can still request an access token successfully, but every management API call (like creating a user) will fail with a `403 Forbidden`.

### The Agent Service Doesn't Register With ThunderID

Unlike the React frontend and the backend's service credential above, the LangGraph agent service is **not** registered as a ThunderID application. [SRS §4.3](../SRS/04-identity-and-access-management.md#43-application-registration-and-grant-types) allows either a ThunderID client-credentials client or "an equivalently protected internal shared secret over the private network path" for the agent service principal, and [§14.2](../SRS/14-deployment-and-operations.md) settles on the latter: the agent authenticates to the API with `AgentService__SharedSecret`, a plain secret generated locally (e.g. `openssl rand -hex 32`) and set identically on both sides — never issued by ThunderID. The API's own authentication pipeline is what recognises that secret and grants the "Agent principal" permissions from [§4.6](../SRS/04-identity-and-access-management.md#46-role-and-permission-model); ThunderID plays no part in it, since the agent service is never reachable from the public internet in the first place.

### The First Administrator and Organisation

There's no manual user-creation step here — see "Organisations Are Created By the App, Not Here" above. Once `ThunderIdIdentityDirectory` is implemented, the first Administrator account and its organisation come from completing the Setup wizard at `http://localhost:5173/setup` (see [CONTRIBUTING.md §7](../../CONTRIBUTING.md#7-try-it-out)).

### Environment Variables

With the applications, roles and organisation(s) created above, add their values to the backend's and frontend's configuration. CoreGrid's env var names are already fixed in [SRS §14.2](../SRS/14-deployment-and-operations.md) — use those exact names, not new ones.

**Backend** (`backend/appsettings.Development.json` or environment variables — the backend has no ThunderID wiring yet, this is what it'll need):

```dotenv
ThunderID__Issuer=https://localhost:8090
ThunderID__Audience=<see note below>
ThunderID__ScimClientId=<Backend Service Client ID from above>
ThunderID__ScimClientSecret=<Backend Service Client Secret from above>
```

- Everything is `https://`, not `http://`. ThunderID doesn't serve plain HTTP by default.
- `ThunderID__Issuer` is the bare server URL only, no path.
- There's no separate JWKS or token URL to configure — ASP.NET Core's JWT bearer middleware resolves both from the issuer's `/.well-known/openid-configuration` document automatically, per [SRS §4.5](../SRS/04-identity-and-access-management.md#45-token-validation-in-aspnet-core) step 2.
- `ThunderID__Audience`: unlike the frontend application above, [Appendix C](../SRS/appendix-c-thunderid-configuration-checklist.md) calls for registering the API "as a protected resource" with its own audience identifier — the OpenSchool-derived steps this guide is otherwise built on never needed that (their backend used a `resource` parameter on the token request instead of a separate registration). Confirm which of the two applies to your ThunderID instance once you wire up `AddJwtBearer` in the backend, and correct this line.
- If your backend's JWKS client verifies TLS certificates strictly, relax that only for local development against ThunderID's self-signed certificate, and only when running locally, never in production.
- **`Token Endpoint Auth Method` must actually be `client_secret_post` on the saved application, not just selected during creation.** The backend's client sends `client_id`/`client_secret` as form body fields, not an HTTP Basic Auth header. If the app ends up on `client_secret_basic`, every `client_credentials` request fails with `unauthorized_client: Client is not allowed to use the specified authentication method`, even though the client ID/secret are correct. Double-check this value in the console after saving.

**Frontend** (`frontend/.env.local` — copy from `.env.example`):

```dotenv
VITE_THUNDERID_CLIENT_ID=<frontend Application ID from above>
VITE_THUNDERID_BASE_URL=https://localhost:8090
VITE_THUNDERID_SCOPES="openid profile email roles"
VITE_THUNDERID_AFTER_SIGN_IN_URL=http://localhost:5173
VITE_THUNDERID_AFTER_SIGN_OUT_URL=http://localhost:5173
```

- `VITE_THUNDERID_AFTER_SIGN_IN_URL` and `VITE_THUNDERID_AFTER_SIGN_OUT_URL` must exactly match the redirect URI you set on the application's config above, including scheme and trailing slash (or lack of one).
- `VITE_THUNDERID_SCOPES` should match whatever you activated under Available Scopes for this application.
- These are already wired into `ThunderIDProvider` in `frontend/src/main.tsx`.

### Troubleshooting quick reference

| Symptom | Likely cause |
|---|---|
| Server won't start, `ouId or ouHandle is required` | A resource is missing its organisation unit reference |
| `403 Forbidden` calling any management API as the backend app | Backend app hasn't been assigned the built-in ThunderID Administrator role |
| Login succeeds but `roles` claim is missing from the token | Application's Allowed User Types isn't set, or the signed-in user has no role assignment |
| Backend gets `key not found` / endless JWKS retries | Issuer is `http://` instead of `https://` |
| Backend gets `certificate signed by unknown authority` | TLS verification needs to be relaxed for local dev against the self-signed cert |
| Backend gets `token has invalid issuer` | Issuer value includes a path; it should be just the bare server URL |
| All users/roles/data disappeared after a restart | `docker compose down -v` was used, or the whole stack (including the one-time database init container) was recreated instead of just restarting the running containers |
| `thunderid-setup` fails with a user-type conflict after a restart, and/or `thunderid` never comes back up | `docker compose up -d` was re-run against an already-initialized volume instead of `docker compose start` — see "Start ThunderID and PostgreSQL" above |
| Console itself won't load — `/oauth2/authorize` redirects to an error page, even for the built-in `CONSOLE` client | An aborted `thunderid-setup` re-run partially applied before it hit the conflict and died, deleting default resources without recreating them. There's no clean recovery from this — `docker compose down -v`, then `docker compose up -d` fresh |
| Multiple ThunderID stacks running or half-remembered, credentials rejected with `invalid_client` | This repo's `docker-compose.yml` pins its project name to `coregrid` (the top-level `name:` field), so running it from anywhere always targets the same stack — but a *different* stack started elsewhere with `-p coregrid` targets the same name too and will collide with it. `docker compose ls`, then `docker ps -a \| grep coregrid` and `docker volume ls \| grep coregrid` to see what actually exists, and consolidate down to one |
| Sign-in silently fails; console shows CORS errors on `/oauth2/token` or `/flow/meta`, ends up back on the sign-in page | Frontend origin isn't in the `cors` server-config's `allowedOrigins` — see "Allow the Frontend Origin (CORS)" above |
| Signing out doesn't return the user to the app (stuck on ThunderID, or an error page) | The application's post-logout redirect URI isn't set — see "Create the Frontend Application" above |
| Backend gets a schema-validation error creating a user | The user type's field name in the console doesn't match what the backend sends — check **User Types → CoreGridUser → schema** for typos against `email` / `given_name` / `family_name` / `password` |
