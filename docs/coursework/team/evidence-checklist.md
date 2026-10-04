# Individual Evidence Checklist

Moved from the progress tracker, now [`../progress.md`](../progress.md) (2026-10-04). Tracks the coursework evidence each member must attach, against [`team-roster-and-work-allocation.md`](team-roster-and-work-allocation.md) §18.3–§18.6.


The implementation sections below show what is built. This checklist tracks the additional evidence required
to fully satisfy the individual contribution requirements in [SRS §18.3–§18.6](team-roster-and-work-allocation.md).
An item is complete only when the artefact is linked or attached, not merely when the feature exists in code.

### Student 1 — Jayashan Guruge — Component A

**Current assessment:** 🟡 Implementation mostly present; individual evidence package incomplete.

- [x] Add the Flutter scanner widget test covering camera success, permission refusal, unknown code, offline
  recovery, and manual-entry fallback.
- [ ] Link the Component A issues and reviewed PRs for FR-016–FR-032, including reviewer names and dates.
- [x] Attach the final React asset-form/component test output and the mobile asset test output.
- [x] Link the Component A README/design note and ADR-006 input for attribute-value storage.
- [x] Complete Jayashan's Appendix E AI log with tool/model, dates, accepted/rejected output, and verification.
- [x] Attach a device or emulator record for QR scan, asset lookup, condition update, and verification.

### Student 2 — Seneja Ramanayaka — Component B

**Current assessment:** 🟡 Implementation mostly present; notification and contribution evidence incomplete.

- [ ] Add or link the notification failure-isolation test proving delivery failure does not roll back maintenance
  completion.
- [ ] Add an explicit storage-provider integration test or recorded verification for maintenance-photo upload and
  retrieval through the configured object storage.
- [ ] Add the Component B notification-provider design note referenced by SRS §18.4.
- [ ] Link the maintenance/notification issues and reviewed PRs for FR-033–FR-042 and FR-077–FR-080, including
  reviewer names and dates.
- [ ] Attach backend, React, and mobile test output for maintenance, notification, photo, and status-transition
  flows.
- [ ] Complete Seneja's Appendix E AI log with tool/model, dates, accepted/rejected output, and verification.

### Student 3 — Nipuna Bhanuka (Bhanuka) — Component C

**Current assessment:** 🟡 Web/API and Budget Agent implementation present; mobile FR-049 and formal evidence incomplete.

- [ ] Reconcile the reported mobile FR-049 PR with the checked `coregrid-mobile` branch. Restore or implement the
  condemnation screen, route, API call, authorization gate, evidence capture, and tests.
- [ ] Add the dedicated concurrency-conflict test for DR-11 using PostgreSQL/EF Core optimistic concurrency.
- [ ] Link negative tests for all disposal preconditions P1–P6 and the separation-of-duties test to FR-051.
- [ ] Add a documented merge-conflict-resolution record for Component C integration work.
- [ ] Add/link Flutter tests for transfer creation, asset identity matching, receipt confirmation, and FR-049.
- [ ] Add the Component C concurrency-control design note and Budget Agent evidence.
- [ ] Link the transfer/disposal issues and reviewed PRs for FR-043–FR-055, including reviewer names and dates.
- [ ] Complete Nipuna's Appendix E AI log with tool/model, dates, accepted/rejected output, and verification.

### Student 4 — Hasitha Erandika — Component D and group integration

**Current assessment:** 🟡 Implementation and documentation coordination mostly present; release/evidence verification incomplete.

- [x] CI is passing for the main CoreGrid repository and the sibling mobile repository, including backend,
  frontend, PostgreSQL integration, Flutter analysis/tests, and mobile APK build. Attach the final run links and
  secret-scanning record to the submission evidence package.
- [ ] Run and record the golden cases required by SRS §13.4, especially approval authorisation, rejection,
  checkpoint resume, and disposal execution.
- [ ] Attach live end-to-end evidence for the four-agent workflow and the human-approval checkpoint.
- [ ] Link Hasitha's reviewed PRs/issues for Component D, authentication, CI, documentation, and integration work.
- [ ] Confirm the authorisation matrix, append-only database checks, and organisation-isolation tests with a
  reproducible test command and output.
- [ ] Attach the mobile authentication, role-gate, dashboard, verification, workflow, and device-run evidence.
- [ ] Complete the consolidated README/report, demonstration script, submission links, and final Appendix E AI
  record for all members.

### Shared completion gate

- [ ] Replace every placeholder student ID and email in the SRS roster.
- [ ] Confirm every PR has a real GitHub link, reviewer, review date, requirement IDs, and final merge status.
- [ ] Reconcile the supplied PR list with local merge commits using the GitHub PR pages as the authority.
- [ ] Store final backend, frontend, and mobile test outputs under the submission evidence location.
- [ ] Update this tracker and SRS §18 only after the evidence links have been checked by the group leader.

