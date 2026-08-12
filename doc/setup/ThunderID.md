# ThunderID Integration

CoreGrid delegates authentication and identity storage to [ThunderID](https://github.com/thunder-id), an external OIDC provider. This document is a practical setup guide; the architecture and rationale behind it live in [SRS §4](../SRS/04-identity-and-access-management.md).

> **Deployment model:** CoreGrid's M0 is self-hosted, one full stack per customer organisation — its own frontend/backend, own Postgres, own ThunderID instance, with a single ThunderID organisation unit and exactly one CoreGrid `Organization` row (SRS §4.2). M1 turns this into a multi-tenant hosted SaaS (SRS §17) without any change on ThunderID's side — only the "one `Organizations` row" restriction lifts.

## How It Works

- **Signing in.** The React frontend redirects to ThunderID's hosted login (authorization code + PKCE) and gets back a JWT. The backend validates that token on every API request; it never sees a password.
- **Creating accounts.** The only account CoreGrid creates today is the first Administrator, during Setup. The backend calls ThunderID's management API as its own registered application (`client_credentials`), via `ThunderIdIdentityDirectory` (`backend/Identity/ThunderIdIdentityDirectory.cs`).

## One-Time Console Setup

Everything below is done once per ThunderID instance, in the console. It does not need repeating per tenant.

### Start ThunderID and PostgreSQL

**Brand-new machine:**

```bash
docker compose -f oci://ghcr.io/thunder-id/thunderid-quick-start:latest -p coregrid up -d
docker compose up -d   # from the repo root — brings up coregrid-postgres alongside it
```

The first command pulls the (large — several minutes) ThunderID image and runs one-shot init containers that exit normally. The server ends up named `coregrid-thunderid-1`.

**To restart later, don't re-run `up -d`** — use `docker compose start` or `docker start coregrid-thunderid-1`/`coregrid-postgres`. Re-running `up -d` re-executes the one-shot setup container against an already-initialized volume and fails with a user-type conflict.

**The admin password is random, not `admin`** — printed once to the setup container's logs (`docker logs coregrid-thunderid-setup-1`, which stays around after exiting):

```
Admin credentials:
  Username: admin
  Password: <random string>
```

Console: `https://localhost:8090/console`.

### 1. Create the CoreGridUser Type

**User Types** → create one type covering all four CoreGrid roles (the claim contract is uniform across them — [SRS §4.4](../SRS/04-identity-and-access-management.md#44-token-model-and-claim-contract)):

| Field | Value |
|---|---|
| Name | CoreGridUser |
| Self-Registration | Disabled |

Attributes:

| Property Name | Display Name | Type | Required | Unique | Credential |
|---|---|---|---|---|---|
| email | Email Address | String | Yes | Yes | No |
| username | Email Address | String | Yes | Yes | No |
| given_name | First Name | String | Yes | No | No |
| family_name | Last Name | String | Yes | No | No |
| password | Password | String | Yes | No | Yes |

**`username` is required even though `email` is the real identifier.** ThunderID's built-in "Username & Password" sign-in method doesn't dynamically pick whichever attribute is marked Unique — its default flow looks up the literal attribute key `username` (ThunderID's own Go source, `internal/flow/executor/constants.go` / `credentials_auth_executor.go` / `internal/authnprovider/defaultprovider/default_authn_provider.go`). Without a `username` attribute present, that lookup never matches and sign-in fails as "user not found" even for a user that genuinely exists. Give it the same Display Name as `email` ("Email Address") so the sign-in form still reads correctly — the user only ever sees and types one value. `ThunderIdIdentityDirectory` mirrors `email` into `username` on every user it creates (`backend/Identity/ThunderIdIdentityDirectory.cs`); if you create a user by hand in the console instead, set `username` to the same value as `email` yourself.

Don't reuse the built-in `Person` type — it can't be added to an application's Allowed User Types and never picks up app roles.

`ThunderID:UserType` is the literal name **`CoreGridUser`**, not its ID — ThunderID's `POST /users` takes the type's name, not its UUID.

### 2. Note the Organisation Unit ID

**Organisations** → open the root organisation (exists by default) → note its **Organisation Unit ID**. This is `ThunderID:OuId`.

### 3. Create the Four CoreGrid Roles

**First, rename ThunderID's built-in `Administrator` role to `Admin`**: **Roles** → built-in **Administrator** → **Edit**. Purely cosmetic — assignments are keyed by ID, not name — but it stops you confusing it with the CoreGrid role of the same name you're about to create. (If you'd rather not rename it, tell them apart by description instead: the built-in one has one, "System administrator role with full permissions"; the CoreGrid one below doesn't.)

**Roles** → create: `Administrator`, `InventoryOfficer`, `Auditor`, `Staff`. The names must exactly match `CoreGridRole` (`backend/Domain/CoreGridRole.cs`) — a literal string comparison. **Note down each role's ID** — `ThunderID:RoleIds:*` needs them; only `Administrator` is consumed today.

These are CoreGrid's own roles, assigned to **users**. They're a separate object from the built-in role you just renamed, which instead gets assigned to the *backend application* in step 6 (see [Role Assignments](#role-assignments-users-vs-applications) below).

### 4. Create the Frontend Application

**Applications** → **New Application** → **Choose a type** → **Single-Page Application**. The React app is a public PKCE client (SRS §4.4), not a confidential/server-rendered one — the wrong type here changes which fields the rest of the wizard offers.

Then, in order:

- **Details screen**: Name & Logo → **CoreGrid Frontend** (paired with **CoreGrid Backend**, step 6). Leave **Allow all user types** **off** and select **CoreGridUser** explicitly — the reason that type exists (step 1).
- **Sign-in method screen**: check **Username & Password** only — it matches `CoreGridUser`'s `password` attribute. Leave Passwordless/Social/Multi-Factor Login unchecked; none are wired up yet.
- **Note the Client ID, not the Application ID** — only the Client ID is a valid OAuth `client_id`.
- Application URL / Redirect URI / Post-logout redirect URI: all `http://localhost:5173`.
- **Allowed User Types** (Access) → `CoreGridUser`, if not already carried over from the details screen.
- **Token Attributes and Response** → Access Token: add `email`, `given_name`, `family_name`, `roles`.
- **Scopes**: under **Token → User → Scopes & User Attribute Mappings** — not a separate "Scopes" tab. `openid`/`profile`/`email` are pre-added; add `roles` too (type it in if it's not offered as a suggestion). This entirely determines what lands in the token — the frontend has no client-side scopes config to match it against (`@thunderid/react`'s `scopes` prop isn't wired to anything in the installed SDK version).
- **Flows**: assign the default authentication flow.

### 5. Allow the Frontend Origin (CORS)

The browser calls ThunderID's `/oauth2/token` and `/flow/meta` directly (required for PKCE). No console page for this yet — set it via API with an admin token:

```bash
curl -k -X PUT "https://localhost:8090/server-config/cors" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"allowedOrigins": ["http://localhost:5173"]}'
```

Takes effect immediately.

### 6. Create the Backend Service Application

**Applications** → **New Application** → **Choose a type** → **Backend Service** (a confidential, server-to-server client — Single-Page/Native app types won't offer `client_credentials`).

- Name: **CoreGrid Backend** — paired with **CoreGrid Frontend** (step 4). Grant Type: `client_credentials`.
- **Token Endpoint Auth Method: `client_secret_post`**, and *saved* as such, not just selected — left on `client_secret_basic`, every request fails with `unauthorized_client`.
- Note the Client ID and Client Secret.
- **No scopes to configure here.** This application type has no per-app scope list in the console — don't go looking for a `system` scope to create or activate. `ThunderIdIdentityDirectory` sends `scope=system` on the token request regardless (`backend/Identity/ThunderIdIdentityDirectory.cs`), but what actually authorizes the token is the resource/audience it's issued against (step 7) plus the role assignment below.
- **Roles** → built-in **Administrator** (`Admin`, if renamed in step 3) → **Assignments** → add this application. Without this, every management call fails with `403 Forbidden`.

### Role Assignments: Users vs. Applications

Two unrelated, same-named concepts — keep them apart:

| | Assigned to | Assigned via | Used by |
|---|---|---|---|
| **CoreGrid roles** (`Administrator`, `InventoryOfficer`, `Auditor`, `Staff` — step 3) | **Users** (frontend sign-ins) | **Roles** → role → **Assignments** → add the user | Lands in the `roles` claim, read by the React app and the backend's authorization checks |
| ThunderID's built-in **Administrator** role | **Applications** (the backend service app, step 6, only) | **Roles** → built-in Administrator → **Assignments** → add the application | Grants the backend's `client_credentials` token its `system` permission |

The frontend application itself never gets a role — only the users who sign into it do. Today only the first Administrator is provisioned with one, during Setup (`POST /users` then `POST /roles/{roleId}/assignments/add`, see `ThunderIdIdentityDirectory.cs`); assigning `InventoryOfficer`/`Auditor`/`Staff` to a user is manual for now (see Known Gaps).

### 7. The Resource: Built-In `System` Resource Server, or a Custom One

**Resource Servers** define what `resource`/`audience` a token can be issued for. List columns: **Name**, **Type** (`System` = built-in, `Custom` = yours), **Identifier** (an absolute URI — becomes the `aud` claim), and **Actions** (just the row's Edit/Delete buttons, not permissions, despite the name — permissions live inside a resource server's own **Resources** tab). `client_credentials` requests fail with `invalid_target` without a `resource` matching one of these Identifiers.

**Default: reuse the built-in `System` resource server — don't create a new one.** Open it, note its **Identifier** (`https://<host>/mcp`, e.g. `https://localhost:8090/mcp`) — this is `ThunderID:Resource`. It's what the built-in Administrator role's `system` permission is bound to, so reusing it is what makes step 6's role assignment take effect.

**Only create a custom resource server** if you deliberately want the backend's permissions scoped away from ThunderID's built-in semantics: **Resource Servers** → new → **Type: Custom** → Name + Identifier (any absolute URI, e.g. `https://api.coregrid.local/backend`) → define permissions in its **Resources** tab. Then point the backend application's Default Audience (or the `resource` it requests) and `ThunderID__Resource` at the new Identifier instead. More setup for no functional gain in the current single-tenant M0 deployment — stick with the default.

### The Agent Service Doesn't Register With ThunderID

It authenticates via a shared secret instead — `AgentService__SharedSecret` ([SRS §14.2](../SRS/14-deployment-and-operations.md)). ThunderID plays no part in it.

## Environment Variables

**Backend** (`backend/appsettings.Development.json` for non-secrets, `dotnet user-secrets` for the client secret):

```dotenv
ThunderID__Issuer=https://localhost:8090
ThunderID__Resource=https://localhost:8090/mcp
ThunderID__OuId=<Organisation Unit ID, step 2>
ThunderID__UserType=CoreGridUser
ThunderID__RoleIds__Administrator=<Administrator role ID, step 3>
ThunderID__RoleIds__InventoryOfficer=<InventoryOfficer role ID>
ThunderID__RoleIds__Auditor=<Auditor role ID>
ThunderID__RoleIds__Staff=<Staff role ID>
ThunderID__ScimClientId=<Backend Client ID, step 6>
ThunderID__ScimClientSecret=<Backend Client Secret, step 6>
```

- Everything is `https://` — ThunderID doesn't serve plain HTTP by default.
- `ThunderID__Issuer` is the bare server URL, no path. `AddJwtBearer` resolves JWKS and token URLs from it automatically.
- **No `ThunderID__Audience`.** Inbound token validation checks only issuer and RS256 signature. `ThunderID__Resource` is unrelated — it's for the backend's own outbound `client_credentials` calls only.
- `ThunderID__ScimClientSecret` goes in `dotnet user-secrets`, never in `appsettings.Development.json` (committed to git).
- Relax TLS verification for the self-signed cert only in Development — `Program.cs` already gates this on `IsDevelopment()`.

**Frontend** (`frontend/.env.local`, copy from `.env.example`):

```dotenv
VITE_THUNDERID_CLIENT_ID=<frontend Client ID, step 4>
VITE_THUNDERID_BASE_URL=https://localhost:8090
VITE_THUNDERID_AFTER_SIGN_IN_URL=http://localhost:5173
VITE_THUNDERID_AFTER_SIGN_OUT_URL=http://localhost:5173
```

`AFTER_SIGN_IN_URL`/`AFTER_SIGN_OUT_URL` must exactly match the redirect URI from step 4.

## Known Gaps

- **Local-identity fallback** (SRS §4.10) isn't built — `IIdentityDirectory` exists as the seam for it, but only `ThunderIdIdentityDirectory` exists today.
- **Only Administrator provisioning is wired up.** Creating InventoryOfficer/Auditor/Staff accounts isn't built yet.

## Troubleshooting Quick Reference

| Symptom | Likely cause |
|---|---|
| `invalid_target: No resource parameter supplied...` | `ThunderID:Resource` is missing/empty — step 7 |
| `invalid_target: ...must be an absolute URI` | `ThunderID:Resource` isn't the `System` resource server's Identifier — step 7 |
| `403 Forbidden` calling any management API as the backend app | Backend app isn't assigned the built-in Administrator role, or the token request used a scope other than `system` — step 6 |
| `USR-1021: user_type_not_found`, even though the type's ID looks right | `type` in `POST /users` (and `ThunderID:UserType`) must be the type's **name** (`CoreGridUser`), not its ID |
| Sign-in says the user can't be found, but the account genuinely exists | `CoreGridUser` is missing its `username` attribute, or the user's `username` value wasn't set — ThunderID's built-in sign-in method looks up the literal `username` key, not `email` (step 1) |
| `roles` claim present but Administrator-only routes still reject the user | CoreGrid's custom role isn't named exactly `Administrator` (e.g. `Admin`) — exact string match |
| Login succeeds but `roles` claim is missing | Allowed User Types isn't set on the frontend application, or the user has no role assignment |
| Sign-in page says the user can't be found, right after Setup created it | Try again after a `docker restart coregrid-thunderid-1` — the in-memory identifier cache can lag a just-created account |
| `invalid_client` on sign-in | `VITE_THUNDERID_CLIENT_ID` is the Application ID, not the Client ID |
| `key not found` / endless JWKS retries | Issuer is `http://` instead of `https://` |
| `certificate signed by unknown authority` | Relax TLS verification for local dev against the self-signed cert |
| `token has invalid issuer` | Issuer value includes a path; should be the bare server URL |
| CORS errors on `/oauth2/token` or `/flow/meta` | Frontend origin isn't in `cors` server-config's `allowedOrigins` — step 5 |
| Signing out doesn't return to the app | Post-logout redirect URI isn't set — step 4 |
| All data disappeared after a restart | `docker compose down -v` was used instead of `docker compose start` |
| `thunderid-setup` fails with a user-type conflict after a restart | `docker compose up -d` was re-run against an initialized volume — use `docker compose start` |
| Multiple ThunderID stacks, `invalid_client` for no obvious reason | Compose was run without `-p coregrid` from another directory. `docker compose ls`, `docker ps -a \| grep coregrid` to find and consolidate |
