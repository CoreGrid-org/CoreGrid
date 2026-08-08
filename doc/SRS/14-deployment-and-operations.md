# 14. Deployment and Operations

## 14.1 Environments

| Environment | Purpose | Data | Identity |
|---|---|---|---|
| Local development | Feature development and unit testing. | Seeded local PostgreSQL; agent service run locally. | Asgardeo development application with localhost redirect URIs. |
| Continuous integration | Automated verification on every push and pull request. | Ephemeral PostgreSQL service container, migrated and seeded per run. | Not required; the agent service and identity are stubbed. |
| Deployed evaluation | Evaluator access, demonstration and performance measurement. | Managed PostgreSQL with restricted credentials and the demonstration seed. | Asgardeo production application with the deployed origins registered. |

## 14.2 Startup Order and Configuration

```
  1  PostgreSQL available            → connection string configured
  2  EF Core migrations applied      → dotnet ef database update
  3  Seed data applied               → idempotent seeder on first start
  4  Agent service started           → model credentials, API base URL,
                                       shared secret configured
  5  ASP.NET Core API started        → Asgardeo issuer, audience, SCIM
                                       credential, agent URL + secret,
                                       email API key, CORS origins
  6  React static build published    → API base URL, Asgardeo client id,
                                       redirect URI baked at build time
  7  Flutter APK built               → API base URL, Asgardeo client id,
                                       custom-scheme redirect

  Required environment variables (names only; values never committed):
    ConnectionStrings__CoreGrid        Asgardeo__Issuer
    Asgardeo__Audience                 Asgardeo__ScimClientId
    Asgardeo__ScimClientSecret         AgentService__BaseUrl
    AgentService__SharedSecret         Email__ApiKey
    Email__FromAddress                 Cors__AllowedOrigins
    Model__ApiKey  (agent service only)
```

## 14.3 Operational Requirements

| ID | Requirement |
|---|---|
| OPS-01 | The API shall expose `/health` reporting the status of the database, the agent service and the identity provider individually. |
| OPS-02 | The API shall expose `/swagger` with the complete operation set, request and response schemas, and security definitions. |
| OPS-03 | Structured logs shall be emitted with correlation identifiers, and shall never contain tokens, credentials or personal data beyond a subject identifier. |
| OPS-04 | Migrations shall be applied automatically on start-up in the evaluation environment, and the seeder shall be idempotent so that a restart does not duplicate data. |
| OPS-05 | A documented account set covering all four roles shall be available to evaluators, with credentials supplied in the consolidated report rather than in the repository. |
| OPS-06 | All evaluator-facing links shall be verified in a private browsing session before submission. |
| OPS-07 | A rollback path shall exist: the previous container image and the corresponding migration state shall be identified in the deployment report. |
