# CoreGrid Mobile — Implementation Progress

Status audited against the mobile repository and supplied PR list on 2026-09-28. This file describes what is built,
what is still limited, and what must be checked before release. The main platform progress tracker remains the
source for backend and React status.

For the evidence still required to fully satisfy each member's SRS allocation, see the
[individual allocation evidence checklist](../progress.md#individual-allocation-evidence--remaining-work).

## Requirement status

| Requirement | Status | Implementation |
|---|---|---|
| FR-001/007/008 | ✅ Done | ThunderID PKCE sign-in, role-aware navigation, refresh-token revocation on sign-out. |
| FR-020 | ✅ Done | Asset detail renders custom attributes from `data_type`, without hardcoded attribute names. |
| FR-024 | ✅ Done | Camera QR scan resolves `GET /api/assets/qr/{code}` and handles permission refusal, unknown codes, and offline state. |
| FR-025 | ✅ Done | Manual asset-code lookup is always available as the scanner fallback. |
| FR-028 | ✅ Done with API-contract follow-up | Search, filters, sorting, pagination, and recent lookup UI exist; some filter/sort requests still use parameter names the API does not bind. |
| FR-029 | ✅ Done | Five-value condition scale, history update, lifecycle gating, and Officer-only UI. |
| FR-031 | ✅ Done | Ad-hoc physical verification is routed at `/assets/:id/verify` and uses the shared verification form. |
| FR-033 | ✅ Done | Fault report with optional compressed photo evidence and reporter tracking. |
| FR-037 | ✅ Done | Assigned Officer can start work (`APPROVED → IN_PROGRESS`); web-only transitions remain out of mobile scope. |
| FR-042 | ✅ Done | Officer maintenance records view with server-side filters, sorting, and load-more pagination. |
| FR-043 | ✅ Done with backend follow-up | Transfer request creation with scan/code asset selection and cascading destination selection; request reason is not yet represented by the backend contract. |
| FR-046 | ✅ Done | Scan or manual code confirmation checks the scanned asset against the transfer before receipt confirmation. |
| FR-058 | ✅ Done | Officer verification tasks grouped by overdue, upcoming, and completed state. |
| FR-059 | ✅ Done | Scan-enforced task completion with manual code fallback and mismatch rejection. |
| FR-061 | ✅ Done | Manual discrepancy raising with compressed photo upload; backend permits the Officer role. |
| FR-067/069 | ✅ Done | Agent workflow initiation and polling of workflow status. |
| FR-076 | ✅ Done | Workflow recommendation, approval status, high-impact flag, and failure reason display. |
| FR-080 | ✅ Done | Notification inbox, unread count, mark-read actions, and navigation to related records. |
| FR-083 | ✅ Done | Role-branched task dashboard with live verification, maintenance, transfer, workflow, and fault sections. |
| FR-049 | ❌ Not verified in current tree | A closed PR is reported for condemnation, but the checked `lib/` and `test/` trees contain no condemnation screen, route, API call, or FR-049 test. Keep open until the PR's final commit is restored or its implementation is found. |

## Completed work

- Replaced development/bypass authentication with the real ThunderID Authorization Code + PKCE flow.
- Added role filtering for Officer and Staff navigation; Auditor and Administrator are sent to
  `/access-restricted` on mobile.
- Added onboarding, Account, notification, Faults, Verify, Workflows, and role-specific dashboard tabs.
- Added shared API, error, theme, media-compression, and UI-kit components so screens use one design system.
- Added QR scanning with torch, permission handling, authoritative lookup, offline recovery, and manual entry.
- Added dynamic asset attributes, condition updates, history, asset search, physical verification, and transfer
  entry points from asset detail.
- Completed maintenance fault reporting, photo upload correction, status tracking, Officer start-work action,
  and filtered maintenance records.
- Repaired transfer URL interpolation, query binding, destination scoping, receipt identity checks, routing,
  dashboard previews, and asset selection.
- Added password recovery links that open ThunderID's hosted recovery page in an external browser; CoreGrid
  never handles passwords.

## Known gaps and release follow-up

1. Run maintenance and transfer flows on a device against the live backend, especially photo upload, start work,
   and receipt confirmation.
2. Have the named owners review the work recorded for maintenance, notifications, and transfers.
3. Correct the FR-028 search parameter contract (`departmentId`, `locationId`, `categoryId`, `assetTypeId`,
   `sortBy`, `sortDirection`, `pageSize`) or update the client/API consistently.
4. Add `Reason` to the transfer request backend contract before exposing the SRS-required reason field.
5. Enable password recovery on the mobile ThunderID application and provide `THUNDERID_APPLICATION_ID`.
6. Add the staging and production ThunderID client registrations before release signing.
7. Reconcile the reported mobile FR-049 PR with the checked branch; implement or restore the condemnation
   screen, route, API call, authorization gate, evidence capture, and tests if it was reverted.
8. Reconcile supplied mobile PR numbers/titles with local merge commits before final assessment submission.

## Verification evidence

- `flutter analyze`: 0 issues in the latest recorded run.
- `flutter test`: 68 passing in the latest recorded run.
- Mobile CI is confirmed passing; `.github/workflows/ci.yml` runs analysis, tests, and a release APK build.
- Widget tests cover authentication states, asset detail and lookup, verification, maintenance, transfers,
  notifications, and workflow behaviour.
- Device-level end-to-end testing is still required for camera, TLS, photo upload, and live API flows.
