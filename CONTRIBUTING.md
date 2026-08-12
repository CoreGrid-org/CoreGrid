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

`backend/appsettings.Development.json` already has the local Postgres connection string and the frontend's CORS origin filled in — nothing to do there. The backend doesn't validate ThunderID tokens or call ThunderID's management API yet (that wiring is still to be built), but when it is, it'll read these from the environment rather than a committed file, matching SRS §14.2's naming:

```bash
export ThunderID__Issuer=https://localhost:8090
export ThunderID__Audience=<see doc/setup/ThunderID.md>
export ThunderID__ScimClientId=<Backend Service Client ID from doc/setup/ThunderID.md>
export ThunderID__ScimClientSecret=<Backend Service Client Secret from doc/setup/ThunderID.md>
```

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
cp .env.example .env.local
```

Fill in `.env.local` with the values from your ThunderID frontend application (see [`doc/setup/ThunderID.md`](./doc/setup/ThunderID.md)) — `VITE_API_URL` is already correct as-is for a default local backend:

```env
VITE_API_URL=http://localhost:5083/api

VITE_THUNDERID_CLIENT_ID=<frontend Client ID>
VITE_THUNDERID_BASE_URL=https://localhost:8090
VITE_THUNDERID_AFTER_SIGN_IN_URL=http://localhost:5173
VITE_THUNDERID_AFTER_SIGN_OUT_URL=http://localhost:5173
```

These are read by `ThunderIDProvider` in `frontend/src/main.tsx`.

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
3. Fill in the admin account and organisation details and submit. **This will currently fail with an error, and that's expected**: the endpoint creates the local `Organizations`/`Users` rows, but the ThunderID-provisioning step it depends on (`Identity/ThunderIdIdentityDirectory.cs`) isn't implemented yet — see the note at the top of [`doc/setup/ThunderID.md`](./doc/setup/ThunderID.md). That's the next piece to build.
4. Once that's wired up, completing setup signs you in and lands you on the dashboard at `/`.

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

### Branch and PR conventions

- Create a feature branch from `development`: `feature/your-feature-name`.
- All PRs target the `development` branch.
- Make sure `dotnet build` (from `backend/`) and `npx tsc -b` (from `frontend/`) both pass before submitting.
- Use the PR template ([`.github/pull_request_template.md`](./.github/pull_request_template.md)) — it's applied automatically when you open a PR on GitHub.

---

## Need Help?

- Open an [issue](https://github.com/CoreGrid-org/CoreGrid/issues)
- See the full [SRS](./doc/SRS/00-front-matter.md) for the system's requirements and architecture
