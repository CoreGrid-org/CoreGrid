# CoreGrid Mobile — Team Allocation

The main SRS already assigns each SE3090 group member end-to-end ownership of one business component —
backend, database, React, Flutter, tests, and one agent (§12, §18 of the
[main SRS](../../srs/00-front-matter.md)). This document is that assignment translated into
**this repository's** `lib/features/` layout (`doc/mobile-specification.md` §3.1), so each member can find
their part here without re-deriving it from the main repo every time. Where the main SRS states Flutter
ownership explicitly (§12's "Flutter" row), this document just restates it against the concrete folder
names. Where it doesn't — the app shell, dashboard — this document proposes an owner and says so plainly, so
the group can ratify or reassign it rather than mistake it for something already decided.

## Roster

| Student | Component (main SRS) | Flutter features owned here | Branch prefix | Requirement range |
|---|---|---|---|---|
| Student 1 — **Jayashan Guruge** | A — Asset Registry & QR Identification | `features/scan/`, `features/assets/` | `feature/asset-*` | FR-016–FR-032 |
| Student 2 — **Seneja Ramanayaka** | B — Maintenance Management | `features/maintenance/`, `features/notifications/` | `feature/maintenance-*` | FR-033–FR-042, FR-077–FR-080 |
| Student 3 — **Nipuna Bhanuka Samarasinghe** | C — Transfer & Disposal | `features/transfers/` | `feature/transfer-*` | FR-043–FR-055 |
| Student 4 — **Hasitha Erandika** (Group Leader) | D — Audit & Compliance, org config, user admin | `features/verification/`, `features/workflows/`, plus the app shell (below) | `feature/audit-*` | FR-056–FR-066, FR-067–FR-069/FR-076 |

Branch prefixes reuse the exact ones already assigned in the main repo (§18.2) — same owner, same naming,
different repository, so there's one mental mapping per person rather than two.

**No one owns a registration screen — there isn't one to build.** Every CoreGrid account is created by an
Administrator through the React console (Component D, `ManageUsers` policy); this app only ever signs
existing users in (`doc/mobile-specification.md` §4.1). Don't read `features/auth/`'s FR-001–FR-009 range
below as including sign-up — it doesn't, on either platform.

For what's actually built vs. outstanding per requirement, `doc/progress.md` is authoritative — this file
divides the work, progress.md tracks its completion; check there before assuming a row below is (or isn't)
done.

### Student 1 — Jayashan Guruge — `features/scan/`, `features/assets/`

The device-feature centrepiece (§8, C-04): QR scan via `mobile_scanner` (FR-024, IF-06/IF-07/IF-10/IF-12),
manual code entry as the always-visible fallback (FR-025), the attribute-driven asset detail screen
(FR-020), and condition update (FR-029, InventoryOfficer/Administrator only server-side via `CanManageAssets`
— see `doc/progress.md` for a currently-open client-side role-gate gap on that button). Every other screen
that needs "resolve an asset from a scan or a code" (transfer receipt confirmation, verification tasks)
calls into `features/scan/`, so this landed first and unblocked the other owners, as intended. **Status
correction:** FR-031's own dedicated ad hoc verification (`AssetVerificationScreen`,
`POST /api/assets/{id}/verify`, this component's named business-specific operation) is built and
widget-tested but not yet wired into a route — asset detail's "Verify" button currently opens Student 4's
task-based FR-059 flow instead (when a pending task exists) rather than this screen. See
`doc/progress.md`'s FR-031 row for the exact gap.

### Student 2 — Seneja Ramanayaka — `features/maintenance/`, `features/notifications/`

Fault reporting with photo capture and client-side compression to ≤1MB (FR-033, IF-11), maintenance task
list and legal-transition-only progress updates (FR-037, FR-042), and the in-app notification list
(FR-080 — in this component's FR-077–080 range, alongside the backend `INotificationService`/email work
this same owner already does in the main repo). Photo upload depends on the object-storage integration in
the main SRS §11.3 (`IBlobStorageService`, Cloudflare R2 / any S3-compatible endpoint) — check that's wired
up backend-side before building the upload UI, not after.

### Student 3 — Nipuna Bhanuka Samarasinghe — `features/transfers/`

Transfer request creation (FR-043) and scan-based receipt confirmation (FR-046, reusing
`features/scan/`). Disposal has no Flutter surface — it's React/Administrator-only per the responsibility
boundary (main SRS §3.4) — so this is the smallest single-feature scope of the four, which is expected: this
owner's larger share of the work is Component C's backend/React transfer-and-disposal engine, not Flutter.

### Student 4 — Hasitha Erandika (Group Leader) — `features/verification/`, `features/workflows/`, app shell

Verification-campaign task list and the field verification flow (FR-058, FR-059), manual discrepancy raising
(FR-061), and agent-status display (FR-067–FR-069, FR-076 — the main SRS's Flutter cell for this component
names "agent status display" explicitly). **Built** — see `doc/progress.md` for the live status and the
exact endpoints each screen calls; both features are Inventory-Officer-only on mobile (Staff has no role in
either — SRS scope change v1.5) and are guarded at the router level, not just hidden from the dashboard.
One backend fix landed alongside them, in this owner's own Component D scope: `DiscrepanciesController`'s
manual-raise action (FR-061) was Auditor/Administrator-only, which would have 403'd every call this app
makes — FR-061 is explicitly an Officer/Flutter requirement (main SRS `06-functional-requirements.md`), so
`InventoryOfficer` was added to that endpoint's allowed roles, and a `POST /api/verification-tasks/photos`
upload endpoint was added (none existed) so the discrepancy photo has somewhere to go before the raise call.
This owner's Flutter scope also includes the pieces §12/§18 don't
assign to any single component, because they're genuinely cross-cutting rather than missing an owner:

| Piece | Why it isn't in anyone's FR range | Recommendation |
|---|---|---|
| `features/auth/` (sign-in, token storage, route guards) | FR-001–FR-009 sits outside every component's stated range — identity is foundational, not owned by one business component | Group Leader, same as this component already owns user administration/SCIM backend-side (§12) |
| `app/` (entry point, `go_router` config, `ProviderScope` root) | Not requirement-numbered at all — it's the scaffold every feature plugs into | Group Leader, consistent with already owning CI and the consolidated submission (§18.2.1) |
| `features/dashboard/` (FR-081–FR-083) | Aggregates every other component's data; the backend `Dashboard/` folder is similarly un-lettered in `CoreGrid/CONTRIBUTING.md` | Group Leader, built last once the other three features exist to summarise |

**This table is a recommendation, not a restatement of the main SRS** — if the group would rather split the
app shell differently (e.g. whoever finishes their own feature first bootstraps it), that's a fine
alternative; just update this file and `doc/progress.md` to match, since progress.md's per-requirement
status is what an evaluator or a teammate resuming later trusts.

## Remaining work

Per `doc/progress.md`'s own "Next milestone" (check there for the live picture — this is just who owns each
outstanding piece, not its status):

- **Student 1 (Jayashan)** — `features/scan/` has landed (camera QR scan, both dashboards route to it). What's
  left in this range: wire `AssetVerificationScreen` into a route so FR-031's ad hoc verification is actually
  reachable (it's built and tested but currently orphaned — see `doc/progress.md`), and close the FR-029
  role-gate gap on the "Update Condition" button (currently shown to Staff, who'll get a 403).
- **Student 2 (Seneja)** — `features/maintenance/` and `features/notifications/` are both still to build;
  the dashboard's "Maintenance Assigned to Me" section stays mock until the former exists to back it.
- **Student 3 (Nipuna Bhanuka Samarasinghe)** — `features/transfers/` is still to build; the dashboard's "Transfers Awaiting My
  Confirmation" section stays mock until it exists.
- **Student 4 (Hasitha, Group Leader)** — Flutter scope (auth, dashboard, verification, workflows) is
  feature-complete against the real backend. The dev ThunderID mobile-client registration
  (`doc/setup/thunderid-mobile-client.md`) is already done and in local use — every owner can already
  exercise their screens against a real sign-in. What remains, infrastructural rather than a feature: the
  `staging`/`prod` client registrations, owned here for the same identity/org-config reason, needed before a
  release build rather than before local development.

## Build order

`features/auth/` and `app/` are the one piece every other feature depends on — nothing else can be
demonstrated without sign-in and a router to reach it — so they should land in week one, mirroring risk
R-01 in the main SRS (§15): a thin vertical slice, integrated early, never allowed to break. `features/scan/`
is next, since three other owners' features (`verification`, `transfers`, and indirectly `maintenance`'s
task list) call into it. `features/dashboard/` is deliberately last — it has nothing to summarise until the
other three exist.

## Testing and evidence

Each owner's contribution evidence requirements are identical to the main repo's (§12.1): a feature branch
per capability, a pull request reviewed by at least one other member, requirement IDs referenced in the
implementing issue/PR, and — per `CLAUDE.md` here — `flutter analyze` and `flutter test` passing before any
of that. The main SRS's rule that "work an owner cannot explain is treated as not delivered" applies here
exactly as it does to the backend and React work.


