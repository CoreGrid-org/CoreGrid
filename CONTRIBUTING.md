# Contributing to CoreGrid

Thank you for your interest in contributing to CoreGrid. This guide walks you through setting up the full development environment from scratch.

---

## Prerequisites

Install the following before starting:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/) with npm
- [Docker](https://docs.docker.com/get-docker/) and Docker Compose

Install the EF Core CLI tool (used for migrations):

```bash
dotnet tool install --global dotnet-ef
```

Make sure `~/.dotnet/tools` is on your `PATH` — the installer tells you if it isn't.

There's no separate codegen step for API docs or database queries: Swagger is generated automatically from the ASP.NET Core controllers at runtime, and there's no SQL query-builder codegen — CoreGrid uses EF Core directly.

---

## 1. Clone the Repository

```bash
git clone https://github.com/CoreGrid-org/CoreGrid.git
cd CoreGrid
```

---

## 2. Start Infrastructure (ThunderID + PostgreSQL)

**On a machine that's never run this project's containers before**, bootstrap ThunderID on its own first:

```bash
docker compose -f oci://ghcr.io/thunder-id/thunderid-quick-start:latest -p coregrid up -d
```

Then, from the repo root, bring up CoreGrid's own database alongside it:

```bash
docker compose up -d
```

Together these give you two running containers: `coregrid-thunderid-1` (identity, `https://localhost:8090`) and `coregrid-postgres` (CoreGrid's own application database, host port `5433` — not the Postgres default `5432`, to avoid colliding with another local project's Postgres). See [`doc/setup/ThunderID.md`](./doc/setup/ThunderID.md#start-thunderid-and-postgresql) for why it's two commands the first time, not one.

**The first `docker compose -f oci://...` run pulls the ThunderID image, which is large and can take several minutes** — this is normal, not a hang. After that first bootstrap, everything is local: to stop without losing data, `docker compose stop`; to restart, `docker compose start` (don't re-run either `up -d` command against an existing volume unless you mean to — see the troubleshooting table in [`doc/setup/ThunderID.md`](./doc/setup/ThunderID.md)).

---

## 3. Set Up ThunderID

Follow [`doc/setup/ThunderID.md`](./doc/setup/ThunderID.md) for the full one-time console setup — creating the `CoreGridUser` type and the four CoreGrid roles, registering the frontend application, allowing the frontend's CORS origin, and creating the backend's service credential. Come back here once that's done.

---

## 4. Backend Setup

```bash
cd backend
dotnet restore
```

### Apply Database Migrations

```bash
dotnet ef database update
```

Schema is managed exclusively by these EF Core migrations (SRS §2.3, C-02) — see [`backend/db/README.md`](./backend/db/README.md) if you also want the plain numbered `.sql` files that get generated from them for review.

### Configure ThunderID Credentials

`backend/appsettings.Development.json` already has the local Postgres connection string, the frontend's CORS origin, and every non-secret ThunderID value filled in (`Issuer`, `Resource`, `OuId`, `UserType`, `RoleIds`, `ScimClientId`) — nothing to fill in there yourself unless you set up a fresh ThunderID instance. The one secret, `ScimClientSecret`, is never committed — set it with `dotnet user-secrets`, matching SRS §14.2's naming:

```bash
cd backend
dotnet user-secrets set "ThunderID:ScimClientSecret" "<Backend Service Client Secret from doc/setup/ThunderID.md>"
```

See [`doc/setup/ThunderID.md`](./doc/setup/ThunderID.md) for what each of these values is and where it comes from in the console — including the `username` attribute `CoreGridUser` needs for sign-in to resolve at all, a common first-time gotcha.

### Start the Backend

```bash
dotnet run
```

The backend runs on `http://localhost:5083`. Swagger UI is available at `http://localhost:5083/swagger`.

---

## 5. Frontend Setup

```bash
cd frontend
npm install
cp .env.example .env
```

Fill in `.env` with the values from your ThunderID frontend application (see [`doc/setup/ThunderID.md`](./doc/setup/ThunderID.md)) — `VITE_API_URL` is already correct as-is for a default local backend:

```env
VITE_API_URL=http://localhost:5083/api

VITE_THUNDERID_CLIENT_ID=<frontend Client ID>
VITE_THUNDERID_BASE_URL=https://localhost:8090
VITE_THUNDERID_AFTER_SIGN_IN_URL=http://localhost:5173
VITE_THUNDERID_AFTER_SIGN_OUT_URL=http://localhost:5173
```

These are read by `ThunderIDProvider` in `frontend/src/main.tsx`. `.env` is gitignored, same as `.env.local` would be — either name works with Vite, but `.env` is what the rest of the team actually uses, so stick with it for consistency.

Start the frontend:

```bash
npm run dev
```

The frontend runs on `http://localhost:5173`.

---

## 6. Verify Everything is Running

| Service | URL |
|---|---|
| Frontend | http://localhost:5173 |
| Backend | http://localhost:5083 |
| Swagger UI | http://localhost:5083/swagger |
| ThunderID Console | https://localhost:8090/console |
| PostgreSQL | localhost:5433 |

---

## 7. Try It Out

1. Go to `http://localhost:5173`.
2. On a fresh database, you'll be redirected through sign-in straight to `/setup` — `GET /api/setup/status` genuinely checks whether any organisation exists yet.
3. Fill in the admin account and organisation details and submit. This provisions a real ThunderID account (`Identity/ThunderIdIdentityDirectory.cs`) and creates the matching `Organizations`/`Users` rows locally.
4. Sign in with that account. `/` resolves your role from the `roles` claim and sends you to the matching dashboard — an Administrator lands on `/admin`. Other roles aren't provisionable through the UI yet (see [`doc/PROGRESS.md`](./doc/PROGRESS.md)), so `/admin` is the only one worth exercising today.
5. From `/admin` → **Users & Roles**, an Administrator can invite further users by email and role (FR-013) — everything else on the Admin Dashboard is a mock/placeholder page; see [Project Structure](#project-structure) below for which parts are real.

---

## Project Structure

### Backend (`backend/`)

Feature-folder layout, one folder per business capability:

```
backend/
  Domain/                    Entities and enums (User, Organization, CoreGridRole) — no behaviour
  Data/                      CoreGridDbContext, EF Core model configuration
  Identity/                  IIdentityDirectory + ThunderIdIdentityDirectory (the ThunderID management-API client)
  Features/
    Setup/                   SetupController + SetupModels — the one unauthenticated write path
    Users/                   UsersController + UsersModels — Administrator-only user administration
  Migrations/                EF Core migrations — the schema source of truth (SRS §2.3, C-02)
  db/                        Generated, readable SQL exports of the migrations — see db/README.md; never hand-edited
```

A new business component (Assets, Maintenance, Transfers, Disposals, Audit) gets its own `Features/<Name>/` folder with a `<Name>Controller.cs` and a `<Name>Models.cs`, following `Features/Users/` as the template. Add its entities to `Domain/`, register them as `DbSet`s in `CoreGridDbContext`, then run a migration (below).

### Frontend (`frontend/src/`)

```
frontend/src/
  app/App.tsx                The route table — the composition root
  features/
    auth/                    Sign-in, role routing/guards, the CoreGridRole type
      components/  hooks/  lib/  pages/  services/
    setup/                   First-run organisation + admin setup
    dashboard/                Post-login landing + per-role dashboards
    users/                   Real Users & Roles feature (list + invite)
  shared/                     Cross-feature reusable pieces
    components/  hooks/  lib/  pages/
  styles/index.scss           Carbon overrides + CoreGrid's own BEM-ish classes (cg-*)
  main.tsx                    Vite entry point — ThunderIDProvider + BrowserRouter
```

A new feature gets its own `features/<name>/` folder with the same internal shape (`components/`, `hooks/`, `pages/`, `services/`) as `features/users/` — copy that one as the template.

**Import convention:** the `@/` alias (configured in `tsconfig.app.json` and `vite.config.ts`) maps to `frontend/src/`. Use relative imports (`../hooks/useX`) for anything inside the same feature folder; use the `@/` alias (`@/shared/...`, `@/features/auth/...`) whenever you're crossing into another feature or into `shared/` — it keeps cross-boundary dependencies visible at a glance instead of buried in `../../../` chains.

---

## Code Comments

Default to **no comments** — a well-named function, variable and file already say what the code does. Add one only when it captures something the code can't say for itself:

- a non-obvious **why** (a constraint from ThunderID, an SRS requirement, a workaround for a specific bug)
- a hidden **invariant** a future change could silently break
- something genuinely surprising about the behaviour

Don't write a comment that just restates what the next line does, and don't reference the current ticket, PR, or "fix" in a comment — that belongs in the commit message and goes stale the moment the code moves on. If you're tempted to explain *what* a block does, that's usually a sign it should be a better-named function instead.

---

## Development Workflow

### Making changes to the database schema

1. Add or edit an entity under `backend/Domain/`.
2. Update `backend/Data/CoreGridDbContext.cs` if the change affects relationships, indexes, or constraints.
3. `dotnet ef migrations add <DescriptiveName>`
4. `dotnet ef database update`
5. Regenerate the readable SQL export — see [`backend/db/README.md`](./backend/db/README.md).

### Making changes to endpoints

1. Add or update a controller under `backend/Features/`.
2. That's it — Swagger picks up new routes automatically from the controller, no separate generation step.

### Before You Push

All of these should pass before you open or update a PR — none of them are optional:

1. `dotnet build` from `backend/` — zero warnings, zero errors.
2. `npx tsc -b --force` from `frontend/` — zero errors.
3. `npm run build` from `frontend/` — the production build has to actually succeed, not just type-check.
4. **Exercise the change in a real browser** against a running backend + ThunderID. A green build proves the code compiles, not that the feature works — click through the actual flow you changed.
5. If your change completes or advances an item in [`doc/PROGRESS.md`](./doc/PROGRESS.md), tick it in the same PR.
6. Reference the requirement ID (e.g. `FR-013`) your change implements in the commit message or PR description, per [SRS §12.1](./doc/SRS/12-individual-contribution-and-work-allocation.md#121-contribution-evidence-requirements) — that's what lets a requirement be traced to code, tests and a reviewer.

### Branch and PR conventions

- Create a feature branch from `development`, named for your component: `feature/<component>-<short-description>` (e.g. `feature/assets-qr-generation`).
- All PRs target the `development` branch.
- Use the PR template ([`.github/pull_request_template.md`](./.github/pull_request_template.md)) — it's applied automatically when you open a PR on GitHub.
- Every PR needs at least one review from another member before merge (SRS §12.1) — no self-merging.

---

## Need Help?

- Open an [issue](https://github.com/CoreGrid-org/CoreGrid/issues)
- See the full [SRS](./doc/SRS/00-front-matter.md) for the system's requirements and architecture
- See [`doc/PROGRESS.md`](./doc/PROGRESS.md) for what's actually built versus still planned
