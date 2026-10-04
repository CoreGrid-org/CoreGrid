# Contribution and Pull-Request History

This consolidated record uses the PR list supplied by the team and the implementation evidence available in
the checked repositories. Titles are preserved verbatim. A PR title proves that work was submitted, but does
not by itself prove that the final checked branch still contains that implementation.

## Mobile repository — reported pull requests

Repository: `CoreGrid-org/coregrid-mobile`. All entries were reported as closed.

| PR | Member | Reported title | Scope/evidence |
|---:|---|---|---|
| #1 | HasithaErandika | *(title not supplied)* | Initial mobile project work; confirm the exact title in GitHub before submission. |
| #2 | HasithaErandika | `implement FR-020,FR-028 ,FR-029,FR-031 asset registry` | Asset detail, search, condition, and physical-verification baseline. |
| #3 | jguruge | `Development` | Asset feature development integration. |
| #4 | jguruge | `Feature/asset` | Asset feature integration. |
| #5 | HasithaErandika | `feat: add QR asset scanning flow` | QR scan flow. |
| #6 | jguruge | `chore: complete milestone 2 in the project` | Milestone and frontend integration fixes. |
| #7 | jguruge | `Fix/frontend issues` | Frontend and asset-flow corrections. |
| #8 | HasithaErandika | `fix: enable ad hoc asset verification without a pending task` | Ad-hoc verification entry point. |
| #9 | NipunaBhanuka18 | `feat(mobile): FR-043 initiate transfer + FR-046 scan-to-confirm receipt` | Mobile transfer request and receipt-confirmation flow. |
| #10 | seneja | `feat(mobile): FR-049 asset condemnation flow` | Reported condemnation flow; not present in the checked mobile tree, so it remains unverified. |
| #11 | jguruge | `Feature/notify maintenance` | Reported maintenance/notification integration. |

### Mobile evidence reconciliation

The checked mobile Git history contains merge commits labelled #1, #3, #4, #6, #7, #8, #10 and #11, but some
titles/numbers differ from the supplied PR list. For example, local history labels the transfer merge as #8
and notifications as #10, while the supplied list labels them #9 and #11. The final report should link the
GitHub PR pages directly and should not infer PR identity from a local merge commit.

The checked tree confirms authentication, dashboards, assets, QR scanning, verification, maintenance,
notifications, transfers, workflows, and tests. It currently has no condemnation screen, route, API call, or
FR-049 test; FR-049 therefore remains an open mobile implementation item until its branch/commit is reconciled.

## Main CoreGrid repository — reported pull requests

Repository: `CoreGrid-org/CoreGrid`. All entries were reported as closed.

| PR | Member | Reported title | Primary area |
|---:|---|---|---|
| #1 | HasithaErandika | *(title not supplied)* | Initial repository/development integration. |
| #2 | HasithaErandika | `Development` | Development integration. |
| #3 | HasithaErandika | `Asset Initialize with migrations` | Asset schema and migrations. |
| #4 | HasithaErandika | `Component C: add AssetTransfer and DisposalRequest entities + migration` | Transfer/disposal entities and migration. |
| #5 | HasithaErandika | `Implement backend and frontend to Component A` | Component A baseline. |
| #6 | HasithaErandika | `Jayashan/task/improve asset component` | Asset component improvements. |
| #7 | HasithaErandika | `Jayashan/feature/asset history timeline` | Asset history timeline. |
| #8 | seneja | `link inventory officers' register page to admin Register page` | Maintenance/inventory integration. |
| #9 | NipunaBhanuka18 | `feat: implement Component B Maintenance Management` | Component B maintenance implementation. |
| #10 | HasithaErandika | `Bhanuka/feature/transfer disposal` | Transfer/disposal integration. |
| #11 | HasithaErandika | `Update With Baseline Implementations` | Baseline implementation update. |
| #12 | HasithaErandika | `Component C: Complete Transfer & Disposal Workflow and Budget Analysis Agent` | Component C and Budget Analysis Agent. |
| #13 | seneja | `Development` | Maintenance/notification integration. |
| #14 | seneja | `Seneja/feature notify maintanance` | Notification/maintenance work. |
| #15 | HasithaErandika | `Implement Planner Agent workflow integration` | Planner Agent integration. |
| #16 | jguruge | `Main Update 2` | Component A/main integration. |
| #17 | jguruge | `feat: FR-019 : enforce validation rules for asset attributes` | Asset attribute validation. |
| #18 | jguruge | `Migrate Planner Agent to .NET backend` | Planner Agent migration. |
| #19 | HasithaErandika | `feat(verification): generate verification tasks for campaigns` | Verification campaign tasks. |
| #20 | seneja | `Development` | Maintenance record/report integration. |
| #21 | jguruge | `feat(reports): add maintenance report panel and integrate with report…` | Maintenance reports. |
| #22 | NipunaBhanuka18 | `Feature/component a enhancements` | Component A enhancements. |
| #23 | jguruge | `Bhanuka/feature/transfer disposal` | Transfer/disposal integration. |
| #24 | HasithaErandika | `docs: add assignment report structure and sections` | Assignment documentation. |
| #25 | jguruge | `Backend Refactor` | Backend refactor. |
| #26 | jguruge | `Fix comments` | Comment and documentation cleanup. |
| #27 | NipunaBhanuka18 | `fix` | Component C/integration fix. |
| #28 | jguruge | `feat(ui): refine asset filters, carbon dropdowns, category card layou…` | Asset UI/filter improvements. |
| #29 | NipunaBhanuka18 | `fix issue` | Component C/agent integration fix. |
| #30 | jguruge | `Wire Budget Analysis Agent into multi-agent orchestration pipeline (Node 3)` | Budget Agent orchestration. |
| #31 | jguruge | `Feature/component a enhancements` | Component A enhancements. |
| #32 | seneja | `add ReporterByuserId` | Maintenance reporter attribution. |
| #33 | HasithaErandika | `feat: Allow future completion dates and validate against requested da…` | Maintenance date validation. |
| #34 | NipunaBhanuka18 | `test(frontend): lock down TransfersPage role-boundary regression coverage` | Transfer role-boundary tests. |
| #35 | jguruge | `test(frontend): lock down TransfersPage role-boundary regression coverage` | Duplicate title in supplied list; verify author/title in GitHub. |
| #36 | jguruge | `update doc files` | Documentation update. |
| #37 | HasithaErandika | `Hh` | Reported milestone/documentation PR; clarify the title in GitHub if possible. |

## Corrected member allocation

| Member / handle | Primary component | Mobile scope | Agent / cross-cutting scope |
|---|---|---|---|
| Jayashan Guruge (`jguruge`) | A — Asset Registry & QR Identification | Assets, QR scan, search/detail, condition, verification UI | Planner Agent and Component A integration. |
| Seneja Ramanayaka (`seneja`) | B — Maintenance Management | Fault reports, maintenance records, notifications | Maintenance Analysis Agent and notification integration. |
| Nipuna Bhanuka / Bhanuka (`NipunaBhanuka18`) | C — Transfer & Disposal | Transfer request and receipt confirmation; condemnation reported but not verified in current mobile code | Budget Analysis Agent. |
| Hasitha Erandika (`HasithaErandika`) | D — Audit, compliance, organisation configuration, and user administration | Authentication, app shell, dashboards, verification/workflows, CI and mobile integration | Policy Compliance Agent, human-approval checkpoint, release and consolidated documentation. |

Student IDs and email addresses still need to be supplied in the SRS roster. The handles above are the
attribution identifiers visible in the supplied PR list and repository history.
