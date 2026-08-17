# Progress Tracker

A living checklist against the ownership and evidence requirements in [SRS §12](SRS/12-individual-contribution-and-work-allocation.md) and [SRS §18](SRS/18-team-roster-and-work-allocation.md). Tick an item only once it's actually in the repo — this file reflects what exists, not what's planned; [SRS §18.9](SRS/18-team-roster-and-work-allocation.md#189-suggested-delivery-rhythm) is the plan. Update it in the same PR that lands the work it describes.

Status as of 2026-08-15: cross-cutting identity/admin slice is up; Component A (Asset Registry) has a working backend + React slice; Component C (Transfer & Disposal) has its database schema only; Component D (Audit & Compliance + Org Config + User Admin) now has a real backend for org structure, user admin, org policy, the audit log, and verification campaigns/discrepancies, and its Settings/Users/Audit-log/Dashboard React screens are now wired to that backend (real data, no more mocks on those). Verification campaigns/discrepancies still have no React screens at all. A shared admin shell polish pass (header branding, sidebar) also landed today. Maintenance and the agentic subsystem have not started; no mobile app, no tests, no CI yet.

## Cross-cutting (Identity, Access, Admin Shell)

| Item | Status |
|---|---|
| ThunderID OIDC sign-in (PKCE) — FR-001 | ✅ |
| Backend JWT validation (issuer + RS256 via JWKS) — FR-002 | ✅ |
| `GET /api/me` — resolve the caller's own CoreGrid profile/role by `sub` (SRS §16.1's named surface for this range) | ✅ (`backend/Features/Me`) |
| Resolve `OrganizationId` from local user mirror by `sub` — FR-003 | 🟡 (`/api/me` does the by-`sub` lookup; `OrganizationId` itself isn't surfaced or used for scoping yet — M0's single-org shortcut is used elsewhere instead) |
| Create/refresh local user mirror on first request; audit role changes — FR-004 | ❌ |
| Every endpoint declares an authorisation policy — FR-005 | 🟡 (mixed: `UsersController`/`OrganizationPoliciesController`/`AuditLogController` and a few Component D write actions declare specific roles; `SetupController` is deliberately open; most Assets/OrgConfig/Verification GET endpoints still use a blanket `[Authorize]` with no role policy) |
| Global `OrganizationId` query filter — FR-006 | ❌ |
| Frontend hides/protects unpermitted routes — FR-007 | ✅ (`RoleRoute`); action-level hiding N/A until there are fine-grained in-page actions |
| Sign-out clears state, revokes refresh token, ends IdP session — FR-008 | 🟡 (delegated to `@thunderid/react`'s `SignOutButton`, not independently verified end to end) |
| Deactivated user denied even with a valid token — FR-009 | ✅ (`RoleEnrichmentMiddleware` rejects with 401 if `Users.IsActive` is false) |
| First-Administrator provisioning via Setup (creates ThunderID account + CoreGrid role) | ✅ |
| Admin invites a user by email + role, provisioned through ThunderID — FR-013 | ✅ (`POST /api/users`, `GET /api/users`) |
| Change a user's role/department, deactivate a user — FR-014 | ✅ (`PATCH /api/users/{id}`, `/deactivate`, `/activate`; guards against deactivating the org's last active Administrator) |
| EF Core migrations + generated `db/schema.sql` export (SRS §2.3 C-02) | ✅ (2026-08-15: `db/schema.sql` and `db/migrations/*.sql` had drifted behind two real migrations — regenerated via `dotnet ef migrations script`) |
| CI pipeline (build/test/lint on push and PR) — §13.6 | ❌ |
| Any backend or frontend test project | ❌ |
| Flutter mobile app | ❌ (not started — no `mobile/`/`flutter/` directory exists yet) |

## Component A — Asset Registry & QR Identification (Jayashan Guruge, FR-016–032)

| Item | Status |
|---|---|
| Create asset categories — FR-016 | ✅ (`POST /api/asset-categories`; `PUT .../{id}` update, and `DELETE .../{id}` + `PATCH .../{id}/activate` added 2026-08-17 — delete hard-removes an unreferenced category, otherwise deactivates it (`IsActive`) so it stops appearing as a choice for new types while existing types/assets keep working; reactivatable) |
| Create asset types (name, code, category, useful life, default maintenance interval) — FR-017 | ✅ (`POST /api/asset-types`; `PUT .../{id}` update, and `DELETE .../{id}` + `PATCH .../{id}/activate` added 2026-08-17 — same hard-delete-if-unreferenced/deactivate-otherwise rule, keyed off whether any `Asset` references the type; deleting an unreferenced type also cascades its own attribute definitions) |
| Ordered custom attribute definitions per asset type — FR-018 | ✅ (`POST /api/asset-types/{id}/attributes`; `PUT .../attributes/{attributeId}` update, and `DELETE .../attributes/{attributeId}` + `PATCH .../attributes/{attributeId}/activate` added 2026-08-17 — same rule, keyed off whether any `AssetAttributeValue` references the definition) |
| Attribute value validation on create and update — FR-019 | 🟡 (required-field and data-type checks are enforced on both create and update; `ValidationRule` is stored and returned to clients but no code path evaluates a submitted value against it — rule enforcement itself isn't implemented) |
| Dynamic attribute-driven detail form, both clients — FR-020 | 🟡 React ✅ (`AssetRegisterPage` renders fields purely from the selected type's attribute definitions, no hardcoded domain knowledge); Flutter ❌ (not started) |
| Register an asset (type, name, department, location, acquisition date/cost, attributes) — FR-021 | ✅ |
| Unique human-readable asset code (org prefix + monotonic sequence, DB-constrained) — FR-022 | ✅ (`AssetCodeGenerator`; `IX_Assets_OrganizationId_AssetCode` unique index) |
| QR label payload + printable label download — FR-023 | 🟡 (a real QR image is generated and rendered in the asset detail modal via the `qrcode` package — not just the raw payload string; no printable-label download/print feature exists yet) |
| Mobile QR scan → authoritative record within 3s — FR-024 | ❌ (Flutter not started) |
| Manual asset-code entry as an alternative to scanning — FR-025 | 🟡 React ✅ (`AssetScanPage` — manual code/QR-payload entry only, resolves via `GET /api/assets/qr/{code}` to the identical detail view a scan would produce); Flutter ❌ |
| Amend asset fields/attributes/department/location — FR-026 | 🟡 (`PUT /api/assets/{id}` works; amendments are not written to `AssetHistory`, so the "every change recorded in history" half of this requirement is unmet) |
| Immutable, ordered per-asset lifecycle history — FR-027 | ❌ (`AssetHistory` table/entity exists but the only writer anywhere in the backend is discrepancy resolution in Component D's Verification feature; asset create, amend, and condition-change don't write to it) |
| Search by code/name/attribute value; filter by department/location/category/type/status/condition; server-side sort + pagination — FR-028 | ✅ (2026-08-17: search now also matches asset type name/code, asset category name/code, and dynamic attribute values — text via `ILike`, number/date via typed equality after parsing the search term; added a `categoryId` filter alongside the existing type/department/location/status/condition ones; sorting and pagination remain fully server-side) |
| Record condition (New/Good/Fair/Poor/Unserviceable) — FR-029 | 🟡 (`PATCH /api/assets/{id}/condition` works; the change is not written to `AssetHistory`) |
| Computed residual value (straight-line depreciation) — FR-030 | ❌ (`ResidualValue` is taken as-is from whatever the client submits on create/update — a free-entry field in the React form — never derived server-side from acquisition cost, acquisition date, and the asset type's useful life) |
| Officer physical verification (presence/location/condition assertion, reconciled against the register) — FR-031 | ❌ (the named surface `POST /api/assets/{id}/verify` doesn't exist; this FR is Flutter-only per the SRS and Flutter hasn't started) |
| Prevent deletion of assets with history; disposal is the only exit from the register — FR-032 | 🟡 (no `DELETE` endpoint exists on `AssetsController` at all, so nothing can be deleted — satisfies the letter of it; but the disposal workflow itself is Component C, not yet built, so in practice assets have no path off the active register yet either) |
| Database (`AssetCategories`, `AssetTypes`, `AssetAttributeDefinitions`, `AssetAttributeValues`, `Assets`, `AssetHistory`) | ✅ (department/location CRUD moved to `backend/Features/OrgConfig` — Component D, 2026-08-15 backend file structure cleanup; migration `AddIsActiveToAssetCategoryTypeAttribute` added an `IsActive` column to `AssetCategories`/`AssetTypes`/`AssetAttributeDefinitions` 2026-08-17 for the hard-delete-vs-deactivate rule — `Assets` itself deliberately untouched) |
| React (asset list/detail/register/update, dynamic attribute forms, category/type/attribute config incl. edit/delete/reactivate, searchable pickers, real organisation-code display in the code preview) | ✅ (`frontend/src/features/assets`) |
| Flutter (QR scanner, asset lookup, condition update) | ❌ |
| Planner Agent | ❌ |
| Tests | ❌ |

## Component B — Maintenance Management (Seneja Ramanayaka, FR-033–042, FR-077–080)

| Area | Status |
|---|---|
| Backend (maintenance state machine, completion op, notification service) | ❌ |
| Database (`MaintenanceRecords`, `MaintenanceAttachments`, `Notifications`) | ❌ |
| React (maintenance list/detail/assign/complete, notification centre) | ❌ |
| Flutter (fault report with photo, task list) | ❌ |
| Maintenance Analysis Agent | ❌ |
| Tests | ❌ |

## Component C — Transfer & Disposal (Bhanuka Samarasinghe, FR-043–055)

| Area | Status |
|---|---|
| Backend (transfer/disposal controllers, approval preconditions P1–P6) | ❌ |
| Database (`AssetTransfers`, `DisposalRequests`) | ✅ (2026-08-15: fixed — `AssetId`/department/location columns had been left as bare `Guid`s with `TODO: add FK` comments even after `Asset`/`Department`/`Location` existed; added the real FK constraints + migration `AddTransferDisposalForeignKeys`, and repaired an out-of-sync `CoreGridDbContextModelSnapshot.cs` — the two entities' `DbSet` properties and model-snapshot blocks had been dropped, silently breaking `dotnet ef migrations add` for anyone touching this schema) |
| React (transfer/disposal queues, precondition checklist) | ❌ |
| Flutter (transfer request, scan-to-confirm receipt, condemnation) | ❌ |
| Budget Analysis Agent | ❌ |
| Tests | ❌ |

## Component D — Audit & Compliance + Org Configuration + User Administration (Hasitha Erandika, FR-010–015, FR-056–066)

| Area | Status |
|---|---|
| Organisation creation (Setup) | ✅ |
| Department/Location CRUD — FR-010, FR-011, FR-012 | ✅ (amend + activate/deactivate added 2026-08-15; FR-012's guard refuses deactivation while a non-`DISPOSED` asset references the department/location) |
| User administration (invite by role) — FR-013 | ✅ |
| User role/department change, deactivation — FR-014 | ✅ |
| Organisation policy parameters — FR-015 | ✅ (`OrganizationPoliciesController`; enforces at most one policy per asset type, including the org-wide default) |
| Verification campaigns, task generation — FR-056, FR-057 | ✅ (`VerificationCampaignsController`; task generation + officer assignment is synchronous on creation — assignment is "first active InventoryOfficer in the asset's Department," since the schema has no location-ownership concept — tasks go unassigned, not dropped, when none exists) |
| Officer scan-to-verify — FR-059 | ✅ (`PATCH /api/verification-tasks/{id}/complete`) |
| Automatic + manual discrepancy raising — FR-060, FR-061 | ✅ (auto: Missing/LocationMismatch/ConditionMismatch only — Surplus/DataMismatch aren't derivable from a single-asset task and stay manual-only; FR-061's "photograph" is a URL field — no file-upload infrastructure exists yet) |
| Discrepancy resolution operation — FR-062 | ✅ (`PATCH /api/discrepancies/{id}/resolve`; register correction + `AssetHistory` write supported for ConditionMismatch/LocationMismatch only — the only two types with one unambiguous register field to correct) |
| Append-only audit log — FR-063, FR-064 | ✅ (generic EF `SaveChanges` interceptor — `AuditSaveChangesInterceptor` — covers every entity in `CoreGridDbContext` automatically, including future ones; DB-level `REVOKE UPDATE, DELETE` matches the `AssetHistory` precedent) |
| Campaign report + PDF/CSV export — FR-065, FR-084, FR-085 | ❌ |
| Dashboard indicators + visualisations — FR-081, FR-082, FR-086 | 🟡 (`GET /api/dashboard/summary` now returns real counts and the React Admin Dashboard's stat tiles are wired to it (2026-08-15); FR-082's charts (assets by department/condition, maintenance cost by month) are still mock — no chart-data endpoint exists yet; the "restricted to permitted departments" half of FR-086 is still open) |
| React (admin screens for departments/locations/users/policy, audit dashboard, campaigns, discrepancy resolution) | 🟡 (Users & Roles, Settings — departments/locations/policy —, and the Audit Log tab are now real, wired to the new backend with add/edit/activate/deactivate actions (2026-08-15); Audit page's Campaigns/Discrepancies tabs and any campaign/discrepancy-resolution UI don't exist yet — the backend has no frontend for those at all; admin shell (header/sidebar) got a branding + polish pass) |
| Flutter (verification task list, field verification flow) | ❌ |
| Policy Compliance Agent + human-approval checkpoint | ❌ |
| Tests (append-only, discrepancy resolution, authorisation matrix) | ❌ |
| CI workflow ownership — §13.6 | ❌ |

## Delivery Rhythm Checkpoints ([SRS §18.9](SRS/18-team-roster-and-work-allocation.md#189-suggested-delivery-rhythm))

| Week | Target | Status |
|---|---|---|
| 1 | Vertical slice per owner: one entity, one CRUD endpoint, one React screen, one Flutter screen, real ThunderID auth, deployed. Agent contracts frozen. | 🟡 Component D (identity + admin) and Component A (asset registry, backend + React) have their slice; Component C has schema only; B not started; Flutter not started for any owner; agent contracts not yet frozen |
| 2 | Full CRUD + search/filter/sort/pagination per component | 🟡 Component D has full CRUD for org structure/users/policy (no search/filter/sort/pagination — lists are small); Component A has full CRUD + pagination for assets; B/C not started |
| 3 | Owned state machine with guarded transitions + negative tests | ❌ |
| 4 | Business-specific operation end to end with transaction + audit trail | ❌ |
| 5 | Own agent built against a stubbed model call, wired into the graph | ❌ |
| 6 | Real model call; full four-agent graph run together; golden cases passing | ❌ |
| 7 | Tests complete, CI green, authorisation matrix run | ❌ |
| 8 | Stabilisation: regression testing, AI usage logs, ADR set, docs, viva prep | ❌ |

**Legend:** ✅ done and in the repo · 🟡 partially done / mocked · ❌ not started
