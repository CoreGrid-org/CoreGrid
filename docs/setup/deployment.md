# Deployment (Live Demo)

CoreGrid is self-hosted and runs on any container host. This guide describes the public demo deployment, which uses only free tiers, and how to reproduce it.

## Live demo

| | Link |
|---|---|
| Web app | https://demo-coregrid.vercel.app |
| API health | https://coregrid-v7jn.onrender.com/health |
| API reference (Swagger) | https://coregrid-v7jn.onrender.com/swagger |
| Sign-in (ThunderID) | https://coregrid-1.onrender.com |
| Project site | the **Live Demo** page of `coregrid-web` |

Demo accounts. The password for all of them is `Login@123456`:

| Account | Role | Client |
|---|---|---|
| `admin@coregrid.test` | Administrator | Web |
| `officer@coregrid.test` | Inventory Officer | Web and mobile |
| `auditor@coregrid.test` | Auditor | Web |
| `staff@coregrid.test` | Department Staff | Mobile only |

The Render free tier sleeps after about 15 minutes without traffic. The first request after that takes up to a minute while the API and ThunderID start again.

## Topology

![CoreGrid live demo deployment topology](../diagrams/deployment%20topology.png)

| Part | Platform | Notes |
|---|---|---|
| React web app | Vercel | Static Vite build of `frontend/`. `VITE_*` values are read at build time, so redeploy after changing them. |
| ASP.NET Core API | Render web service (Docker) | Built from `backend/Dockerfile`, port 8080. |
| ThunderID | Render web service (Docker) | Built from `infra/thunderid/render/`; see below. |
| CoreGrid database | Neon PostgreSQL, database `coregrid-backend` | Used only by the API. |
| ThunderID database | Neon PostgreSQL, a separate project | Holds ThunderID's users, apps, roles and flows. |
| Photo storage | Cloudflare R2 | Private bucket, reached only by the API. |
| AI model | Gemini (primary), Groq (fallback) | Called only by the API's Planner and Budget agents. |

There are two Neon databases, one for CoreGrid and one for ThunderID. They are never shared. This matches the self-hosted model, where each component owns its own data.

## ThunderID on Render

ThunderID's quick-start image keeps its data in SQLite files on disk, but Render's free tier has no persistent disk. The demo therefore runs ThunderID with a different configuration:

- **Data in PostgreSQL.** ThunderID's four stores (`config`, `entity`, `runtime_persistent`, `runtime_transient`) point at the Neon ThunderID database. The image ships the schema in `/opt/thunderid/dbscripts/*/postgres.sql`.
- **HTTP inside, HTTPS outside.** `server.http_only: true` is set because Render terminates TLS, and `server.public_url` is the Render URL. The public URL becomes the token issuer, so it must equal the API's `ThunderID__Issuer`.
- **Keys and config as Render Secret Files.** `infra/thunderid/render/render-start.sh` copies them from `/etc/secrets` into place at start-up, so nothing secret is in the image or the repository:
  - `deployment.yaml`
  - `server.cert`, `server.key`
  - `signing.cert`, `signing.key`
  - `ecdsa-signing.cert`, `ecdsa-signing.key`
  - `crypto.key`
  - `direct_auth_secret`

Render settings for the ThunderID service: Root Directory `infra/thunderid/render`, Dockerfile Path `./Dockerfile`, Build Context `.`, and environment variable `PORT=8090`.

### One-time setup

1. Create the ThunderID database's tables by running the four `postgres.sql` scripts from the image against the Neon database.
2. Run the image's `./setup.sh` once against that database, with `deployment.yaml` and empty `config/certs` and `config/secrets` folders mounted. It generates the keys and creates the default resources and the console `admin` user. Keep the generated keys safe. Losing them invalidates every issued token.
3. Import the CoreGrid configuration from `infra/thunderid/coregrid.yaml`, with the URLs changed to the deployed ones. The import goes through **Import Configuration** in the console or `POST /import` with an admin token. It creates the `CoreGridUser` type, the four roles, the Frontend, Backend and Mobile applications, the flows and CORS.
   - Leave the built-in **System** resource server's identifier as `setup.sh` created it (`https://localhost:8090/mcp`), and set `ThunderID__Resource` to the same value. It is only an identifier; nothing calls it.
   - **`POST /import` ignores `dryRun`: every call is applied.** Never test an import against an instance you want to keep unchanged.

## API environment variables (Render)

The names are the same as in `backend/.env.example`:

- `PORT=8080`, `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`
- `ConnectionStrings__CoreGrid`: the Neon `coregrid-backend` database, in Npgsql form (`Host=…;Database=…;Username=…;Password=…;SSL Mode=Require`). Npgsql does not accept `postgresql://` URLs. Use the direct host, without `-pooler`.
- `Cors__AllowedOrigins__0=https://demo-coregrid.vercel.app`
- `ThunderID__Issuer`, `ThunderID__Resource`, `ThunderID__OuId`, `ThunderID__UserType`, `ThunderID__RoleIds__<Role>`, `ThunderID__ScimClientId`, `ThunderID__ScimClientSecret`
- `Llm__ApiKey`, `LlmFallback__ApiKey`
- `CloudflareR2__AccountId`, `CloudflareR2__AccessKeyId`, `CloudflareR2__SecretAccessKey`, `CloudflareR2__BucketName`

The API does not migrate on start-up. Apply the schema once, and again after each new migration:

```bash
cd backend && dotnet ef migrations script --idempotent -o deploy.sql
psql "<neon coregrid-backend url>" -f deploy.sql
```

Then call `POST /api/setup/complete` once, or use the web app's Setup page. This creates the organisation and the first Administrator.

## Web app environment variables (Vercel)

`VITE_API_URL` (`https://coregrid-v7jn.onrender.com/api`), `VITE_THUNDERID_BASE_URL`, `VITE_THUNDERID_CLIENT_ID`, `VITE_THUNDERID_APPLICATION_ID`, `VITE_THUNDERID_AFTER_SIGN_IN_URL`, `VITE_THUNDERID_AFTER_SIGN_OUT_URL`.

## Mobile app

Build the APK against the demo with `--dart-define-from-file`, using a JSON file with `API_BASE_URL` (`https://coregrid-v7jn.onrender.com`), `THUNDERID_ISSUER` (`https://coregrid-1.onrender.com`), `THUNDERID_CLIENT_ID` and `THUNDERID_APPLICATION_ID`:

```bash
flutter build apk --release --dart-define-from-file=<env>.json
```

## Known limits of the demo

- **Swagger** is public, so anyone can read the API reference. To call an endpoint from it, you still need a ThunderID access token (**Authorize** → paste the token).
- **Email:** ThunderID's password-recovery email is not configured. An Administrator can reset passwords from **Users & Roles** instead.
