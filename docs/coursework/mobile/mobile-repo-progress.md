# CoreGrid Mobile — Progress

Tracks what's actually built in **this repository** against the Flutter-tagged requirements in the main
[`CoreGrid` SRS](../../srs/00-front-matter.md). Treat this file as more current than
assumptions about the codebase. For the platform-wide picture (backend + React status), see
[`CoreGrid/doc/progress.md`](../progress.md); for how each requirement below maps to a
module, see [`mobile-specification.md` §8](https://github.com/CoreGrid-org/coregrid-mobile/blob/development/doc/mobile-specification.md#8-traceability); for who owns it, see
[`team-allocation.md`](mobile-repo-team-allocation.md).

Status as of 2026-09-27 (see the dated entries below for what changed since 2026-09-25): `features/auth/`, `features/dashboard/`, `features/verification/` and
`features/workflows/` (Student 4/Hasitha's full scope per `team-allocation.md`) landed first: ThunderID PKCE
sign-in via `flutter_appauth`, the SRS §2.3.1/v1.5 role gate (Auditor/Administrator routed to
`/access-restricted`), sign-out revoking the refresh token against ThunderID's `oauth2/revoke` before
clearing local state (FR-008), and a role-branched dashboard — no dev/bypass sign-in path, every sign-in
goes through the real PKCE flow. `features/verification/` (task list, task-completion flow, manual
discrepancy raising with a compressed photo) and `features/workflows/` (initiate, poll status, show outcome)
are both live against the real backend — no mock data — and both Inventory-Officer-only, guarded at the
router level. Student 1 (Jayashan)'s `features/scan/` and the bulk of `features/assets/` have since landed
too: camera QR scan with torch/permission-refusal/unknown-code/offline handling, manual entry, attribute-
driven asset detail, condition update, and asset search all resolve against the real backend. One real gap
found while auditing this update, not yet fixed: FR-031's dedicated ad-hoc verification screen
(`AssetVerificationScreen`, `POST /api/assets/{id}/verify`) is built and has its own widget test, but is
**not wired into any route** — nothing in `lib/` navigates to it — so it's currently unreachable from the
running app; see FR-031's row below. (Both since closed — see the 2026-09-27 entries.) As of 2026-09-27
`features/maintenance/` and `features/transfers/` are built too; only `features/notifications/` (FR-080) is
still empty.

**2026-09-27 — app-wide redesign and navigation.** The app now runs in a role-filtered bottom-navigation
shell (Home · Verify · Workflows · Faults · Account for Officers; Home · Faults · Account for Staff — see
`mobile-specification.md` §3.3), with a new Account tab (`GET /api/me` profile, confirmed sign-out) and a
Faults tab listing the user's own fault reports. Every screen, including `features/scan/`,
`features/assets/` and `features/maintenance/`, was restyled onto one shared UI kit
(`shared/widgets/ui.dart`) and theme — no screen hard-codes its own palette any more. Behaviour, API
calls and on-screen strings the tests depend on were kept; the only deliberate test-visible changes are
sentence-case titles ("Fault report"), `d MMM y` dates, and relative due dates ("Overdue by 3 days").

**2026-09-27 — verification procedure, department context, refactor.** Staff take no part in verification
(FR-031/058/059/061 are Officer-only; SRS §3.4.1) — the procedure is Officer: scan the label → the matching
campaign task (or ad-hoc) → assert → submit. Account and Home now show the user's department and its
locations (`GET /api/me` `department_id` + `/api/departments`, `/api/locations?departmentId=`); Staff search
filters drop the Department filter and list only their department's locations. Data scoping itself stays
server-side (`DepartmentScope`: Staff → own department, others org-wide) — the app never filters API
results. Also: org-config filter lists now request `pageSize=100` (they silently stopped at 20); dead
`getOpenDiscrepancies` (Auditor/Admin-only endpoint, would 403 for an Officer) removed; photo compression
and several widgets de-duplicated into `shared/`; Faults polling pauses when the tab is hidden.

**2026-09-27 — transfers merge repaired; maintenance FR-033/037/042 completed.** Pulling PR #8
(`feature/transfer-request-and-receipt`) left `development` **not compiling** (17 analyzer errors) and with
several transfer-flow bugs that would have failed at runtime even once it built. It looks like the files
went through a Windows shell that stripped every `$…` interpolation and re-encoded the files (UTF-8 BOMs,
`Â§`/`â€”` mojibake in `router.dart`). Fixed:

- **Every transfer detail / confirm call hit the wrong URL** — `'/api/transfers/$transferId'` had become
  `'/api/transfers/'` and `'/api/transfers//confirm-receipt'`, so detail loaded the *list* endpoint and
  receipt confirmation could never succeed. After creating a transfer the form navigated to `/transfers/`
  instead of the new transfer.
- **Query parameters the API silently ignored** — `page_size`, `department_id`, `include_inactive`.
  ASP.NET binds `[FromQuery]` by property name (the snake_case policy is JSON-body only), so the
  destination-location picker listed *every* location in the org, not the chosen department's. Now
  camelCase, capped at the API's `pageSize` 100.
- **Receipt confirmation could skip the identity check** — the mismatch check was skipped whenever the
  transfer detail wasn't already cached, confirming receipt for whatever was scanned. The duplicated
  scanner (`ConfirmReceiptScanScreen`, hard-coded colours, no IF-10 manual-entry fallback) was replaced
  by the shared `identifyAssetByScan` flow on the transfer detail screen: scan (or type the code) → must
  equal the transfer's asset → `POST …/confirm-receipt`.
- **"Awaiting my confirmation" listed every approved transfer in the org** — now `status=APPROVED&
  departmentId=<mine>` narrowed to transfers *into* the officer's department (the only ones the API lets
  them confirm); an officer elsewhere sees who must confirm instead of a button that would 403.
- **Router / dashboard** — `/transfers` wasn't actually in the Officer-only guard (the comment said it
  was); a duplicate top-level `/workflows` route shadowed the Workflows tab; the dashboard merge dropped
  the "Recent evaluations" and "My fault reports" previews and called widgets that no longer exist. The
  officer dashboard is restored with two new live sections, **Maintenance assigned to me** and
  **Transfers to receive**, and a **Transfers** quick action.
- **FR-043 asset entry** was a raw UUID text box; it's now asset code lookup or scan (or pre-filled from
  asset detail's new **Request Transfer** action, Officer + ACTIVE assets only), with IF-03 feedback when
  the asset isn't ACTIVE. All transfer screens moved onto the shared UI kit.

Maintenance (Student 2's scope — built this session at the group's request; review before merging):

- **FR-033** — fault reporting already existed but **photo upload was broken**: each photo was sent twice
  (`photo` and `file` parts) as `application/octet-stream`, which `PhotoUploadValidator` rejects (JPEG/PNG/
  WebP only). Now one `photo` part labelled `image/jpeg`; `pickCompressedPhoto` also names the file `.jpg`
  so HEIC/PNG sources don't mislabel the re-encoded JPEG (this fixes FR-061 discrepancy photos too). The
  description enforces the API's 2000-char limit client-side (IF-03).
- **Status bug** — the API's `IN_PROGRESS` wasn't recognised as active (only `inprogress`/`in progress`
  were), so in-progress reports showed under *Closed* and the tracker jumped to *Resolved*; `CANCELLED`
  also showed as *Resolved*. Fixed, and the tracker is now the SRS's four-step Requested → Approved → In
  progress → Completed, with cancelled records shown as such (with the reason).
- **FR-042** — Officers' Faults tab is now **Maintenance** with *My reports* / *All records*. *All records*
  is `GET /api/maintenance` with every FR-042 filter (status, priority, type, department, asset by code →
  id, assignee = "Assigned to me", reported date range), sort (newest / oldest / priority / status) and
  "Load more" pagination, all server-side. `/faults?view=assigned` opens it pre-filtered.
- **FR-037** — the record detail (`/maintenance/:id`) re-reads `GET /api/maintenance/{id}` and offers the
  assigned officer the one transition SRS §3.4 gives Flutter ("Progress update only"): **Start work**,
  APPROVED → IN_PROGRESS via `POST /api/maintenance/{id}/start`, behind a confirm. Approval/assignment/
  costing and completion stay on the web console per that table; the screen says what each record is
  waiting for instead. `/maintenance/:id` also no longer crashes when opened without a list copy.

`flutter analyze`: 0 issues. `flutter test`: 61 passing (new: `test/features/transfers/`,
`test/features/maintenance/maintenance_records_test.dart`, FR-037 cases in `fault_detail_screen_test.dart`).
Not yet exercised on a device against the live backend.

**2026-09-27 — FR-080 notifications and password reset.** Built on the backend's existing
`/api/notifications` inbox (see the FR-080 row). **Password reset** reuses the backend's ThunderID recovery
setup (`CoreGrid/docs/setup/thunderid.md` step 8): *Forgot password?* on the sign-in screen and *Change
password* on Account open ThunderID's hosted `/gate/recovery?applicationId=…` page in the external browser
(`url_launcher`, never a WebView — SEC-ID-06); CoreGrid never handles the password, and an existing session
stays signed in. It needs a new `THUNDERID_APPLICATION_ID` define and recovery enabled on the **mobile**
ThunderID application — see `doc/setup/thunderid-mobile-client.md` → *Password recovery*. Until both are
done, the buttons explain the alternatives (ThunderID's own sign-in page, or an Administrator reset) instead
of failing. `flutter analyze`: 0 issues; `flutter test`: 68 passing. Not yet tried on a device.

## Legend

✅ Done &nbsp;·&nbsp; 🟡 Partial &nbsp;·&nbsp; ❌ Not started

## By requirement

| Requirement | Owner | Status |
|---|---|---|
| FR-001/007/008 — Sign in/out via ThunderID PKCE, role-aware nav, sign-out clears session | Student 4 (Hasitha) | ✅ (sign-in, role gate and route guard work; sign-out now revokes the stored refresh token via ThunderID's `oauth2/revoke` — RFC 7009 — before clearing local state, best-effort so an offline sign-out still succeeds locally) |
| FR-020 — Attribute-driven asset detail rendering | Student 1 (Jayashan) | ✅ (`features/assets/` — `AssetDetailScreen` renders custom attributes from `data_type` alone, no domain-specific code; see [`doc/features/asset-detail.md`](features/asset-detail.md). Widget-tested; reached routinely from both scan and manual lookup against the real backend, so the earlier "blocked on ThunderID native client" caveat on this row no longer holds — a dev ThunderID mobile client is configured (`.env.json`) and in active use) |
| FR-024 — QR scan → authoritative asset record within 3s | Student 1 (Jayashan) | ✅ (`features/scan/` opens the device camera from both dashboards, accepts QR codes, resolves `GET /api/assets/qr/{code}`, and immediately opens the returned authoritative record; torch, permission refusal, unknown-code, and offline recovery included. No widget test yet for `ScanAssetScreen` itself — worth adding before calling this fully evidenced per `team-allocation.md`'s testing rule) |
| FR-025 — Manual asset-code entry fallback | Student 1 (Jayashan) | ✅ (`AssetLookupScreen` at `/assets`, reachable from the scanner's "Enter code instead" and camera-refused/error fallback; the dashboard's "Find an asset" field also resolves a typed code via the same call → resolves via `GET /api/assets/qr/{code}` → detail screen; non-leaking 404 + offline states) |
| FR-028 — Asset search/filter (basic lookup + recent list) | Student 1 (Jayashan) | ✅ (`/assets/search`, reached from the dashboard's "Find an asset" field — which pre-fills `?q=` when the text isn't an exact code — or its filter icon; server-side server-side search by code/name/custom attribute, department/location/category/asset-type/status/condition filters, sorting and pagination via `GET /api/assets`; mock asset data supported. **Gap found 2026-09-27, not fixed (owner's call):** the department/location/category/asset-type filters send *names* (`department=Fleet`) but `AssetQueryParameters` only accepts ids (`departmentId`, `locationId`, `categoryId`, `assetTypeId`), and `sort_by`/`sort_order`/`page_size` aren't bound either (`sortBy`/`sortDirection`/`pageSize`) — so those filters and the sort are silently ignored by the API) |
| FR-029 — Record asset condition | Student 1 (Jayashan) | ✅ (`features/assets/` — `PATCH /api/assets/{id}/condition` via the condition-update sheet, five-point scale, history written server-side; gated client-side to ACTIVE/UNDER_MAINTENANCE. Officer-only in the UI via `canUpdateAssetConditionProvider`, matching the backend's `CanManageAssets` — the earlier Staff-sees-a-button-that-403s gap is closed and covered by `asset_detail_screen_test.dart`) |
| FR-031 — Physical verification (presence/location/condition assertion) | Student 1 (Jayashan) | ✅ (`AssetVerificationScreen` at `/assets/:id/verify` — Officer-only in the router guard — submits the ad-hoc assertion via `POST /api/assets/{id}/verify`. Reached when an Officer starts verification (asset detail's Verify, or Scan to verify) for an asset with no pending campaign task, after a confirm dialog. Shares `VerificationForm` with FR-059. *Was unreachable until 2026-09-27 — the earlier ✅ here predated the route actually existing.*) |
| FR-033 — Fault report with photo evidence | Student 2 (Seneja) | ✅ (`ReportFaultScreen` at `/maintenance/report` — from asset detail (asset locked), the dashboard or the Faults tab (department asset picker); observed condition, description (≤2000), optional camera/library photo compressed to ≤1MB (IF-11) → `POST /api/maintenance/photos` (JPEG part) → `POST /api/maintenance/faults`. Staff and Officer. The reporter tracks it under *My reports* (`GET /api/maintenance/my-reports`, polled while visible). Photo upload was broken until 2026-09-27 — see that entry) |
| FR-037 — Maintenance status progress update | Student 2 (Seneja) | ✅ (`FaultDetailScreen` at `/maintenance/:id` — four-step tracker; the assigned officer (matched on `assignee_id`/email from `GET /api/me`) gets **Start work**, APPROVED → IN_PROGRESS via `POST /api/maintenance/{id}/start`. Only that transition is offered on mobile — SRS §3.4 gives Flutter "Progress update only"; approve/assign/cost, completion and cancel are React. Widget-tested) |
| FR-042 — Maintenance list/filter | Student 2 (Seneja) | ✅ (`MaintenanceRecordsView` — Officer's Faults tab → *All records*; `GET /api/maintenance` with status/priority/type/department/asset/assignee/date-range filters, four sorts and page-by-page "Load more", all server-side (department scoping stays the API's). Staff keep *My reports* only. Widget- and unit-tested) |
| FR-043 — Raise transfer request | Student 3 (Nipuna Bhanuka Samarasinghe) | ✅ (`InitiateTransferScreen` at `/transfers/new` — asset by code/scan or pre-filled from asset detail's *Request Transfer* (Officer, ACTIVE assets), destination department → location (cascading, `/api/departments` + `/api/locations?departmentId=`) → `POST /api/transfers` → opens the new transfer. `TransferListScreen` at `/transfers` with status filter. **Gap:** the SRS text includes a *reason*, but the API's `InitiateTransferRequest` has no reason field — needs a backend change first. Repaired 2026-09-27, see that entry) |
| FR-046 — Scan-to-confirm transfer receipt | Student 3 (Nipuna Bhanuka Samarasinghe) | ✅ (`TransferDetailScreen` at `/transfers/:id` — while APPROVED and heading into the officer's department: *Scan to confirm receipt* → shared scanner (manual code entry still available, IF-10) → scanned asset must be the transfer's asset → `POST /api/transfers/{id}/confirm-receipt`. Dashboard *Transfers to receive* lists these. Tested for URL/params, destination gating and status gating; confirm flow was unusable until 2026-09-27) |
| FR-058 — Verification task list, ordered by due date | Student 4 (Hasitha) | ✅ (`features/verification/` — the Verify tab at `/verification`, `GET /api/verification-tasks?mine=true`, grouped Overdue / Upcoming / Completed with counts; loading/empty/error/populated states. A Campaigns segment adds read-only campaign context — progress, scope, the officer's tasks in it — via the two `GET` campaign endpoints only; campaign management stays React-only per SRS §3.4) |
| FR-059 — Complete verification task via scan | Student 4 (Hasitha) | ✅ (**Scan is now enforced.** Verify tab / Home "Scan to verify" → scanner in identify mode (`/scan?purpose=identify`) → the officer's pending task for that asset opens already confirmed (`/verification/:taskId?scanned=1`); no task → offer ad-hoc (FR-031). Opening a task from the list instead requires "Scan asset" before *Present* can be submitted — a scan of a different asset is rejected with its code; *Not found* needs no scan. IF-10 manual code entry still works inside identify mode. Submits via `PATCH /api/verification-tasks/{id}/complete`; same `VerificationForm` as FR-031) |
| FR-061 — Raise discrepancy manually with photo | Student 4 (Hasitha) | ✅ (`RaiseDiscrepancyScreen` at `/verification/:taskId/discrepancy` — type/description/optional photo, compressed client-side to ≤1MB via `flutter_image_compress` (IF-11) before `POST /api/verification-tasks/photos` then `POST /api/verification-tasks/{taskId}/discrepancies`; backend role check widened to include InventoryOfficer — it was Auditor/Administrator-only, which would have 403'd every mobile call) |
| FR-067/069 — Initiate agentic evaluation, view workflow status | Student 4 (Hasitha) | ✅ (`features/workflows/` — `InitiateWorkflowScreen` at `/workflows/new` resolves an asset by code then `POST /api/agent-workflows`; `WorkflowListScreen`/`WorkflowDetailScreen` at `/workflows` and `/workflows/:id`, the latter polling `GET /api/agent-workflows/{id}` every 5s until resolved. This app never calls `/evaluate` or `/run-policy-agent` — those run inside the backend's own agent orchestration, not from a mobile trigger) |
| FR-076 — Display evaluation outcome | Student 4 (Hasitha) | ✅ (`WorkflowDetailScreen` — recommendation, approval status, high-impact flag, or the failure reason if the evaluation failed; no approval action, per the React-only responsibility boundary) |
| FR-080 — In-app notifications | Student 2 (Seneja) | ✅ (`features/notifications/` — bell with unread badge on the Home header (`GET /api/notifications/unread-count`, re-checked every minute while visible and on resume) → `NotificationsScreen` at `/notifications`, every role: All / Unread, newest first, "Load more" (`GET /api/notifications?onlyUnread=`), *Mark all read* (`PATCH …/read-all`); tapping one marks it read (`PATCH …/{id}/read`) and opens its record (`MaintenanceRecord` → `/maintenance/:id`; transfer/workflow/asset types mapped for when the backend emits them). Today the backend only emits maintenance notifications (assigned / status changed / cancelled). Widget-tested) |
| FR-083 — Task-focused dashboard | Student 4 (Hasitha) | ✅ (Home tab of the bottom-navigation shell, role-branched per §2.3.1. Officer: "Find an asset", at-a-glance counts, quick actions (report fault, request evaluation, transfers, campaigns, scan to verify), live previews of verification due / maintenance assigned to me / transfers to receive / recent evaluations / my fault reports. Staff: "Find an asset", report fault / search, my fault reports. All sections are live data — the former hard-coded maintenance/transfer sample rows are now real sections. Role scoping still checked against `CoreGrid/docs/srs/02-overall-description.md` §2.3.1) |

### Component C (Nipuna Bhanuka Samarasinghe) — Transfers

FR-043 and FR-046 landed in PR #8 and were repaired the same day (see the 2026-09-27 transfers entry above
for the exact bugs). FR-049 (condemnation from the field) not started.

## Next milestone

Every requirement in this table is now built; FR-049 condemnation (Student 3) is the remaining Flutter
item outside it. Outstanding, in rough priority order:

1. **Device run against the live backend** of the maintenance and transfer flows — they're analyzer-clean
   and widget-tested, not yet exercised end-to-end (photo upload, start work, confirm receipt in
   particular).
2. **Student 2 (Seneja)** to review the FR-033/037/042/080 work done on their behalf, and **Student 3
   (Nipuna Bhanuka Samarasinghe)** the transfer repair, so the individual-contribution record (main SRS §12.1) stays accurate.
3. **FR-028** asset-search filter/sort parameter mismatch (Student 1) — see that row.
4. **FR-043 reason** — needs `Reason` on the backend's `InitiateTransferRequest` before the form can send
   it.
5. **Enable password recovery for the mobile ThunderID app** and set `THUNDERID_APPLICATION_ID` (see the
   2026-09-27 password-reset entry); recovery emails also need ThunderID SMTP, deferred to deployment.
6. FR-049 condemnation.
7. `staging`/`prod` ThunderID client registrations (`mobile-specification.md` §5.1) before a release build;
   the dev client (`doc/setup/thunderid-mobile-client.md`) is registered and in local use.


