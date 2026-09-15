# Component A — Asset Registry and QR Identification (FR-021 – FR-032)

Status snapshot as of 2026-08-17, branch `jayashan/feature/qr-asset-detail`. Source of truth for done/not-done is [`doc/PROGRESS.md`](./PROGRESS.md); this file exists to lay out what's left to fully close out Component A in one place.

**Role note:** none of the Assets controllers (`AssetsController`, `AssetTypesController`, `AssetCategoriesController`) restrict by role — they only require `[Authorize]` (any authenticated user). Every FR below that names "Inventory Officer" as the actor is therefore reachable today by any logged-in user, including Administrator, since no role check gates it yet.

## Status by requirement

| FR | Requirement | Status | Gap |
|----|---|---|---|
| FR-019 | Attribute value validation on create and update | ✅ | None — `AttributeValidationRuleEngine` (2026-09-15) enforces `ValidationRule` on every create/update: `min`/`max` for NUMBER, `minLength`/`maxLength` for TEXT, `maxDate` for DATE. Required-field and data-type checks were already in place; rule enforcement was the remaining gap, now closed. |
| FR-021 | Register asset (type, name, department, location, acquisition date/cost, attributes) | ✅ | None |
| FR-022 | Unique human-readable asset code (org prefix + monotonic sequence, DB-constrained) | ✅ | None — `AssetCodeGenerator`, unique index `IX_Assets_OrganizationId_AssetCode` |
| FR-023 | QR label payload + printable label download | 🟡 | Real QR image renders in the asset detail modal (`qrcode` package). No printable-label download/print feature. |
| FR-024 | Mobile QR scan → authoritative record within 3s | ❌ | Flutter app not started |
| FR-025 | Manual asset-code entry as scan alternative | 🟡 | React ✅ (`AssetScanPage`, resolves via `GET /api/assets/qr/{code}`). Flutter ❌ |
| FR-026 | Amend descriptive fields/attributes/department/location, recorded in history | ✅ | `PUT /api/assets/{id}` diffs core fields + attributes and writes one `FIELD_AMENDMENT` entry per change |
| FR-027 | Immutable, ordered per-asset history (state change, amendment, verification, maintenance, transfer, disposal, agent recommendation) | 🟡 | Creation/amendment/condition-change now write to `AssetHistory`; `GET /api/assets/{id}/history` (paginated) exists; History section shown in the Asset Detail modal (also reachable from Update Asset's "View history" button). Remaining gap: transfer/maintenance/disposal/agent-recommendation event types have no writer yet since those features don't exist |
| FR-028 | Search by code/name/attribute; filter by department/location/category/type/status/condition; server-side sort+pagination | ✅ | None |
| FR-029 | Record condition (New/Good/Fair/Poor/Unserviceable), recorded in history | ✅ | `PATCH /api/assets/{id}/condition` writes a `FIELD_AMENDMENT` entry (skipped if resubmitted unchanged) |
| FR-030 | Computed residual value (straight-line depreciation from cost, acquisition date, useful life) | ❌ | `ResidualValue` is a free-entry field from the client, never derived server-side |
| FR-031 | Officer physical verification (presence/location/condition assertion vs. register, discrepancy raised) | ❌ | `POST /api/assets/{id}/verify` doesn't exist; FR is Flutter-only per SRS, Flutter not started |
| FR-032 | Prevent deletion of assets with history; disposal is the only exit | 🟡 | No `DELETE` endpoint exists at all (satisfies the letter), but disposal workflow (Component C) isn't built either, so assets currently have no exit path |

**Legend:** ✅ done · 🟡 partial · ❌ not started

## What's needed to fully close Component A

### 1. ~~`AssetHistory` write-side + read endpoint + frontend timeline~~ — done
`CreateAssetAsync` writes a `STATUS_CHANGE` entry on registration; `UpdateAssetAsync` diffs core fields + attributes and writes one `FIELD_AMENDMENT` entry per change; `UpdateConditionAsync` writes a `FIELD_AMENDMENT` entry (skipped when resubmitted unchanged). `AssetHistoryEventTypes` (`backend/Domain/Assets/AssetHistoryEventTypes.cs`) centralizes the 7 constrained values, also now used by `DiscrepancyService`. `GET /api/assets/{id}/history` (paginated) added to `AssetsController`/`AssetService`. Frontend: `useAssetHistory` hook, `getAssetHistory` API client function, and a History section in `AssetDetailModal.tsx` (also reachable via a "View history" button on the Update Asset edit page). Verified end-to-end in the browser. Remaining gap under FR-027: `TRANSFER`/`MAINTENANCE`/`DISPOSAL`/`AGENT_RECOMMENDATION` event types have no writer yet, since those features don't exist.

### 2. QR label printing (rest of FR-023)
- Printable/downloadable label (PDF or print-styled view) from the already-generated QR image.

### 3. Residual value computation (FR-030)
- Server-side straight-line depreciation: `ResidualValue = AcquisitionCost - (AcquisitionCost / UsefulLifeYears) × YearsElapsed`, clamped at 0.
- Remove/ignore client-submitted `ResidualValue`; compute in `AssetService` on create/update and probably expose as computed-on-read too, since elapsed time changes it without any write happening.
- Needs `UsefulLifeYears` (or similar) on `AssetType` — confirm it exists.

### 4. Physical verification (FR-031)
- Flutter-only per SRS — blocked on the mobile app existing at all. When it starts, needs `POST /api/assets/{id}/verify` (presence/location/condition assertion → compare to register → raise discrepancy via existing Verification/Component D pipeline).

### 5. Mobile scan + manual entry parity (FR-024, FR-025)
- Blocked on Flutter app existing. React/manual-entry side is already done (`AssetScanPage`).

### 6. Disposal as the only register exit (FR-032)
- Blocked on Component C's disposal workflow. Once it exists, confirm no `DELETE` endpoint is ever added to `AssetsController` and that disposal is the sole state transition out of "active."

### 7. Role enforcement (cross-cutting, not a numbered FR gap but implied by every "Officer shall…" line)
- Decide whether/when to add `[Authorize(Roles = nameof(CoreGridRole.InventoryOfficer))]` (or a broader officer+admin policy) to the Assets endpoints. Currently anyone authenticated — effectively "admin does everything" — can register, amend, and condition-change assets.

## Suggested order

1. ~~`AssetHistory` write-side + read endpoint + frontend timeline~~ — done (closed FR-026, FR-027 partially, FR-029).
2. Residual value computation (FR-030) — self-contained, backend-only, no dependency on other components.
3. QR label printing (FR-023) — self-contained, frontend-only.
4. Role enforcement pass — cross-cutting, do once the above stabilizes so you're not re-touching every controller mid-flight.
5. FR-024/025/031 (Flutter) and FR-032 (disposal) — blocked on other components/platforms, not actionable from this branch alone.
