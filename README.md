<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="frontend/public/assets/w-coregrid.webp">
    <img src="frontend/public/CoreGrid.png" alt="CoreGrid" width="140">
  </picture>
</p>

<h1 align="center">CoreGrid</h1>
<p align="center">A configurable, agentic-AI-assisted asset lifecycle management platform for government &amp; institutional assets.</p>

CoreGrid registers, identifies, maintains, transfers and disposes of an organisation's physical assets under one role-controlled platform, backed by a four-agent, human-approved agentic AI workflow for repair, transfer and disposal decisions. Full detail is in the [Software Requirements Specification](docs/srs/00-front-matter.md).

## Repository layout

| Path | Contents |
|---|---|
| `frontend/` | Marketing/landing site (React + Vite) - see [`frontend/README.md`](frontend/README.md) |
| `docs/srs/` | Baselined Software Requirements Specification |
| `docs/mobile/` | Flutter mobile client specification, progress, ownership, setup, and feature documentation |
| `LICENSE` | Apache License 2.0 |

## Component A — Asset Registry and QR Identification

Component A provides the asset register used by the rest of CoreGrid. Inventory Officers can configure asset categories and types, define ordered custom attributes for each type, and register assets with an organisation-scoped unique code, department, location, acquisition details, and validated attribute values. Asset records support search and filtering, condition updates, depreciation information, and lifecycle history.

### Scan, lookup, and verification

Each asset’s QR payload identifies its asset code. The Flutter [mobile application](https://github.com/CoreGrid-org/coregrid-mobile) scans the label and retrieves the authoritative record through `GET /api/assets/qr/{code}`. Manual code entry provides a fallback lookup. The API scopes lookup to the authenticated organisation.

For physical verification, field users confirm the asset’s presence, actual location, and condition in the mobile workflow. CoreGrid compares those observations with the register and records a discrepancy when they differ. Verification can be completed from a campaign task or through the ad-hoc verification flow. The React application supports asset registration, configuration, search, and record management; mobile screens support field lookup and verification.

### Design decisions

#### Custom attribute storage — ADR-006

Each asset type has configurable attribute definitions in `AssetAttributeDefinitions`. An asset’s values are stored separately in `AssetAttributeValues`, linked to both the asset and its definition. Values use typed fields, and the application validates each value against its definition before saving it.

This relational design was chosen because asset attributes must remain connected to valid definitions and support type-aware validation and search. It supports FR-019 validation and FR-028 filtering without adding new database columns for every asset type. Foreign keys preserve those relationships, while indexes support common searches. Displaying an asset’s attributes requires joining its value rows to their definitions; variable agent workflow records use JSONB, where flexible record shapes are more appropriate. See [ADR-006](docs/architecture/decision-records.md#adr-006--relational-attribute-value-storage-and-jsonb-workflow-state) for the options and rationale.

#### Asset identity and lifecycle

Asset codes are unique within an organisation. Asset amendments and lifecycle events are recorded in asset history; assets with lifecycle history are retained and leave the active register through the disposal workflow.

### Component A references

- [Component A design and progress note](docs/progress.md#component-a--asset-registry--qr-identification-jayashan-guruge)
- [Functional requirements FR-021–FR-032](docs/srs/06-functional-requirements.md#64-component-a--asset-registry-and-qr-identification-fr-021--fr-032)
- [Custom attribute storage rationale](docs/srs/08-data-requirements.md#84-custom-attribute-storage-strategy)
- [Physical schema for Component A](docs/srs/appendix-f-physical-database-schema.md#f7-component-a--asset-registry--qr-identification)

## License

Apache License 2.0 - see [LICENSE](LICENSE) and [NOTICE](NOTICE).
