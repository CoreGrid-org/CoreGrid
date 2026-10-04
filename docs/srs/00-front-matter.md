# SOFTWARE REQUIREMENTS SPECIFICATION
## CoreGrid
### A Configurable, Agentic-AI-Assisted Asset Lifecycle Management Platform

**Version 1.8  |  Baseline Release**

Prepared in accordance with IEEE 830 / ISO-IEC-IEEE 29148 requirements-specification practice

| Item | Detail |
|---|---|
| Product name | CoreGrid — Intelligent Asset Lifecycle Management Platform |
| Document type | Software Requirements Specification (SRS) |
| License | Apache License 2.0 — open source |
| Deployment model | Self-hosted, single-tenant: one deployment per customer organisation (Sections 2.4, 4.2) |
| Maintainers | See Section 12, Component Ownership |
| Identity provider | ThunderID (OIDC / OAuth 2.0); organisation scoping is done in CoreGrid's own database, not ThunderID (Section 4.2) |
| Technology stack | ASP.NET Core Web API · PostgreSQL · React (IBM Carbon Design System) · Flutter · Agentic AI (in-process .NET orchestrator and four agent services, Section 7.2.1, ADR-010) |
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
| 1.8 | 2026-10-04 | Hasitha Erandika | Separated the product specification from academic coursework material. Section 12 is now Component Ownership and Section 18 is now Development Workflow and Change Control. The team roster, contribution logs, AI-usage disclosure, coursework rubric mapping and evidence documents moved to `docs/coursework/`. The physical database schema appendix was relettered from F to E. ADR-003 was corrected to match the implemented React state approach, and ADR-004 (Riverpod) was added. | Baselined |

### Approval

| Role | Name | Responsibility | Signature / Date |
|---|---|---|---|
| Project lead | Hasitha Erandika | Baseline control, shared platform, release management. | |
| Component A maintainer | Jayashan Guruge | Asset Registry & QR Identification; Planner Agent. | |
| Component B maintainer | Seneja | Maintenance Management; Maintenance Analysis Agent. | |
| Component C maintainer | Nipuna Bhanuka | Transfer & Disposal; Budget Analysis Agent. | |
| Component D maintainer | Hasitha Erandika | Audit & Compliance, organisation configuration, user administration; Policy Compliance Agent and human-approval checkpoint. | |

### Purpose of Baselining

Version 1.0 of this Software Requirements Specification is the development contract for the CoreGrid implementation. Every artefact produced during the project — the database schema, the ASP.NET Core API surface, the React and Flutter screens, the agent node and orchestrator definitions and the automated test suite — traces back to a requirement identifier in this document. Changes after baselining follow the scope-change process in Section 18.5.

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
12. [Component Ownership](12-component-ownership.md)
13. [Verification and Validation](13-verification-and-validation.md)
14. [Deployment and Operations](14-deployment-and-operations.md)
15. [Risks and Descope Order](15-risks-and-descope-order.md)
16. [Traceability](16-traceability.md)
17. [Future Enhancements](17-future-enhancements.md)
18. [Development Workflow and Change Control](18-development-workflow-and-change-control.md)
19. [Business Plan](19-business-plan.md)
- [Appendix A — Status and Enumeration Reference](appendix-a-status-and-enumeration-reference.md)
- [Appendix B — Route-Level Authorisation Map](appendix-b-route-level-authorisation-map.md)
- [Appendix C — ThunderID Configuration Checklist](appendix-c-thunderid-configuration-checklist.md)
- [Appendix D — Architecture Decision Record Index](appendix-d-architecture-decision-record-index.md)
- [Appendix E — Full Physical Database Schema (Reference Design)](appendix-e-physical-database-schema.md)
