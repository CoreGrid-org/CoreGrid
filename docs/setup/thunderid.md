# ThunderID Integration

CoreGrid delegates authentication and identity storage to [ThunderID](https://github.com/thunder-id), an external OIDC provider. This document is a practical setup guide; the architecture and rationale behind it live in [SRS §4](../srs/04-identity-and-access-management.md).

> **Deployment model:** CoreGrid's M0 is self-hosted, one full stack per customer organisation — its own frontend/backend, own Postgres, own ThunderID instance, with a single ThunderID organisation unit and exactly one CoreGrid `Organization` row (SRS §4.2). M1 turns this into a multi-tenant hosted SaaS (SRS §17) without any change on ThunderID's side — only the "one `Organizations` row" restriction lifts.

## How It Works

- **Signing in.** The React frontend redirects to ThunderID's hosted login (authorization code + PKCE) and gets back a JWT. The backend validates that token on every API request; it never sees a sign-in password.
- **Creating accounts.** Setup creates the first Administrator; after that, Administrators create users of any role from **Users & Roles** (`POST /api/users`). Both go through `ThunderIdIdentityDirectory` (`backend/Features/Identity/ThunderIdIdentityDirectory.cs`), which calls ThunderID's management API as the backend's own registered application (`client_credentials`): `POST /users`, then `POST /roles/{roleId}/assignments/add` for the chosen role.
- **Passwords.** CoreGrid stores no password. A user who forgot theirs — or wants to change it from **My Profile** — goes through ThunderID's hosted recovery flow (step 8). An Administrator can also set a new password for any user; the backend forwards it to ThunderID's `POST /users/{id}/update-credentials` and discards it.

## One-Time Console Setup

Everything below is done once per ThunderID instance, in the console. It does not need repeating per tenant.

### Start ThunderID and PostgreSQL

The quickest route is `./setup.sh` from the repository root (see `CONTRIBUTING.md`). It starts the containers, prints the console password, checks the backend credentials once you have done the steps below, and creates the test accounts. To start the containers by hand:

```bash
make infra-bootstrap   # first time only — docker compose up -d
```

`docker-compose.yml` includes ThunderID's quick-start bundle (`oci://ghcr.io/thunder-id/thunderid-quick-start:latest`) alongside CoreGrid's PostgreSQL. The first run pulls a large image and runs two one-shot containers, `thunderid-db-init` and `thunderid-setup`, which exit normally. The server container is `coregrid-thunderid` (`coregrid-thunderid-1` if it was created by an older checkout).

**To restart later, use `make infra-up` (`docker start` on the server and the database). Never run `docker compose up -d` again.** Re-running `up` re-executes the one-shot setup container against an already-initialised volume, and it fails with a user-type conflict.

**The admin password is random, not `admin`.** It is printed once to the setup container's logs (`docker logs coregrid-thunderid-setup-1`), and `setup.sh` prints it for you:

```
Admin credentials:
  Username: admin
  Password: <random string>
```

Console: `https://localhost:8090/console`.

**Reference configuration.** [`infra/thunderid/coregrid.yaml`](../../infra/thunderid/coregrid.yaml) is an export of a ThunderID instance configured by these steps. It shows every resource, ID and token attribute below, so you can check your console setup against it. It is not loaded automatically; see [`infra/thunderid/README.md`](../../infra/thunderid/README.md).

### 1. Create the CoreGridUser Type

**User Types** → create one type covering all four CoreGrid roles (the claim contract is uniform across them — [SRS §4.4](../srs/04-identity-and-access-management.md#44-token-model-and-claim-contract)):

| Field | Value |
|---|---|
| Name | CoreGridUser |
| Self-Registration | Disabled |

Attributes:

| Property Name | Display Name | Type | Required | Unique | Credential |
|---|---|---|---|---|---|
| email | Email Address | String | Yes | Yes | No |
| username | Username | String | Yes | Yes | No |
| given_name | First Name | String | Yes | No | No |
| family_name | Last Name | String | Yes | No | No |
| password | Password | String | Yes | No | Yes |

**`username` is required even though `email` is the real identifier.** ThunderID's built-in "Username & Password" sign-in method doesn't dynamically pick whichever attribute is marked Unique — its default flow looks up the literal attribute key `username` (ThunderID's own Go source, `internal/flow/executor/constants.go` / `credentials_auth_executor.go` / `internal/authnprovider/defaultprovider/default_authn_provider.go`). Without a `username` attribute present, that lookup never matches and sign-in fails as "user not found" even for a user that genuinely exists. Its Display Name stays "Username" — the sign-in form's "Username" field is where the user types their email address, since CoreGrid has no separate username of its own. Leave its pattern/regex constraint blank; a restrictive default (e.g. alphanumeric-only) will reject email-shaped values with a schema validation error. `ThunderIdIdentityDirectory` mirrors `email` into `username` on every user it creates (`backend/Features/Identity/ThunderIdIdentityDirectory.cs`); if you create a user by hand in the console instead, set `username` to the same value as `email` yourself.

Don't reuse the built-in `Person` type — it can't be added to an application's Allowed User Types and never picks up app roles.

`ThunderID:UserType` is the literal name **`CoreGridUser`**, not its ID — ThunderID's `POST /users` takes the type's name, not its UUID.

### 2. Note the Organisation Unit ID

**Organisations** → open the root organisation (exists by default) → note its **Organisation Unit ID**. This is `ThunderID:OuId`.

### 3. Create the Four CoreGrid Roles

**First, rename ThunderID's built-in `Administrator` role to `Admin`**: **Roles** → built-in **Administrator** → **Edit**. Purely cosmetic — assignments are keyed by ID, not name — but it stops you confusing it with the CoreGrid role of the same name you're about to create. (If you'd rather not rename it, tell them apart by description instead: the built-in one has one, "System administrator role with full permissions"; the CoreGrid one below doesn't.)

**Roles** → create: `Administrator`, `InventoryOfficer`, `Auditor`, `Staff`. The names must exactly match `CoreGridRole` (`backend/Domain/CoreGridRole.cs`) — a literal string comparison. **Note down each role's ID** — `ThunderID:RoleIds:*` needs all four; creating a user assigns the role chosen for them.

These are CoreGrid's own roles, assigned to **users**. They're a separate object from the built-in role you just renamed, which instead gets assigned to the *backend application* in step 6 (see [Role Assignments](#role-assignments-users-vs-applications) below).

### 4. Create the Frontend Application

**Applications** → **New Application** → **Choose a type** → **Single-Page Application**. The React app is a public PKCE client (SRS §4.4), not a confidential/server-rendered one — the wrong type here changes which fields the rest of the wizard offers.

Then, in order:

- **Details screen**: Name & Logo → **CoreGrid Frontend** (paired with **CoreGrid Backend**, step 6). Leave **Allow all user types** **off** and select **CoreGridUser** explicitly — the reason that type exists (step 1).
- **Sign-in method screen**: check **Username & Password** only — it matches `CoreGridUser`'s `password` attribute. Leave Passwordless/Social/Multi-Factor Login unchecked; none are wired up yet.
- **Note the Client ID, not the Application ID** — only the Client ID is a valid OAuth `client_id`.
- Application URL / Redirect URI / Post-logout redirect URI: all `http://localhost:5173`.
- **Allowed User Types** (Access) → `CoreGridUser`, if not already carried over from the details screen.
- **Token Attributes and Response** → Access Token: add `email`, `given_name`, `family_name`, `roles`. `roles` here is for the **backend** (step 6's `[Authorize(Roles = ...)]` checks, via `RoleClaimType` in `Program.cs`) — the **frontend** doesn't read a `roles` claim from ThunderID at all. It resolves the signed-in user's role from `GET /api/me`, which reads CoreGrid's own `Users.Role` column (`backend/Features/Me/MeController.cs`), specifically so frontend routing doesn't depend on ThunderID's own claim/token wiring being exactly right.
- **Scopes**: under **Token → User → Scopes & User Attribute Mappings** — not a separate "Scopes" tab. `openid`/`profile`/`email` are pre-added; add `roles` too (type it in if it's not offered as a suggestion) — still needed so the *backend's* client_credentials-independent, per-request bearer tokens carry it. This entirely determines what lands in the token; the frontend has no client-side scopes config to match it against (`@thunderid/react`'s `scopes` prop isn't wired to anything in the installed SDK version).
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
- **No scopes to configure here.** This application type has no per-app scope list in the console — don't go looking for a `system` scope to create or activate. `ThunderIdIdentityDirectory` sends `scope=system` on the token request regardless (`backend/Features/Identity/ThunderIdIdentityDirectory.cs`), but what actually authorizes the token is the resource/audience it's issued against (step 7) plus the role assignment below.
- **Roles** → built-in **Administrator** (`Admin`, if renamed in step 3) → **Assignments** → add this application. Without this, every management call fails with `403 Forbidden`.

### Role Assignments: Users vs. Applications

Two unrelated, same-named concepts — keep them apart:

| | Assigned to | Assigned via | Used by |
|---|---|---|---|
| **CoreGrid roles** (`Administrator`, `InventoryOfficer`, `Auditor`, `Staff` — step 3) | **Users** (frontend sign-ins) | **Roles** → role → **Assignments** → add the user | Lands in the `roles` claim, read by the backend's `[Authorize(Roles = ...)]` checks. The frontend does **not** read this claim — see step 4's note on `GET /api/me` |
| ThunderID's built-in **Administrator** role | **Applications** (the backend service app, step 6, only) | **Roles** → built-in Administrator → **Assignments** → add the application | Grants the backend's `client_credentials` token its `system` permission |

The frontend application itself never gets a role — only the users who sign into it do. Every user CoreGrid creates — the first Administrator during Setup, and any role from **Users & Roles** — is assigned its role automatically (`POST /users` then `POST /roles/{roleId}/assignments/add`, see `ThunderIdIdentityDirectory.cs`). **Changing** a user's role in CoreGrid updates only CoreGrid's `Users.Role`, which is what the frontend reads; the ThunderID role assignment (and so the `roles` claim the backend checks) isn't moved with it — update it in the console too.

### 7. The Resource: Built-In `System` Resource Server, or a Custom One

**Resource Servers** define what `resource`/`audience` a token can be issued for. List columns: **Name**, **Type** (`System` = built-in, `Custom` = yours), **Identifier** (an absolute URI — becomes the `aud` claim), and **Actions** (just the row's Edit/Delete buttons, not permissions, despite the name — permissions live inside a resource server's own **Resources** tab). `client_credentials` requests fail with `invalid_target` without a `resource` matching one of these Identifiers.

**Default: reuse the built-in `System` resource server — don't create a new one.** Open it, note its **Identifier** (`https://<host>/mcp`, e.g. `https://localhost:8090/mcp`) — this is `ThunderID:Resource`. It's what the built-in Administrator role's `system` permission is bound to, so reusing it is what makes step 6's role assignment take effect.

**Only create a custom resource server** if you deliberately want the backend's permissions scoped away from ThunderID's built-in semantics: **Resource Servers** → new → **Type: Custom** → Name + Identifier (any absolute URI, e.g. `https://api.coregrid.local/backend`) → define permissions in its **Resources** tab. Then point the backend application's Default Audience (or the `resource` it requests) and `ThunderID__Resource` at the new Identifier instead. More setup for no functional gain in the current single-tenant M0 deployment — stick with the default.

### The Agents Don't Register With ThunderID

The four agents run in-process inside the API (ADR-010) and call their tools directly through their in-process tool interfaces (`IPlannerTools`, `IMaintenanceTools`, `IBudgetTools`, `IPolicyTools`), so they need no ThunderID application, client credentials or shared secret. Only the model key (`Llm__ApiKey`) is configured — see [`ai-agents.md`](ai-agents.md).

### 8. Enable Password Recovery ("Forgot password?" and "Change password")

Password recovery is ThunderID's own hosted RECOVERY flow — CoreGrid never sees the password. Run once per ThunderID instance; it's idempotent, so re-running is safe:

```bash
make thunderid-recovery   # runs scripts/thunderid/enable-password-recovery.sh
```

It reads the backend app's credentials from `backend/.env` (or `THUNDERID_CLIENT_ID` / `THUNDERID_CLIENT_SECRET`), then:

1. creates a **CoreGrid Recovery Flow** — a copy of ThunderID's default recovery flow whose "Back to sign in" returns to the CoreGrid Frontend sign-in flow instead of ThunderID's generic one;
2. adds a **Forgot password?** link, just above the Sign In button, to the CoreGrid Frontend sign-in flow;
3. sets the application's `recoveryFlowId` and `isRecoveryFlowEnabled: true`. Until this is set, ThunderID refuses with `FES-1009 Recovery not allowed`.

Put the **Application ID** it prints (not the Client ID) into `VITE_THUNDERID_APPLICATION_ID` in `frontend/.env`, and restart Vite. CoreGrid then links straight to `https://<thunderid>/gate/recovery?applicationId=…` from two places:

| Where | Who | Notes |
|---|---|---|
| `/forgot-password` (linked from CoreGrid's sign-in card) | Anyone signed out | |
| **My Profile → Password → Change password** | Every signed-in web user — Administrator, Inventory Officer, Auditor | Opens in a new tab; the current CoreGrid session stays signed in |

Without `VITE_THUNDERID_APPLICATION_ID`, both places tell the user to use **Forgot password?** on ThunderID's own sign-in page instead, which step 2 added.

The flow is the same for every role: enter the sign-in email → receive a single-use link → choose a new password. ThunderID identifies the user by `username`, which CoreGrid sets to the email (step 1). For an unknown email it still shows "check your email", so it doesn't reveal which addresses have accounts.

**Email delivery — not configured yet.** The link is sent by ThunderID's own SMTP client, configured in the `email.smtp` block of its `/opt/thunderid/deployment.yaml`. The image default points at `127.0.0.1:2525`, where nothing listens, so today the flow runs end to end but no email arrives. Configuring a real SMTP relay is part of deploying ThunderID and is deferred until then; until it's done, use the Administrator reset below.

**Administrator reset.** An Administrator can also set a new password for any user, themselves included, from **Users & Roles → ⋯ → Reset password**. This is `POST /api/users/{id}/reset-password` (`CanManageUsers`, 8–200 characters), which calls ThunderID's `POST /users/{id}/update-credentials`; the old password stops working immediately. Use it when the user can't receive the recovery email.

## Environment Variables

**Backend.** Every value goes in **`backend/.env`**, which is git-ignored and loaded by `Program.cs`; `appsettings*.json` hold only logging. `backend/.env.example` already contains the IDs of the reference configuration, so on a matching instance only the client secret needs filling in.

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

- Everything is `https://`, because ThunderID doesn't serve plain HTTP by default.
- `ThunderID__Issuer` is the bare server URL, with no path. `AddJwtBearer` resolves the JWKS and token URLs from it.
- **There is no `ThunderID__Audience`.** Inbound token validation checks only the issuer and the RS256 signature. `ThunderID__Resource` is unrelated: it is only for the backend's own outbound `client_credentials` calls.
- Never put `ThunderID__ScimClientSecret` in a committed file. `backend/.env` is git-ignored.
- TLS verification for the self-signed certificate is relaxed only in Development; `Program.cs` already gates this on `IsDevelopment()`.

**Frontend** (`frontend/.env`, created from `frontend/.env.example` by `setup.sh` or `make env`):

```dotenv
VITE_THUNDERID_CLIENT_ID=<frontend Client ID, step 4>
VITE_THUNDERID_APPLICATION_ID=<frontend Application ID, printed by step 8>
VITE_THUNDERID_BASE_URL=https://localhost:8090
VITE_THUNDERID_AFTER_SIGN_IN_URL=http://localhost:5173
VITE_THUNDERID_AFTER_SIGN_OUT_URL=http://localhost:5173
```

`AFTER_SIGN_IN_URL` and `AFTER_SIGN_OUT_URL` must match the redirect URI from step 4 exactly.

## Test Accounts

Once the backend credentials work, `setup.sh` creates one account per role, all with password `Login@123456`:
- `admin@coregrid.test`, created through first-run Setup together with the organisation;
- `officer@coregrid.test`, `auditor@coregrid.test` and `staff@coregrid.test`, created in ThunderID exactly as `ThunderIdIdentityDirectory` does (`POST /users`, then `POST /roles/{id}/assignments/add`). CoreGrid creates their local user record on first sign-in.

These are for local development only. Never create them on a shared or deployed instance.

## Known Gaps

- **Local-identity fallback** (SRS §4.10) isn't built — `IIdentityDirectory` exists as the seam for it, but only `ThunderIdIdentityDirectory` exists today.
- **No "current password + new password" form.** Changing a password from My Profile goes through the emailed recovery link. Checking the current password inside CoreGrid would need ThunderID's Direct API (`POST /auth/credentials/authenticate`, gated by its `Direct-Auth-Secret`), which isn't wired up; ThunderID's own `POST /users/me/update-credentials` doesn't check the current password, so it isn't used.
- **Recovery emails aren't delivered yet.** ThunderID's SMTP is still the image default; set it up when ThunderID is deployed (see step 8).
- **Mobile app recovery isn't enabled.** Step 8 only changes the **CoreGrid Frontend** application and its sign-in flow; the **CoreGrid Mobile** app's flow is untouched.
- **Role changes aren't pushed to ThunderID.** See [Role Assignments](#role-assignments-users-vs-applications).

## Troubleshooting Quick Reference

| Symptom | Likely cause |
|---|---|
| `invalid_target: No resource parameter supplied...` | `ThunderID:Resource` is missing/empty — step 7 |
| `invalid_target: ...must be an absolute URI` | `ThunderID:Resource` isn't the `System` resource server's Identifier — step 7 |
| `403 Forbidden` calling any management API as the backend app | Backend app isn't assigned the built-in Administrator role, or the token request used a scope other than `system` — step 6 |
| `USR-1021: user_type_not_found`, even though the type's ID looks right | `type` in `POST /users` (and `ThunderID:UserType`) must be the type's **name** (`CoreGridUser`), not its ID |
| Sign-in says the user can't be found, but the account genuinely exists | `CoreGridUser` is missing its `username` attribute, or the user's `username` value wasn't set — ThunderID's built-in sign-in method looks up the literal `username` key, not `email` (step 1) |
| `roles` claim present but Administrator-only backend routes still reject the caller | CoreGrid's custom role isn't named exactly `Administrator` (e.g. `Admin`) — exact string match |
| Backend management calls get `403 Forbidden` even with the right scope | Allowed User Types isn't set on the frontend application, or the user has no role assignment — `roles` claim ends up empty |
| Sign-in succeeds but the frontend always lands on `/access-restricted` | The frontend reads role from `GET /api/me`, not from ThunderID — check `Users.Role` for that user in CoreGrid's own database, not the ThunderID console. If `/api/me` 404s, the user's `ExternalSubjectId` doesn't match any `Users` row — they weren't provisioned through Setup or `POST /api/users` |
| Sign-in page says the user can't be found, right after Setup created it | Try again after a `docker restart coregrid-thunderid-1` — the in-memory identifier cache can lag a just-created account |
| `invalid_client` on sign-in | `VITE_THUNDERID_CLIENT_ID` is the Application ID, not the Client ID |
| `key not found` / endless JWKS retries | Issuer is `http://` instead of `https://` |
| `certificate signed by unknown authority` | Relax TLS verification for local dev against the self-signed cert |
| `token has invalid issuer` | Issuer value includes a path; should be the bare server URL |
| "Forgot password?" says recovery isn't allowed (`FES-1009`) | Recovery isn't enabled on the application — run step 8 |
| Recovery says the email was sent but nothing arrives | ThunderID's SMTP isn't configured (its image default points at `127.0.0.1:2525`) — expected until deployment; reset the password from Users & Roles instead |
| CORS errors on `/oauth2/token` or `/flow/meta` | Frontend origin isn't in `cors` server-config's `allowedOrigins` — step 5 |
| Signing out doesn't return to the app | Post-logout redirect URI isn't set — step 4 |
| All data disappeared after a restart | `docker compose down -v` was used instead of `make infra-stop` / `make infra-up` |
| `thunderid-setup` fails with a user-type conflict after a restart | `docker compose up -d` was re-run against an initialised volume — use `make infra-up` |
| Multiple ThunderID stacks, `invalid_client` for no obvious reason | Compose was run without `-p coregrid` from another directory. `docker compose ls`, `docker ps -a \| grep coregrid` to find and consolidate |
