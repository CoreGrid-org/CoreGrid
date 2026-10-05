# ThunderID Reference Configuration

`coregrid.yaml` is an export of a working local ThunderID instance configured for CoreGrid by following
[`docs/setup/thunderid.md`](../../docs/setup/thunderid.md). It is the canonical record of that configuration:
- the `CoreGridUser` type;
- the `Administrator`, `InventoryOfficer`, `Auditor` and `Staff` roles, plus the built-in `Admin` role granted to the backend app;
- the CoreGrid Frontend, Backend and Mobile applications;
- their sign-in and recovery flows;
- CORS and CSP settings;
- ThunderID's own defaults (console, themes, translations).

Use it to check a console setup field by field, or to see the exact IDs and token attributes CoreGrid expects.

| File | Purpose |
|---|---|
| `coregrid.yaml` | ThunderID declarative-resource export. Personal user accounts were removed before committing; only ThunderID's default `admin@example.com` console user remains |
| `render/` | Image and start script for running ThunderID as a Render web service (PostgreSQL storage, keys from Secret Files) — see [`docs/setup/deployment.md`](../../docs/setup/deployment.md) |
| `environment.env.example` | Values for the `{{.CONSOLE_*}}` and `{{.CORE_GRID_*}}` placeholders (client IDs, redirect URIs). The backend client secret is left blank |

## Not loaded automatically

ThunderID can load `*.yaml` files from `config/resources/` at start-up. This file is **not** mounted into
the local container, because the quick-start's one-shot setup already writes the same default resources
(organisation unit, console, flows, themes) into ThunderID's database. Loading the export on top of an
initialised instance fails with duplicate-ID errors.

Configure ThunderID through the console using `docs/setup/thunderid.md`, and use this file as the reference.

## Keeping it current

After changing the ThunderID configuration, export it again and update this file in the same pull request:
- **remove every `resource_type: user` document** except the default `admin@example.com`;
- **remove their role `assignments`**;
- keep secrets as `{{.CORE_GRID_BACKEND_CLIENT_SECRET}}` placeholders.
