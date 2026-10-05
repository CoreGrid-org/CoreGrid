# CoreGrid Mobile SRS — Team, AI-Use and Commit-Evidence Sections

Moved from `coregrid-mobile/doc/software-requirements-specification.md` (v1.0, sections 10, 11 and Appendices A–B) on 2026-10-04 so that the mobile repository's SRS stays a product document. Section numbers are kept as they were.

## 10. Team roster

| Student | Name | Primary area |
|---|---|---|
| Student 1 | Jayashan Guruge | Asset registry, QR identification, verification, and mobile asset UI. |
| Student 2 | Seneja Ramanayaka | Maintenance, notifications, and related mobile integration. |
| Student 3 | Nipuna Bhanuka Samarasinghe | Transfer/disposal domain and transfer mobile implementation. |
| Student 4 | Hasitha Erandika | Authentication, organisation configuration, integration, dashboards, documentation, and release support. |

Student IDs, GitHub handles, and emails remain placeholders until supplied by the named members.

## 11. AI usage disclosure

CoreGrid follows this rule: **AI proposes → owner reviews the diff → owner tests it → owner understands it → Git records it.** AI-generated code is not accepted without review. Each student is responsible for maintaining their own tool/model, date, task, accepted/rejected output, and verification record.

| Date | Tool/model | Scope | Verification |
|---|---|---|---|
| 2026-09-28 | Codex | Audited mobile documentation, standardized documentation filenames, rewrote the README, and organized this SRS/commit appendix. | Reviewed repository paths, cross-checked links, inspected `git log --all`, and did not create a commit. |

Any member-specific AI record must be expanded in the submitted assessment report with the exact tool/model used. No owner may claim work they cannot explain, modify, test, or debug.

## Appendix A — Repository commit history

The following table is generated from `git log --all` for this repository at the time of this documentation update. It records commit messages as evidence; merge commits and revert commits are intentionally retained.

| Commit | Date | Author | Message |
|---|---|---|---|
| `f077d49` | 2026-09-28 | HasithaErandika | revert: bhanuka merged tasks |
| `e754e07` | 2026-09-28 | Jayashan | Merge pull request #11 from CoreGrid-org/task/ui-enhancement |
| `d2f24a3` | 2026-09-28 | Jayashan | style(ui): apply simple orange and white theme to dashboard and surfaces |
| `5f25914` | 2026-09-27 | Hasitha Erandika | Merge pull request #10 from CoreGrid-org/feature/notify_maintenance |
| `9a8a2d2` | 2026-09-27 | seneja | refactor(transfers): clean up obsolete transfer screens and models |
| `7d0a59a` | 2026-09-27 | seneja | chore: update app routing, auth config tests, and specifications |
| `958572b` | 2026-09-27 | seneja | feat(notifications): introduce notifications feature |
| `4c303fc` | 2026-09-27 | seneja | feat(maintenance): enhance fault reporting, add maintenance records view and update officer actions |
| `91df428` | 2026-09-27 | HasithaErandika | feat: add org config with password reset by thunderID issuer |
| `736555c` | 2026-09-27 | Hasitha Erandika | Merge pull request #8 from CoreGrid-org/feature/transfer-request-and-receipt |
| `caa1fa3` | 2026-09-27 | Hasitha Erandika | Merge branch 'development' into feature/transfer-request-and-receipt |
| `e8d3d92` | 2026-09-27 | HasithaErandika | feat: shell UI improvements |
| `635f6db` | 2026-09-27 | HasithaErandika | feat: improve UIUX and design revamp |
| `2a1b9a3` | 2026-09-27 | HasithaErandika | fix: local auth certificate validations |
| `9ef8100` | 2026-09-27 | NipunaBhanuka18 | feat(mobile): implement FR-043 (initiate transfer) and FR-046 (scan-to-confirm receipt) |
| `1c72182` | 2026-09-26 | HasithaErandika | fix(auth): request roles scope and show why /api/me failed |
| `3c62524` | 2026-09-26 | HasithaErandika | refactor(assets): fold search/verify into AssetsApi and fix tests |
| `c5258c2` | 2026-09-26 | HasithaErandika | fix(auth): return to the app after ThunderID sign-in |
| `2eb218d` | 2026-09-25 | Jayashan | Merge pull request #7 from CoreGrid-org/fix/asset_feature |
| `6fafa96` | 2026-09-25 | Jayashan | fix: enable ad hoc asset verification without a pending task |
| `7d649da` | 2026-09-25 | Jayashan | Merge pull request #6 from CoreGrid-org/fix/frontend-issues |
| `abb6ad0` | 2026-09-25 | Jayashan | add comments |
| `a607e9c` | 2026-09-25 | Jayashan | add report fault feature and  improve ui |
| `0608a65` | 2026-09-25 | Jayashan | fix ui/ux staff dashbaord |
| `d3f1acc` | 2026-09-25 | HasithaErandika | docs: update tasks to the backend implementations refctor |
| `9bff166` | 2026-09-16 | Jayashan | Merge pull request #4 from CoreGrid-org/feature/asset |
| `878282c` | 2026-09-16 | Jayashan | feat: add QR asset scanning flow |
| `9a4c2ba` | 2026-09-16 | Jayashan | Merge pull request #3 from CoreGrid-org/feature/asset |
| `0bd5ae8` | 2026-09-16 | Jayashan | feat(mobile): implement asset verification workflow and improve Ui/Ux related Asset and Discrepancy |
| `6f300b4` | 2026-09-15 | Jayashan | wire asset search page with backend |
| `dc792b6` | 2026-09-15 | Jayashan | improve asset ui/ux |
| `3c7eb3d` | 2026-09-15 | HasithaErandika | feat: remove dev bypass and improve mobile UI, add onbording screens. |
| `1eec808` | 2026-09-15 | HasithaErandika | feat: init the basic dashboards map with verification and workflows. |
| `760943f` | 2026-09-15 | HasithaErandika | docs: correct the setup Docs |
| `52d91b1` | 2026-09-09 | Hasitha Erandika | Merge pull request #1 from CoreGrid-org/feature/asset-registry-qr |
| `3efcf76` | 2026-09-09 | Jayashan | Update progress.md |
| `ee8003f` | 2026-09-09 | Jayashan | implement FR-020,FR-028 ,FR-029,FR-031 asset registry |
| `7f3a475` | 2026-08-18 | HasithaErandika | feat: Update setup with ThunderID and Dev Pass for Dashboards for Inventory Officer + Staff |
| `e6ab745` | 2026-08-18 | HasithaErandika | feat: Flutter mobile core init |
| `f18a333` | 2026-08-17 | HasithaErandika | chore: project doc init. |

## Appendix B — AI review checklist

- [ ] The named owner reviewed the resulting diff.
- [ ] The owner ran the relevant tests or documented why they could not run.
- [ ] The owner checked security, authorization, and data-scoping implications.
- [ ] The owner understands the final implementation.
- [ ] The owner recorded the tool, model, task, accepted/rejected output, and verification.
- [ ] No external AI assistant will be used during the demonstration or viva.

*End of Software Requirements Specification — CoreGrid Mobile, Version 1.0.*
