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
| given_name | First Name | String | Yes | No | No |
| family_name | Last Name | String | Yes | No | No |
| password | Password | String | Yes | No | Yes |

Don't reuse the built-in `Person` type — it can't be added to an application's Allowed User Types and never picks up app roles.

`ThunderID:UserType` is the literal name **`CoreGridUser`**, not its ID — ThunderID's `POST /users` takes the type's name, not its UUID.

### 2. Note the Organisation Unit ID

**Organisations** → open the root organisation (exists by default) → note its **Organisation Unit ID**. This is `ThunderID:OuId`.

### 3. Create the Four CoreGrid Roles

**Roles** → create: `Administrator`, `InventoryOfficer`, `Auditor`, `Staff`.

The name must exactly match `CoreGridRole` (`backend/Domain/CoreGridRole.cs`) — the backend does a literal string comparison. **Note down each role's ID** — `ThunderID:RoleIds:*` needs them; only `Administrator` is consumed today.

These are **not** the same object as ThunderID's own built-in `Administrator` role (used in step 6 below, assigned to the *backend application*, not a CoreGrid user). Tell them apart by description: the built-in one has one ("System administrator role with full permissions"); the one you just created doesn't.

### 4. Create the Frontend Application

**Applications** → new web/browser application:

- **Note the Client ID, not the Application ID** — only the Client ID is a valid OAuth `client_id`.
- Application URL / Redirect URI / Post-logout redirect URI: all `http://localhost:5173`.
- **Allowed User Types** (Access) → add `CoreGridUser`. Without this, no attributes or roles land in tokens for anyone.
- **Token Attributes and Response** → Access Token: add `email`, `given_name`, `family_name`, `roles`.
- **Available Scopes**: activate `roles` alongside the default `openid`/`profile`/`email`.
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

**Applications** → new Backend Service application:

- Name: CoreGrid Backend, Grant Type: `client_credentials`.
- **Token Endpoint Auth Method: `client_secret_post`** — must be *saved* as this, not just selected. Left on `client_secret_basic`, every request fails with `unauthorized_client`.
- Note the Client ID and Client Secret.
- **Available Scopes**: add `system` as a custom scope, activate it.
- **Roles** → built-in **Administrator** → **Assignments** → add this application. Without this, every management call fails with `403 Forbidden`.

### 7. The Resource: ThunderID's Built-In `System` Resource Server

**Use the built-in `System` resource server — do not create a new one.** `client_credentials` requests fail with `invalid_target` without a `resource`. Go to **Resource Servers**, open the built-in `System` one, and note its **Identifier** (`https://<host>/mcp`, e.g. `https://localhost:8090/mcp` locally) — this is `ThunderID:Resource`. Despite the MCP-sounding name, it's the resource server the built-in Administrator role's `system` permission is actually bound to.

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
VITE_THUNDERID_SCOPES="openid profile email roles"
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
