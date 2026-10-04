# CoreGrid Web Application

CoreGrid's authenticated management application, used for administration, back-office work, dashboards, reporting, and agent-workflow monitoring and approval. It talks only to the CoreGrid ASP.NET Core API; users sign in through ThunderID (OIDC with PKCE).

The public static site (features, user manual, changelog) is a separate project (`CoreGrid-org/coregrid-web`).

React 19 + TypeScript + Vite, React Router 7, IBM Carbon Design System.

## Structure

| Path | Contents |
|---|---|
| `src/app/App.tsx` | Route tree: one layout per web role (`/admin`, `/inventory`, `/audit`), each guarded by `RoleRoute`. Staff accounts are mobile-only and are sent to an *Access restricted* page |
| `src/features/<name>/` | One folder per feature (`assets`, `maintenance`, `transfers`, `audit`, `workflows`, `users`, `settings`, `reports`, `dashboard`, `notifications`, `setup`, `profile`, `auth`), each with `api/`, `hooks/`, `components/`, `pages/`, `lib/` |
| `src/features/auth/lib/permissions.ts` | `usePermissions()` mirrors the API's role grants, so every page shows or hides actions the same way. The API is still the enforcement point |
| `src/shared/` | Reusable components, hooks (`useStubMutation`), and helpers (dates, error messages, status tags) |
| `src/styles/index.scss` | Carbon theme tokens and shared layout classes; no hard-coded colours in features |

## State management

- **Server state:** feature-scoped custom hooks (`useWorkflowsList`, …) built on `useState`/`useEffect`, with cancellation guards and a `refetch()`. Mutations use the shared `useStubMutation` helper, which exposes `isPending`/`isError`/`data`.
- **Session:** the ThunderID React SDK provider (`useThunderID()`, `getAccessToken()`).
- **Route state:** React Router.

See ADR-003 in [`docs/architecture/decision-records.md`](../docs/architecture/decision-records.md).

## Configuration

Copy `.env.example` to `.env` (or run `make env` from the repository root):

| Variable | Purpose |
|---|---|
| `VITE_API_URL` | API base URL, e.g. `http://localhost:5083/api` |
| `VITE_THUNDERID_BASE_URL`, `VITE_THUNDERID_CLIENT_ID` | ThunderID tenant and application |
| `VITE_THUNDERID_APPLICATION_ID` | Enables the "Forgot password?" page |
| `VITE_THUNDERID_AFTER_SIGN_IN_URL`, `VITE_THUNDERID_AFTER_SIGN_OUT_URL` | Redirect URIs |

These values are baked in at build time.

## Develop

```bash
npm install
npm run dev       # http://localhost:5173
npm test          # Vitest + React Testing Library
npm run build     # tsc -b, then vite build
npm run lint
```

## Production image

`Dockerfile` builds the static bundle and serves it with nginx (`nginx.conf`, which provides SPA fallback routing). Pass the `VITE_*` values as build args (`make docker-build-frontend`). Any static host that rewrites unknown paths to `index.html` also works.

## Report exports

PDF exports use `jspdf` (with `html2canvas` for charts). CSV exports are generated in the browser from the same filtered data.

## Assets

- `public/CoreGrid.png` — official logo (full colour, hexagon skyline mark).
- `public/assets/w-coregrid.webp` — white, transparent-background logo for dark surfaces.
