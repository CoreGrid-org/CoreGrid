# SOFTWARE REQUIREMENTS SPECIFICATION
## CoreGrid
### A Configurable, Agentic-AI-Assisted Asset Lifecycle Management Platform

**Version 1.1  |  Baseline Release**

Prepared in accordance with IEEE 830 / ISO-IEC-IEEE 29148 requirements-specification practice

| Item | Detail |
|---|---|
| Product name | CoreGrid — Intelligent Asset Lifecycle Management Platform |
| Document type | Software Requirements Specification (SRS) |
| Module | SE3090 — Software Engineering Frameworks, Assignment 1 |
| Programme | BSc (Hons) in Information Technology, specialising in Software Engineering / Artificial Intelligence |
| Academic period | Year 3, Semester 1, 2026 |
| Group | SE3090_G<NN>  (to be completed by group leader) |
| Author | Hasitha Erandika (Group Leader) |
| Team members | See the full roster below; §18 is the authoritative, per-member breakdown of ownership and evidence. |

### Team Roster

The single-line team-members field above names who is on the group; this table is the quick-reference version of it. It exists so a reader (or an evaluator) does not have to open §18 just to find out who owns what — every column here is drawn from, and must stay consistent with, the fuller record in [§18](18-team-roster-and-work-allocation.md#182-roster). If the two ever disagree, §18 is authoritative and this table is stale and should be corrected in the same edit.

| Student # | Name | Student ID | GitHub username | Email | Component owned | Group role |
|---|---|---|---|---|---|---|
| Student 1 | Jayashan Guruge | `<ID>` | `jguruge` | `<email>` | A — Asset Registry & QR Identification | Member |
| Student 2 | Seneja Ramanayaka | `<ID>` | `seneja` | `<email>` | B — Maintenance Management | Member |
| Student 3 | Nipuna Bhanuka (Bhanuka) | `<ID>` | `NipunaBhanuka18` | `<email>` | C — Transfer & Disposal | Member |
| Student 4 | Hasitha Erandika | `<ID>` | `HasithaErandika` | `<email>` | D — Audit & Compliance, org configuration, user administration | Group Leader |

Placeholders (`<ID>`, `<github-handle>`, `<email>`) are left for the named student to fill in — they are not invented here. See [§18.2](18-team-roster-and-work-allocation.md#182-roster) for what a complete roster row must capture and why each field is required, and [§18.11](18-team-roster-and-work-allocation.md#1811-keeping-the-roster-current) for how this table is kept in sync as the project proceeds.
| Identity provider | ThunderID (OIDC / OAuth 2.0); organisation scoping is done in CoreGrid's own database, not ThunderID (Section 4.2) |
| Mandatory stack | ASP.NET Core Web API · PostgreSQL · React (IBM Carbon Design System) · Flutter · Agentic AI (.NET-native `IAgentNode`/`IModelClient`, Section 7.2.1, ADR-010) |
| Status | Approved baseline for implementation |

## Document Control

### Revision History

| Version | Date | Author | Summary of change | Status |
|---|---|---|---|---|
| 0.1 | 2026-08-01 | Hasitha Erandika | Initial scope, objectives and domain analysis drafted from the CoreGrid architecture and feasibility study. | Draft |
| 0.2 | 2026-08-04 | Hasitha Erandika | React / Flutter responsibility boundary, four business components and agent roles added. | Draft |
| 0.3 | 2026-08-06 | Hasitha Erandika | Configurable asset-type and custom-attribute platform model incorporated. | Draft |
| 0.4 | 2026-08-07 | Hasitha Erandika | Identity and access management re-based on ThunderID with organisation-scoped users. | Draft |
| 1.0 | 2026-08-08 | Hasitha Erandika | Complete functional, data, agentic-AI, non-functional, verification and traceability specification. Baselined for the seven-week implementation. | Baselined |
| 1.1 | 2026-08-09 | Hasitha Erandika | React client design system mandated as IBM Carbon Design System (ADR-008); backend target environment corrected to .NET 10. | Draft |
| 1.2 | 2026-08-10 | Hasitha Erandika | Generalised deployment and identity sections from government/ministry-specific language to a generic customer organisation, and documented the product's two-stage delivery plan: M0 (self-hosted, current baseline) and M1 (multi-tenant hosted SaaS, Section 17). Corrected an inaccurate claim that M1 would require per-tenant ThunderID organisations. | Draft |
| 1.3 | 2026-08-17 | Hasitha Erandika | Relicensed from MIT to Apache License 2.0 (`LICENSE`, `NOTICE`) to support an open-core commercial model. Added Section 19, Business Plan: Community edition (self-hosted, open-source) sold to government/institutional buyers first; multi-tenant SaaS (M1) built only once Community-edition revenue funds it. Added Section 11.3, object-storage integration requirements (INT-09 to INT-14) and ADR-009, closing the previously unspecified `StorageKey` provider for photographic evidence — Cloudflare R2 for CoreGrid-operated editions, any S3-compatible endpoint for self-hosted Community-edition customers. | Draft |
| 1.4 | 2026-08-17 | Hasitha Erandika | Added Section 19.4.1, What SaaS Adds: the multi-tenant SaaS edition differentiates by genuinely new, infrastructure-native capability drawn from Section 17 (cross-organisation analytics, additional agents, offline sync, managed operations), not by withholding any functional requirement from the Community edition — preserves the Section 19.2 rationale that self-hosting must remain feature-complete for the government/institutional buyer it targets. | Draft |
| 1.5 | 2026-08-18 | Hasitha Erandika | Scope change: restricted the Flutter client's per-role reach to match Section 3.4's own stated design principle (React is the management/control interface, Flutter is the field operations interface). Auditor and Administrator are now web-console-only; Inventory Officer uses both clients; Staff remains mobile-only. Corrected FR-059 (dropped Auditor's Flutter scan-to-complete), FR-067 and FR-069 (Administrator's and Auditor's workflow-initiation/monitoring access is React only; Officer keeps both) and Section 3.4. No backend authorisation change — Appendix B's role-permission grants are unaffected, since they describe API authorisation, not client UI surface. | Draft |
| 1.6 | 2026-08-18 | Hasitha Erandika | Added Section 3.4.1, Users by Role and Platform: a consolidated table and integration diagram (Figure 10) showing which platform each role uses and why, and how both clients share one ThunderID identity provider, claim contract and API. Documentation consolidation of the v1.5 scope change — no new requirement introduced. | Draft |
| 1.7 | 2026-09-28 | Hasitha Erandika | Reconciled current implementation status, member GitHub handles, supplied mobile PR history, and supplied CoreGrid PR history. Recorded FR-049 mobile condemnation as unverified in the checked tree pending branch/commit reconciliation. | Draft |

### Individual Contribution Log — Hasitha Erandika

This log groups the work attributed to Hasitha Erandika in the repository history and the Component D progress record. The abbreviated commit IDs provide evidence for review against the corresponding implementation, tests and documentation.

| Date | Task area | Contribution summary | Commit evidence |
|---|---|---|---|
| 2026-08-08–2026-08-12 | Project foundation and identity | Established the repository and SRS baseline, structured the backend and frontend, implemented the initial login/authentication flow, and set up organisation-scoped identity and user provisioning. | `1177d28`, `bd03623`, `88d7d03`, `eff5ef2`, `27fa3a8`, `5702ca1`, `9822fdd`, `eb16174` |
| 2026-08-13–2026-08-15 | Component D foundation | Implemented the initial Audit & Compliance, organisation configuration and user-administration work, including role dashboards, user management and backend restructuring. | `1c16300`, `1a19900`, `a708373`, `6658b1b`, `87b2fd3` |
| 2026-08-17–2026-08-21 | Architecture, business plan and CI | Updated the licence and business plan, documented deployment and identity decisions, added CI workflow ownership, implemented policy management and connected audit workflows. | `023c7e8`, `b98966c`, `a726f62`, `bad3d62`, `31130c6` |
| 2026-09-12 | Tenant isolation and authorisation | Enforced the organisation query filter, added automatic user provisioning and tightened endpoint authorisation. | `aa00c9f` |
| 2026-09-14–2026-09-15 | Component D completion and authentication verification | Completed the main Component D progress record, added tests, verified authentication behaviour and updated the agent plan. | `4ca2f62`, `8141231`, `0040165`, `31957b2` |
| 2026-09-18–2026-09-19 | Backend quality and RBAC | Planned and executed backend modularisation, completed the constants/dead-code sweep, wired RBAC and selected SRS gap items, repaired test-project references and cleaned up CI. | `f67370a`, `8048b50`, `6495e9c`, `9cca43e`, `3fe85bd`, `4dda6bb`, `e46e777` |
| 2026-09-18–2026-09-25 | Documentation, validation and UI improvements | Maintained progress and assessment documentation, added frontend validation, improved reusable UI components and corrected frontend/backend issues. | `a217918`, `9745e9c`, `93d2b6e`, `583671e`, `1bc5e40` |
| 2026-09-26 | Integration, testing and deployment support | Added photo and LLM fetching endpoints, expanded tests for photos, LLMs and campaign reports, improved campaign/disposal/maintenance models, updated setup documentation, and added Docker components and CI corrections. | `2bfcb66`, `3ea91f0`, `242682c`, `81ba176`, `d6b9358`, `fdfa755` |
| 2026-09-27 | Mobile authentication and role authorisation | Added mobile ThunderID configuration/authentication support and corrected role-authorisation behaviour. | `29dd490`, `cf7b274` |

**Current status:** The Component D implementation covers organisation setup, department/location management, user administration, policies, verification campaigns, discrepancies, append-only audit logging, reporting, dashboards, the Policy Compliance Agent checkpoint, and the related React interfaces and tests. Remaining documented work is the runtime split between migration-owner and restricted database connections for full append-only enforcement, completion of the Policy Compliance Agent's business-action execution, and the planned shared-style/component sweep.

#### Complete Git-history contribution index — Hasitha Erandika

The following index contains every commit authored by Hasitha Erandika/HasithaErandika returned by `git log --all --author=Hasitha` at the time of this revision, including merge and branch-integration commits. Merge commits are included as integration work; they do not claim authorship of the merged member's original implementation.

| Date | Commit | Recorded contribution |
|---|---|---|
| 2026-09-27 | `29dd490` | Mobile ThunderID configuration and authentication. |
| 2026-09-27 | `cf7b274` | Role-authorisation correction. |
| 2026-09-26 | `fdfa755` | Added Docker components and corrected CI workflow issues. |
| 2026-09-26 | `d6b9358` | Structured documentation and updated tasks. |
| 2026-09-26 | `242682c` | Improved campaign-report models, disposals and maintenance frontend work. |
| 2026-09-26 | `2bfcb66` | Added backend endpoints for LLM and photo fetching. |
| 2026-09-26 | `3ea91f0` | Added tests for photos, LLMs and campaign reports. |
| 2026-09-26 | `81ba176` | Updated setup documentation. |
| 2026-09-26 | `a5d3cd7` | Updated settings and `.gitignore`. |
| 2026-09-26 | `583671e` | UI/UX improvements, bug fixes and reusable shared components. |
| 2026-09-25 | `1bc5e40` | Removed the banner and refactored the backend update. |
| 2026-09-25 | `93d2b6e` | Added frontend validations. |
| 2026-09-25 | `e60471e` | Merged the budget-agent orchestration pull request. |
| 2026-09-23 | `fa23104` | Merged the development branch. |
| 2026-09-19 | `7323a57` | Updated packages. |
| 2026-09-19 | `e46e777` | Implemented opt-in SRS gaps, RBAC wiring and CI cleanup. |
| 2026-09-19 | `4dda6bb` | Completed backend Phase 4 constants and dead-code sweep. |
| 2026-09-19 | `3fe85bd` | Completed backend modularisation Phases 1–3. |
| 2026-09-18 | `a217918` | Added the 0–100 system-marks rubric before and after each phase. |
| 2026-09-18 | `9cca43e` | Made mandatory value-type request fields non-defaultable. |
| 2026-09-18 | `332f2c7` | Deduplicated the `ProjectReference` after the merge. |
| 2026-09-18 | `0edfb21` | Merged the remote development branch. |
| 2026-09-18 | `6495e9c` | Continued the Phase 1 request-field correction. |
| 2026-09-18 | `8048b50` | Fixed backend test compilation by restoring the API project reference. |
| 2026-09-18 | `f67370a` | Added the backend refactor plan. |
| 2026-09-18 | `b844cdb` | Merged development pull request #19. |
| 2026-09-18 | `d225c6a` | Merged transfer/disposal pull request #22. |
| 2026-09-18 | `9747b4a` | Updated project progress. |
| 2026-09-18 | `4e5b756` | Merged maintenance pull request #20. |
| 2026-09-17 | `a53aa7c` | Merged Planner Agent pull request #17. |
| 2026-09-15 | `0040165` | Verified authentication. |
| 2026-09-15 | `655ed98` | Updated the agent plan and removed the budget agent. |
| 2026-09-14 | `31957b2` | Merged development pull request #15. |
| 2026-09-14 | `9853919` | Merged Planner Agent pull request #14. |
| 2026-09-14 | `8141231` | Added tests. |
| 2026-09-14 | `4ca2f62` | Recorded Component D completion progress. |
| 2026-09-14 | `a8248be` | Reverted transfer/disposal changes because of the pull-request error. |
| 2026-09-14 | `36ab263` | Merged maintenance/notification pull request #13. |
| 2026-09-14 | `2f59a37` | Merged transfer/disposal pull request #12. |
| 2026-09-12 | `aa00c9f` | Enforced the organisation query filter, automatic user provisioning and endpoint authorisation. |
| 2026-09-12 | `9f6786a` | Merged the asset-inventory-report pull request. |
| 2026-08-21 | `ab59901` | Merged development pull request #10. |
| 2026-08-21 | `a726f62` | Added CI workflow, policy management and audit agent workflows. |
| 2026-08-21 | `e02e572` | Merged transfer/disposal pull request #9. |
| 2026-08-18 | `79cd45e` | Merged maintenance-record pull request #8. |
| 2026-08-18 | `31130c6` | Updated role IDs and the SCIM client ID in settings. |
| 2026-08-18 | `bad3d62` | Updated documentation with specific role values. |
| 2026-08-17 | `b98966c` | Updated the business plan. |
| 2026-08-17 | `023c7e8` | Updated the licence and business plan. |
| 2026-08-17 | `f92e5e1` | Merged asset-history timeline pull request #7. |
| 2026-08-17 | `e43f81d` | Merged asset-history timeline pull request #6. |
| 2026-08-17 | `b923872` | Merged the asset-component improvement pull request #5. |
| 2026-08-15 | `87b2fd3` | Updated user management and role-dashboard configuration. |
| 2026-08-15 | `6658b1b` | Merged development pull request #2. |
| 2026-08-15 | `a708373` | Completed the Component D baseline, fixed pull-request issues and improved sidebar UI/features. |
| 2026-08-15 | `1a19900` | Restructured backend files. |
| 2026-08-15 | `d77c04c` | Merged asset pull request #4. |
| 2026-08-15 | `97a1440` | Merged development into the asset feature branch. |
| 2026-08-15 | `0cd342c` | Merged transfer/disposal pull request #3. |
| 2026-08-13 | `1c16300` | Updated the admin features dashboard with mock data. |
| 2026-08-12 | `eb16174` | Completed user provisioning/auth setup and mock-data dashboards. |
| 2026-08-12 | `9822fdd` | Added identity work. |
| 2026-08-12 | `ef39d19` | Restructured the frontend. |
| 2026-08-10 | `add01a4` | Added authentication work. |
| 2026-08-10 | `713e7d8` | Updated the single-tenant baseline. |
| 2026-08-09 | `5702ca1` | Added single-tenant organisation configuration. |
| 2026-08-09 | `cf43d93` | Added the initial login flow. |
| 2026-08-09 | `eff5ef2` | Revamped and separated the web frontend. |
| 2026-08-09 | `88d7d03` | Initialised the backend. |
| 2026-08-09 | `27fa3a8` | Migrated plans and identity-provider documentation to ThunderID and .NET 10. |
| 2026-08-08 | `deb1587` | Merged development pull request #1. |
| 2026-08-08 | `46dbddd` | Updated the project baseline. |
| 2026-08-08 | `d0a914a` | Initialised the frontend. |
| 2026-08-08 | `19f1b72` | Updated the team documentation. |
| 2026-08-08 | `bd03623` | Initialised the SRS. |
| 2026-08-08 | `1177d28` | Created the initial repository commit. |

### Individual Contribution Log — Seneja Thehansi

Seneja Thehansi's work covers Component B — Maintenance Management and the Maintenance Analysis Agent. The Git history uses the author name `seneja`.

| Date | Task area | Contribution summary | Commit evidence |
|---|---|---|---|
| 2026-08-17–2026-08-18 | Maintenance foundation | Implemented fault reporting, maintenance creation/approval, start/complete/cancel flows, maintenance records, DTOs, background processing, modals, hooks, seeding, LKR cost labels, error handling and maintenance-page details. | `29e2505`, `9b3f8c3`, `dd59ebb`, `de01012`, `d4b0e22`, `4f4b681`, `50de0ea`, `69ba1fc`, `8c7cccd`, `95d5e2a` |
| 2026-09-14 | Notifications and supporting integration | Implemented notification storage/service integration, the in-app notification centre and unread-count display; updated the maintenance API, fault-report page and transfer/disposal services; removed legacy transfer modules; cleaned test references. | `dac9e48`, `2f4e94e`, `58f4b58`, `db5e30d`, `a37fdd4`, `52a1169`, `99a0201`, `77abbe6`, `8c63993` |
| 2026-09-17 | Maintenance reporting and storage | Implemented preventive-maintenance logic and scheduling, maintenance reports, storage/EF migration/schema configuration, audit-report and users-controller updates, and maintenance photo/filter improvements. | `fb77167`, `65885df`, `fd7ddca`, `dc21ff8`, `b2d0260`, `1167953` |
| 2026-09-17 | Report pagination and data model | Updated the maintenance-record type and added server-side pagination for inventory and maintenance reports. | `4c1c2e4`, `f8b1bc9` |
| 2026-09-25 | Maintenance validation | Allowed future completion dates and validated them against the requested date in maintenance forms. | `8defc60` |

#### Complete Git-history contribution index — Seneja Thehansi

| Date | Commit | Recorded contribution |
|---|---|---|
| 2026-09-25 | `8defc60` | Allowed future completion dates and validated them against the requested date in maintenance forms. |
| 2026-09-17 | `f8b1bc9` | Implemented server-side pagination for inventory and maintenance reports. |
| 2026-09-17 | `4c1c2e4` | Updated the `MaintenanceRecord` type and added pagination parameters. |
| 2026-09-17 | `1167953` | Enhanced maintenance photo-upload handling and filtering. |
| 2026-09-17 | `b2d0260` | Integrated maintenance and workflow-management features. |
| 2026-09-17 | `dc21ff8` | Implemented audit reporting and updated the users controller. |
| 2026-09-17 | `fd7ddca` | Configured storage, EF migrations and database schema updates. |
| 2026-09-17 | `fb77167` | Implemented maintenance logic and preventive-maintenance scheduling. |
| 2026-09-17 | `65885df` | Added the maintenance report panel and integrated it with the Reports page. |
| 2026-09-14 | `8c63993` | Implemented the in-app notification system with CRUD operations. |
| 2026-09-14 | `77abbe6` | Removed an unused project reference from `backend.Tests.csproj`. |
| 2026-09-14 | `99a0201` | Removed legacy transfer modules and consolidated the UI. |
| 2026-09-14 | `52a1169` | Updated the maintenance API, hook and report-fault page. |
| 2026-09-14 | `a37fdd4` | Integrated the notification centre into `RoleLayout` with unread-count display. |
| 2026-09-14 | `db5e30d` | Updated disposal and transfer services and controllers. |
| 2026-09-14 | `58f4b58` | Added maintenance analysis tools and notification integration. |
| 2026-09-14 | `2f4e94e` | Implemented the Notifications table/schema and service integration. |
| 2026-09-14 | `dac9e48` | Added the Notifications table and related migrations. |
| 2026-08-18 | `95d5e2a` | Added the `MaintenanceRecords` table, indexes and migration history. |
| 2026-08-18 | `8c7cccd` | Enhanced error-message handling and maintenance-page details. |
| 2026-08-18 | `69ba1fc` | Added maintenance-record seeding and updated cost labels to LKR. |
| 2026-08-18 | `50de0ea` | Added maintenance-management modals and hooks. |
| 2026-08-18 | `4f4b681` | Added maintenance cancellation and listing with DTOs and background service. |
| 2026-08-18 | `d4b0e22` | Added maintenance start and completion with DTOs. |
| 2026-08-17 | `de01012` | Cleaned comments and formatting in `MaintenanceService`. |
| 2026-08-17 | `dd59ebb` | Implemented maintenance creation and approval endpoints. |
| 2026-08-17 | `9b3f8c3` | Implemented the fault-reporting API. |
| 2026-08-17 | `29e2505` | Added the `MaintenanceRecords` table and EF Core mappings. |

### Individual Contribution Log — Nipuna Bhanuka (Bhanuka)

Nipuna Bhanuka's work covers Component C — Transfer & Disposal and the Budget Analysis Agent. The Git history uses the author name `NipunaBhanuka18`.

| Date | Task area | Contribution summary | Commit evidence |
|---|---|---|---|
| 2026-08-14–2026-08-20 | Transfer and disposal foundation | Implemented transfer-state-machine endpoints, condemnation/disposal workflows, disposal preconditions, agent tool endpoints and the P4 maintenance precondition check. | `697c58f`, `dfded6c`, `3da7010`, `f7c3a19`, `3b7023f` |
| 2026-09-12–2026-09-13 | Component C workflow and agent | Implemented the P6 workflow precondition, standalone Budget Analysis Agent, transfer history, disposal revision, real backend wiring and the Auditor read-only view with enum corrections. | `9ce601f`, `ba4cc3e`, `8368e14`, `e600cf0`, `65c64ac` |
| 2026-09-18 | Transfer/disposal quality | Added pagination to transfers/disposals, fixed test compilation and merged development into the feature branch; migrated the Budget Analysis Agent to in-process C#. | `c92a52f`, `6be2a49`, `b6fbf83` |
| 2026-09-24 | Agent orchestration and frontend quality | Wired the Budget Analysis Agent into the multi-agent pipeline and fixed a frontend build-breaking duplicate property and stale test assertion. | `d44a11b`, `4793dc0` |

#### Complete Git-history contribution index — Nipuna Bhanuka (Bhanuka)

| Date | Commit | Recorded contribution |
|---|---|---|
| 2026-09-24 | `4793dc0` | Wired the Budget Analysis Agent into the multi-agent orchestration pipeline (Node 3). |
| 2026-09-24 | `d44a11b` | Fixed a duplicate frontend property and stale `WorkflowsPage` test assertion. |
| 2026-09-18 | `b6fbf83` | Migrated the Budget Analysis Agent to in-process C#. |
| 2026-09-18 | `6be2a49` | Merged the development branch into the transfer/disposal feature branch. |
| 2026-09-18 | `c92a52f` | Fixed test compilation and added transfer/disposal pagination. |
| 2026-09-13 | `65c64ac` | Added the Auditor read-only view and corrected enumeration behaviour. |
| 2026-09-13 | `e600cf0` | Wired the transfers/disposals frontend to the real backend. |
| 2026-09-12 | `8368e14` | Implemented disposal revision and transfer history. |
| 2026-09-12 | `ba4cc3e` | Added the standalone Budget Analysis Agent using Python/LangGraph. |
| 2026-09-12 | `9ce601f` | Implemented the P6 agent-workflow precondition. |
| 2026-08-20 | `3b7023f` | Completed the P4 maintenance-precondition check. |
| 2026-08-19 | `aed2ad0` | Merged development into the transfer/disposal feature branch. |
| 2026-08-19 | `f7c3a19` | Added agent-tool endpoints for the Budget Analysis Agent. |
| 2026-08-18 | `3da7010` | Implemented the condemnation and disposal workflow. |
| 2026-08-18 | `dfded6c` | Implemented transfer state-machine endpoints. |
| 2026-08-18 | `1cc6433` | Implemented the disposal precondition engine. |
| 2026-08-14 | `697c58f` | Added `AssetTransfer` and `DisposalRequest` entities and migration. |

### Individual Contribution Log — Jayashan Guruge

This log groups related work attributed to Jayashan Guruge into task areas. The summaries are based on repository commit history; abbreviated commit IDs provide evidence to review against the associated changes. Add requirement references and verification results in the individual report.

| Date | Task area | Contribution summary | Commit evidence |
|---|---|---|---|
| 2026-08-15–2026-08-17 | Asset Registry and configuration | Implemented Component A asset registry and QR identification work, including registration and update flows, LKR display, QR payload, searchable asset configuration, safe soft deletion, and asset history/timeline. | `d15d89b`, `01e214a`, `f14e6b9`, `5b39b92`, `f942d6c`, `257db73`, `d3ac751`, `32a7644`, `0f68605`, `156b292`, `e4e88b2`, `25f2f6d`, `37007c4`, `31ecaa4`, `608bade`, `b1e5b2b` |
| 2026-08-17–2026-09-27 | Project and assignment documentation | Updated progress records, documented role/platform scenarios, added the assignment report structure, and revised project documentation. | `222aa15`, `549e58f`, `ba95423`, `187ed97`, `885168d` |
| 2026-09-10 | Asset inventory reporting | Connected the asset inventory report to the backend and added filters and exports. | `bc168d7`, `699b96b` |
| 2026-09-14–2026-09-16 | Planner Agent and verification | Integrated and migrated the Planner Agent workflow, added asset-attribute validation, and generated campaign verification tasks. | `6812cc5`, `fb94a30`, `426c9eb`, `8496892` |
| 2026-09-17 | Asset lifecycle features | Implemented server-side asset depreciation and printable QR label downloads. | `4ad9977`, `ae49c1a` |
| 2026-09-22–2026-09-25 | Interface, maintenance and quality fixes | Refined asset filters and category UI, added the user profile page and reporter attribution, implemented current-user fault-report retrieval, and addressed CI, view, issue and code-comment fixes. | `2d50677`, `ae9f8c6`, `61ed61e`, `d3c1f0f`, `6cdc2f7`, `7f258ee`, `1433467`, `ff0ed19`, `9143da2`, `603b827`, `ff78bff` |

**Evidence basis:** The task summaries group non-merge commits attributed to Jayashan in `git log --all`. Commit IDs identify the underlying records; review the associated code or documentation changes and test results when preparing assessed evidence.

#### Mobile — `CoreGrid-org/coregrid-mobile`

The following mobile work is evidenced by the supplied commit and pull-request links for the separate mobile repository.

##### Individual Contribution Log — Mobile (Jayashan)

| Date | Task / area | Contribution summary | Commit | Evidence |
|---|---|---|---|---|
| 2026-09-09 | Asset registry, search, condition, verification | Implemented FR-020, FR-028, FR-029 and FR-031 asset-registry functionality. | `ee8003f` | [Commit](https://github.com/CoreGrid-org/coregrid-mobile/commit/ee8003f) |
| 2026-09-09 | Progress documentation | Updated mobile progress tracking. | `3efcf76` | [Commit](https://github.com/CoreGrid-org/coregrid-mobile/commit/3efcf76) |
| 2026-09-15 | Asset search | Connected asset search to the backend. | `6f300b4` | [Commit](https://github.com/CoreGrid-org/coregrid-mobile/commit/6f300b4) |
| 2026-09-15 | Asset UI/UX | Improved asset-related interface and experience. | `dc792b6` | [Commit](https://github.com/CoreGrid-org/coregrid-mobile/commit/dc792b6) |
| 2026-09-16 | Asset verification and discrepancy | Implemented the mobile asset-verification workflow and improved asset/discrepancy UI. | `0bd5ae8` | [Commit](https://github.com/CoreGrid-org/coregrid-mobile/commit/0bd5ae8) |
| 2026-09-16 | QR asset scanning | Added the QR asset-scanning flow. | `878282c` | [Commit](https://github.com/CoreGrid-org/coregrid-mobile/commit/878282c) |
| 2026-09-25 | Dashboard UI | Fixed staff-dashboard UI/UX. | `0608a65` | [Commit](https://github.com/CoreGrid-org/coregrid-mobile/commit/0608a65) |
| 2026-09-25 | Fault reporting | Added the report-fault feature and related UI improvements. | `a607e9c` | [Commit](https://github.com/CoreGrid-org/coregrid-mobile/commit/a607e9c) |
| 2026-09-25 | Code comments | Added explanatory comments to the relevant implementation. | `abb6ad0` | [Commit](https://github.com/CoreGrid-org/coregrid-mobile/commit/abb6ad0) |
| 2026-09-25 | Ad-hoc verification | Enabled asset verification when no pending campaign task exists. | `6fafa96` | [Commit](https://github.com/CoreGrid-org/coregrid-mobile/commit/6fafa96) |

##### Merged mobile pull requests

| Merged date | PR | Source branch | Merge commit | Link |
|---|---|---|---|---|
| 2026-09-09 | #1 — asset registry QR | `feature/asset-registry-qr` | `52d91b1` | [Pull request](https://github.com/CoreGrid-org/coregrid-mobile/pull/1) |
| 2026-09-16 | #3 — asset feature | `feature/asset` | `9a4c2ba` | [Pull request](https://github.com/CoreGrid-org/coregrid-mobile/pull/3) |
| 2026-09-16 | #4 — asset feature | `feature/asset` | `9bff166` | [Pull request](https://github.com/CoreGrid-org/coregrid-mobile/pull/4) |
| 2026-09-25 | #6 — frontend issues | `fix/frontend-issues` | `7d649da` | [Pull request](https://github.com/CoreGrid-org/coregrid-mobile/pull/6) |
| 2026-09-25 | #7 — asset feature fixes | `fix/asset_feature` | `2eb218d` | [Pull request](https://github.com/CoreGrid-org/coregrid-mobile/pull/7) |

The complete supplied mobile PR list, including reported PRs #8–#11 and its reconciliation with the checked
local Git history, is maintained in [`docs/contribution-history.md`](../contribution-history.md). The supplied
list reports #9 as transfer/receipt, #10 as FR-049 condemnation, and #11 as maintenance/notification; local
merge-commit numbering differs for some of these entries. Use the GitHub PR pages as the final PR evidence.

### Pull Requests Submitted — Jayashan Guruge

The following submitted PRs are recorded as merged in repository history.

| Merged date | PR | Source branch | Merge commit | Link |
|---|---:|---|---|---|
| 2026-09-15 | #16 | `feature/component-a-enhancements` | `074df98` | [PR #16](https://github.com/CoreGrid-org/CoreGrid/pull/16) |
| 2026-09-16 | #18 | `feature/campaign-verification-task-generation` | `3d05787` | [PR #18](https://github.com/CoreGrid-org/CoreGrid/pull/18) |
| 2026-09-17 | #21 | `feature/component-a-enhancements` | `3e3c2c4` | [PR #21](https://github.com/CoreGrid-org/CoreGrid/pull/21) |
| 2026-09-18 | #23 | `feature/component-a-enhancements` | `a9b16c6` | [PR #23](https://github.com/CoreGrid-org/CoreGrid/pull/23) |
| 2026-09-22 | #25 | `fixComments` | `2508fc0` | [PR #25](https://github.com/CoreGrid-org/CoreGrid/pull/25) |
| 2026-09-22 | #26 | `fixComments` | `24e8f07` | [PR #26](https://github.com/CoreGrid-org/CoreGrid/pull/26) |
| 2026-09-24 | #27 | `feature/component-a-enhancements` | `e6ce874` | [PR #27](https://github.com/CoreGrid-org/CoreGrid/pull/27) |
| 2026-09-24 | #28 | `feature/component-a-enhancements` | `63e9ba2` | [PR #28](https://github.com/CoreGrid-org/CoreGrid/pull/28) |
| 2026-09-25 | #30 | `feature/component-a-enhancements` | `7e833cb` | [PR #30](https://github.com/CoreGrid-org/CoreGrid/pull/30) |
| 2026-09-25 | #31 | `feature/component-a-enhancements` | `2fa96ff` | [PR #31](https://github.com/CoreGrid-org/CoreGrid/pull/31) |

**Commit and PR summary:** The repository history attributes 40 non-merge commits to Jayashan Guruge. Together, they document work on asset registration and QR identification, asset lifecycle and inventory reporting, Planner Agent integration and campaign verification, plus project documentation and interface/maintenance fixes. The history records 10 merged PRs: #16, #18, #21, #23, #25, #26, #27, #28, #30 and #31. Two additional merge commits synchronize development branches and are not counted as PRs.

### Approval

| Role | Name | Responsibility | Signature / Date |
|---|---|---|---|
| Group Leader | Hasitha Erandika | Owns the consolidated submission, baseline control and evaluator access. | |
| Component A Owner | Jayashan Guruge | Asset Registry & QR Identification; Planner Agent. | |
| Component B Owner | Seneja Ramanayaka | Maintenance Management; Maintenance Analysis Agent. | |
| Component C Owner | Bhanuka Samarasinghe | Transfer & Disposal; Budget Analysis Agent. | |
| Component D Owner | Hasitha Erandika | Audit & Compliance; Policy Agent and human-approval checkpoint. | |
| Lecturer-in-Charge | <Name> | Scope confirmation and any approved variation to group size or agent count. | |

### Purpose of Baselining

Version 1.0 of this Software Requirements Specification is the development contract for the CoreGrid implementation. Every artefact produced during the project — the database schema, the ASP.NET Core API surface, the React and Flutter screens, the agent node and orchestrator definitions, the automated test suite and the consolidated report — traces back to a requirement identifier in this document. Any change requested after baselining must be raised as a GitHub issue labelled "scope-change", assessed against the seven-week implementation schedule, approved by the group and recorded in the revision history above before work begins.

### Relationship to the SE3090 assignment specification

This SRS is written to satisfy the SE3090 Assignment 1 specification (release 31 July 2026). Section 16 provides an explicit traceability matrix from the assignment's marking rubric to the sections of this document, so that an evaluator can confirm coverage of the integrated-system rule, the minimum agentic-AI acceptance workflow, the minimum domain complexity, and the individual-contribution requirements without reading the implementation.

## Table of Contents

If the entries below do not appear, select the field and press F9 (Word) or use References → Update Table to populate the contents.

1. [Introduction](01-introduction.md)
2. [Overall Description](02-overall-description.md)
3. [System Architecture](03-system-architecture.md)
4. [Identity and Access Management with ThunderID](04-identity-and-access-management.md)
5. [External Interface Requirements](05-external-interface-requirements.md)
6. [Functional Requirements](06-functional-requirements.md)
7. [Agentic AI Subsystem Requirements](07-agentic-ai-subsystem-requirements.md)
8. [Data Requirements](08-data-requirements.md)
9. [API Specification Summary](09-api-specification-summary.md)
10. [Non-Functional Requirements](10-non-functional-requirements.md)
11. [Third-Party Integration](11-third-party-integration.md)
12. [Individual Contribution and Work Allocation](12-individual-contribution-and-work-allocation.md)
13. [Verification and Validation](13-verification-and-validation.md)
14. [Deployment and Operations](14-deployment-and-operations.md)
15. [Risks and Descope Order](15-risks-and-descope-order.md)
16. [Traceability](16-traceability.md)
17. [Future Enhancements](17-future-enhancements.md)
18. [Team Roster and Individual Work Allocation](18-team-roster-and-work-allocation.md)
19. [Business Plan](19-business-plan.md)
- [Appendix A — Status and Enumeration Reference](appendix-a-status-and-enumeration-reference.md)
- [Appendix B — Route-Level Authorisation Map](appendix-b-route-level-authorisation-map.md)
- [Appendix C — ThunderID Configuration Checklist](appendix-c-thunderid-configuration-checklist.md)
- [Appendix D — Architecture Decision Record Index](appendix-d-architecture-decision-record-index.md)
- [Appendix E — AI Usage Disclosure](appendix-e-ai-usage-disclosure.md)
- [Appendix F — Full Physical Database Schema (Reference Design)](appendix-f-physical-database-schema.md)
