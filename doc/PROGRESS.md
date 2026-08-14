# Progress Tracker

A living checklist against the ownership and evidence requirements in [SRS §12](SRS/12-individual-contribution-and-work-allocation.md) and [SRS §18](SRS/18-team-roster-and-work-allocation.md). Tick an item only once it's actually in the repo — this file reflects what exists, not what's planned; [SRS §18.9](SRS/18-team-roster-and-work-allocation.md#189-suggested-delivery-rhythm) is the plan. Update it in the same PR that lands the work it describes.

Status as of 2026-08-12: cross-cutting identity/admin slice is up; none of the four business components (Asset Registry, Maintenance, Transfer & Disposal, Audit & Compliance) or the agentic subsystem have started; no mobile app, no tests, no CI yet.

## Cross-cutting (Identity, Access, Admin Shell)

| Item | Status |
|---|---|
| ThunderID OIDC sign-in (PKCE) — FR-001 | ✅ |
| Backend JWT validation (issuer + RS256 via JWKS) — FR-002 | ✅ |
| `GET /api/me` — resolve the caller's own CoreGrid profile/role by `sub` (SRS §16.1's named surface for this range) | ✅ (`backend/Features/Me`) |
| Resolve `OrganizationId` from local user mirror by `sub` — FR-003 | 🟡 (`/api/me` does the by-`sub` lookup; `OrganizationId` itself isn't surfaced or used for scoping yet — M0's single-org shortcut is used elsewhere instead) |
| Create/refresh local user mirror on first request; audit role changes — FR-004 | ❌ |
| Every endpoint declares an authorisation policy — FR-005 | 🟡 (`UsersController` does; `SetupController` is deliberately open — no other endpoints exist yet) |
| Global `OrganizationId` query filter — FR-006 | ❌ |
| Frontend hides/protects unpermitted routes — FR-007 | ✅ (`RoleRoute`); action-level hiding N/A until there are fine-grained in-page actions |
| Sign-out clears state, revokes refresh token, ends IdP session — FR-008 | 🟡 (delegated to `@thunderid/react`'s `SignOutButton`, not independently verified end to end) |
| Deactivated user denied even with a valid token — FR-009 | ❌ (`User.IsActive` exists on the entity; nothing reads it yet) |
| First-Administrator provisioning via Setup (creates ThunderID account + CoreGrid role) | ✅ |
| Admin invites a user by email + role, provisioned through ThunderID — FR-013 | ✅ (`POST /api/users`, `GET /api/users`) |
| Change a user's role/department, deactivate a user — FR-014 | ❌ |
| EF Core migrations + generated `db/schema.sql` export (SRS §2.3 C-02) | ✅ |
| CI pipeline (build/test/lint on push and PR) — §13.6 | ❌ |
| Any backend or frontend test project | ❌ |
| Flutter mobile app | ❌ (not started — no `mobile/`/`flutter/` directory exists yet) |

## Component A — Asset Registry & QR Identification (Jayashan Guruge, FR-016–032)

| Area | Status |
|---|---|
| Backend (asset/type/category/attribute/location controllers, QR generation, verify operation) | ❌ |
| Database (`AssetCategories`, `AssetTypes`, `AssetAttributeDefinitions`, `AssetAttributeValues`, `Assets`, `AssetHistory`) | ❌ |
| React (asset list/detail/create/edit, dynamic attribute forms, category/type config) | ❌ |
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
| Database (`AssetTransfers`, `DisposalRequests`) | ✅ |
| React (transfer/disposal queues, precondition checklist) | ❌ |
| Flutter (transfer request, scan-to-confirm receipt, condemnation) | ❌ |
| Budget Analysis Agent | ❌ |
| Tests | ❌ |

## Component D — Audit & Compliance + Org Configuration + User Administration (Hasitha Erandika, FR-010–015, FR-056–066)

| Area | Status |
|---|---|
| Organisation creation (Setup) | ✅ |
| Department/Location CRUD — FR-010, FR-011, FR-012 | ❌ |
| User administration (invite by role) — FR-013 | ✅ |
| User role/department change, deactivation — FR-014 | ❌ |
| Organisation policy parameters — FR-015 | ❌ |
| Verification campaigns, task generation — FR-056, FR-057 | ❌ |
| Automatic + manual discrepancy raising — FR-060, FR-061 | ❌ |
| Discrepancy resolution operation — FR-062 | ❌ |
| Append-only audit log — FR-063, FR-064 | ❌ |
| Campaign report + PDF/CSV export — FR-065, FR-084, FR-085 | ❌ |
| Dashboard indicators + visualisations — FR-081, FR-082, FR-086 | 🟡 (React Admin Dashboard built with mock data only — no backend metrics yet) |
| React (admin screens for departments/locations/users/policy, audit dashboard, campaigns, discrepancy resolution) | 🟡 (Users & Roles is real; everything else is a mock/placeholder page) |
| Flutter (verification task list, field verification flow) | ❌ |
| Policy Compliance Agent + human-approval checkpoint | ❌ |
| Tests (append-only, discrepancy resolution, authorisation matrix) | ❌ |
| CI workflow ownership — §13.6 | ❌ |

## Delivery Rhythm Checkpoints ([SRS §18.9](SRS/18-team-roster-and-work-allocation.md#189-suggested-delivery-rhythm))

| Week | Target | Status |
|---|---|---|
| 1 | Vertical slice per owner: one entity, one CRUD endpoint, one React screen, one Flutter screen, real ThunderID auth, deployed. Agent contracts frozen. | 🟡 Component D's slice only (identity + admin); A/B/C not started; Flutter not started for any owner; agent contracts not yet frozen |
| 2 | Full CRUD + search/filter/sort/pagination per component | ❌ |
| 3 | Owned state machine with guarded transitions + negative tests | ❌ |
| 4 | Business-specific operation end to end with transaction + audit trail | ❌ |
| 5 | Own agent built against a stubbed model call, wired into the graph | ❌ |
| 6 | Real model call; full four-agent graph run together; golden cases passing | ❌ |
| 7 | Tests complete, CI green, authorisation matrix run | ❌ |
| 8 | Stabilisation: regression testing, AI usage logs, ADR set, docs, viva prep | ❌ |

**Legend:** ✅ done and in the repo · 🟡 partially done / mocked · ❌ not started
