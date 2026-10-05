# 14. Deployment and Operations

## 14.1 Environments

| Environment | Purpose | Data | Identity |
|---|---|---|---|
| Local development | Feature development and unit testing. | Seeded local PostgreSQL; agent service run locally. | ThunderID development application with localhost redirect URIs. |
| Continuous integration | Automated verification on every push and pull request. | Ephemeral PostgreSQL service container, migrated and seeded per run. | Not required; the agent service and identity are stubbed. |
| Staging / production | User acceptance, demonstration, performance measurement and live use. | Managed PostgreSQL with restricted credentials; demonstration seed in staging only. | ThunderID production application with the deployed origins registered. |

## 14.2 Startup Order and Configuration

```
  1  PostgreSQL available            → connection string configured
  2  EF Core migrations applied      → dotnet ef database update
  3  Seed data applied               → idempotent seeder on first start
  4  ASP.NET Core API started        → ThunderID issuer/resource/SCIM
                                       credential, CORS origins, model
                                       key, object-storage credentials
                                       (the agents run in-process, ADR-010)
  5  First-run Setup                 → organisation + first Administrator
  6  React static build published    → API base URL, ThunderID client id,
                                       redirect URI baked at build time
  7  Flutter APK built               → API base URL, ThunderID client id,
                                       custom-scheme redirect

  Required environment variables (names only; values never committed):
    ConnectionStrings__CoreGrid       Cors__AllowedOrigins__0
    ThunderID__Issuer                 ThunderID__Resource
    ThunderID__OuId                   ThunderID__RoleIds__<Role>
    ThunderID__ScimClientId           ThunderID__ScimClientSecret
    Llm__ApiKey (optional Llm__Endpoint, Llm__Model)
    LlmFallback__ApiKey (optional fallback model provider)
    CloudflareR2__AccountId           CloudflareR2__AccessKeyId
    CloudflareR2__SecretAccessKey     CloudflareR2__BucketName
```

## 14.3 Reference Demo Deployment

The public demo runs entirely on free tiers: the React build on Vercel (https://demo-coregrid.vercel.app); the API and ThunderID as two Render web services; and two separate Neon PostgreSQL databases, one for CoreGrid and one for ThunderID. ThunderID runs statelessly, with its data in PostgreSQL and its keys supplied as secret files. The full configuration and its known limits are in [`docs/setup/deployment.md`](../setup/deployment.md).

## 14.4 Operational Requirements

| ID | Requirement |
|---|---|
| OPS-01 | The API shall expose `/health` reporting the status of the database, the agent service and the identity provider individually. |
| OPS-02 | The API shall expose `/swagger` with the complete operation set, request and response schemas, and security definitions. |
| OPS-03 | Structured logs shall be emitted with correlation identifiers, and shall never contain tokens, credentials or personal data beyond a subject identifier. |
| OPS-04 | Migrations shall be applied automatically on start-up in staging environments, and the seeder shall be idempotent so that a restart does not duplicate data. |
| OPS-05 | Staging shall provide a documented test account for each of the four roles. The public demo's shared accounts are listed in the README; a real customer deployment distributes credentials out of band and never stores them in the repository. |
| OPS-06 | Every public URL of a release (web application, API health and Swagger) shall be verified in a private browsing session before the release is announced. |
| OPS-07 | A rollback path shall exist: the previous container image and the corresponding migration state shall be identified in the deployment report. |
