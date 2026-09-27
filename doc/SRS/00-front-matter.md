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
| Student 1 | Jayashan Guruge | `<ID>` | `<github-handle>` | `<email>` | A — Asset Registry & QR Identification | Member |
| Student 2 | Seneja Ramanayaka | `<ID>` | `<github-handle>` | `<email>` | B — Maintenance Management | Member |
| Student 3 | Bhanuka Samarasinghe | `<ID>` | `<github-handle>` | `<email>` | C — Transfer & Disposal | Member |
| Student 4 | Hasitha Erandika | `<ID>` | `<github-handle>` | `<email>` | D — Audit & Compliance, org configuration, user administration | Group Leader |

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

### Individual Contribution Log — Jayashan Guruge

This log groups related work attributed to Jayashan Guruge into task areas. The task descriptions are summarized from the repository history; commit IDs provide evidence that can be checked against the code and documentation changes. Add requirement references and verification results in the individual report.

| Date | Task area | Contribution summary | Commit evidence |
|---|---|---|---|
| 2026-08-15–2026-08-17 | Asset Registry and configuration | Implemented Component A asset registry and QR identification work, including registration and update flows, LKR display, QR payload, searchable asset configuration, safe soft deletion, and asset history/timeline. | `d15d89b`, `01e214a`, `f14e6b9`, `5b39b92`, `f942d6c`, `257db73`, `d3ac751`, `32a7644`, `0f68605`, `156b292`, `e4e88b2`, `25f2f6d`, `37007c4`, `31ecaa4`, `608bade`, `b1e5b2b` |
| 2026-08-17–2026-09-18 | Project and assignment documentation | Updated progress records, documented role/platform scenarios, and added the assignment report structure. | `222aa15`, `549e58f`, `ba95423`, `187ed97` |
| 2026-09-10 | Asset inventory reporting | Connected the asset inventory report to the backend and added filters and exports. | `bc168d7`, `699b96b` |
| 2026-09-14–2026-09-16 | Planner Agent and verification | Integrated and migrated the Planner Agent workflow, added asset-attribute validation, and generated campaign verification tasks. | `6812cc5`, `fb94a30`, `426c9eb`, `8496892` |
| 2026-09-17 | Asset lifecycle features | Implemented server-side asset depreciation and printable QR label downloads. | `4ad9977`, `ae49c1a` |
| 2026-09-22–2026-09-25 | Interface, maintenance and quality fixes | Refined asset filters and category UI, added the user profile page and reporter attribution, implemented current-user fault-report retrieval, and addressed CI, view, issue and code-comment fixes. | `2d50677`, `ae9f8c6`, `61ed61e`, `d3c1f0f`, `6cdc2f7`, `7f258ee`, `1433467`, `ff0ed19`, `9143da2`, `603b827`, `ff78bff` |

#### Mobile

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

##### Merged pull requests

| Merged date | PR | Source branch | Merge commit | Link |
|---|---|---|---|---|
| 2026-09-09 | #1 — asset registry QR | `feature/asset-registry-qr` | `52d91b1` | [Pull request](https://github.com/CoreGrid-org/coregrid-mobile/pull/1) |
| 2026-09-16 | #3 — asset feature | `feature/asset` | `9a4c2ba` | [Pull request](https://github.com/CoreGrid-org/coregrid-mobile/pull/3) |
| 2026-09-16 | #4 — asset feature | `feature/asset` | `9bff166` | [Pull request](https://github.com/CoreGrid-org/coregrid-mobile/pull/4) |
| 2026-09-25 | #6 — frontend issues | `fix/frontend-issues` | `7d649da` | [Pull request](https://github.com/CoreGrid-org/coregrid-mobile/pull/6) |
| 2026-09-25 | #7 — asset feature fixes | `fix/asset_feature` | `2eb218d` | [Pull request](https://github.com/CoreGrid-org/coregrid-mobile/pull/7) |

**Evidence basis:** Commit IDs are from non-merge commits in `git log --all` attributed to Jayashan. The summaries describe commit subjects and should be checked against the associated changes; they do not by themselves establish test coverage or requirement acceptance.

### Pull Requests Submitted — Jayashan Guruge

The following PRs were submitted by Jayashan Guruge and are recorded as merged in the repository history.

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
- [Appendix F — Full Physical Database Schema (Reference Design)](system.md)
