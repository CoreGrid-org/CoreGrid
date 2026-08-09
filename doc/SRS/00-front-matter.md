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
| Team members | Hasitha Erandika — Component D (Group Leader)<br>Jayashan Guruge — Component A<br>Seneja Ramanayaka — Component B<br>Bhanuka Samarasinghe — Component C |
| Identity provider | ThunderID (OIDC / OAuth 2.0, organisation-scoped users) |
| Mandatory stack | ASP.NET Core Web API · PostgreSQL · React (IBM Carbon Design System) · Flutter · Agentic AI (LangGraph) |
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

Version 1.0 of this Software Requirements Specification is the development contract for the CoreGrid implementation. Every artefact produced during the project — the database schema, the ASP.NET Core API surface, the React and Flutter screens, the LangGraph agent definitions, the automated test suite and the consolidated report — traces back to a requirement identifier in this document. Any change requested after baselining must be raised as a GitHub issue labelled "scope-change", assessed against the seven-week implementation schedule, approved by the group and recorded in the revision history above before work begins.

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
- [Appendix A — Status and Enumeration Reference](appendix-a-status-and-enumeration-reference.md)
- [Appendix B — Route-Level Authorisation Map](appendix-b-route-level-authorisation-map.md)
- [Appendix C — ThunderID Configuration Checklist](appendix-c-thunderid-configuration-checklist.md)
- [Appendix D — Architecture Decision Record Index](appendix-d-architecture-decision-record-index.md)
- [Appendix E — AI Usage Disclosure](appendix-e-ai-usage-disclosure.md)
