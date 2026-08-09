# 1. Introduction

## 1.1 Purpose

This document specifies the complete functional and non-functional requirements of CoreGrid, a configurable asset lifecycle management platform that enables an organisation to register, identify, maintain, transfer, verify, audit and dispose of physical assets, and that uses a controlled agentic-AI workflow to support complex lifecycle decisions under authorised human approval.

The specification defines what CoreGrid must do and the quality attributes it must exhibit; it deliberately does not prescribe implementation detail beyond the constraints imposed by the mandated technology stack and by the identity architecture. It is intended to be sufficient for a four-person engineering team to implement, test, deploy and defend the system, and sufficient for an independent evaluator to verify that the delivered software satisfies the stated requirements.

## 1.2 Document Conventions

- Functional requirements are identified as FR-nnn and are grouped by business component. Non-functional requirements are identified as NFR-nn. Agentic-AI requirements carry the prefix AI-nn. Interface requirements carry the prefix IF-nn. Data requirements carry the prefix DR-nn.
- The key words shall, should and may are used in the RFC 2119 sense. "Shall" denotes a mandatory requirement whose absence constitutes a defect. "Should" denotes a strongly recommended requirement that may be traded off against schedule with recorded justification. "May" denotes an optional capability.
- Every requirement carries a priority: Must (required for the baseline release and for the assignment demonstration), Should (planned for the baseline release but may be descoped with a recorded decision), or Could (deferred to the future-enhancement roadmap in Section 17).
- Identifiers written in a monospaced font — for example `/api/assets/{id}/verify` or `org_id` — denote literal API routes, database columns, token claims or configuration keys.
- Where a requirement is satisfied differently by the web client and the mobile client, the responsible client is stated explicitly. Where a requirement is enforced by the backend regardless of client, it is marked "API".

## 1.3 Intended Audience and Reading Suggestions

| Audience | Purpose | Suggested reading order |
|---|---|---|
| Development team (four component owners) | Implementation contract; defines the scope each owner is accountable for. | Sections 3, 4, 6, 7, 8, 9, then the traceability matrices in Section 16. |
| Module evaluator / lecturer-in-charge | Verification that the system meets the assignment specification and the marking rubric. | Sections 1.4, 2.8, 3, 7, 16.2, then Section 13 (verification) and Section 14 (deployment). |
| Test engineer role (shared across owners) | Derivation of test cases and acceptance evidence. | Sections 6, 7, 10, 13 and the FR-to-test traceability table in Section 16.3. |
| Prospective institutional stakeholder | Understanding of the business problem, scope boundary and roadmap. | Sections 1.4, 2, 2.8, 17. |
| Security and identity reviewer | Assessment of the ThunderID-based identity architecture and access controls. | Sections 4, 5.3, 10.2, Appendix B and Appendix D. |

## 1.4 Product Scope

CoreGrid addresses a documented operational problem. Institutions that hold large populations of non-financial physical assets — vehicles, machinery, medical devices, workshop equipment, IT equipment, furniture and buildings — typically track them in disconnected spreadsheets and paper ledgers held by individual departments. Physical verification is performed manually and infrequently, condition data is stale by the time it reaches a decision maker, maintenance is reactive rather than scheduled, and the decision to repair, transfer, condemn or dispose of an asset is taken without a consistent, evidenced comparison of residual value against projected repair cost. The consequence is asset registers that do not reconcile with physical reality, avoidable expenditure on equipment that should have been replaced, and audit findings that persist unresolved from one reporting period to the next.

CoreGrid replaces that fragmented process with a single, role-controlled digital platform built around three deliberate design commitments.

- First, identification is physical. Every asset carries a QR label; a field officer scans it and immediately sees the authoritative record, its maintenance history and the actions available to them, so that verification happens where the asset is rather than at a desk afterwards.
- Second, the platform is domain-configurable rather than domain-hardcoded. Asset categories, asset types, the custom attributes each type requires, departments, locations and selected workflow behaviour are configuration, not code. The same deployment therefore serves a transport fleet, a hospital biomedical inventory or a railway rolling-stock register without a new build.
- Third, intelligence is advisory and auditable. A multi-agent workflow assembles the evidence for a lifecycle decision, produces a structured recommendation and explains the factors behind it — but it never changes business state on its own. Deterministic rules validate the recommendation, and a defined high-impact action pauses until an authorised human approves, rejects or requests revision.

The initial release configures and demonstrates a single departmental domain end to end, while proving through configuration that additional domains require no application code. The commercial extension of the platform — multi-tenant SaaS delivery, enterprise resource-planning integration and sovereign-cloud hosting — is described in the roadmap in Section 17 and is explicitly outside the baseline scope.

## 1.5 Definitions, Acronyms and Abbreviations

| Term | Definition |
|---|---|
| Agent | A component of the agentic-AI subsystem with an identifiable responsibility, a defined input and output contract, an explicit allow-list of tools it may call, and visible participation in the workflow graph. |
| Agentic workflow | A stateful, multi-step execution graph that receives a domain objective, produces a plan, delegates steps to distinct agents, calls controlled tools, validates results deterministically and pauses for human approval before a high-impact action. |
| ThunderID | ThunderID — the cloud identity-as-a-service provider used by CoreGrid for authentication, user management and organisation modelling. |
| Asset | A uniquely identified physical item under lifecycle management, owned by a department and located at a location. |
| Asset type | A configurable classification (for example Bus, MRI Machine, Locomotive) that determines which custom attributes an asset of that type must carry. |
| Attribute definition | A configurable field declaration attached to an asset type, specifying name, data type, required flag, validation rule and display order. |
| Board of Survey | A periodic independent physical verification exercise in which a committee confirms the existence, location and condition of recorded assets and reports discrepancies. |
| Condemnation | The formal declaration that an asset is no longer serviceable and is a candidate for disposal. |
| Custom attribute value | The value an individual asset holds for an attribute definition belonging to its asset type. |
| Discrepancy | A recorded difference between the register and physical reality: missing, surplus, location mismatch, condition mismatch or data mismatch. |
| Golden case | A fixed, versioned test scenario with a known-correct expected outcome, used to evaluate the agentic workflow deterministically. |
| HITL | Human-in-the-loop: the mandatory pause point at which an authorised user approves, rejects or requests revision of an agent recommendation. |
| IdP | Identity provider. In CoreGrid this is ThunderID. |
| JWKS | JSON Web Key Set — the public keys published by the IdP and used by the API to verify token signatures. |
| LangGraph | The Python framework used to express the agentic workflow as an explicit directed graph with persisted state and interrupt points. |
| Organisation | The top-level tenant of a CoreGrid deployment. Modelled in ThunderID as an organisation and mirrored locally; every user, department and asset belongs to exactly one organisation. |
| PKCE | Proof Key for Code Exchange — the OAuth 2.0 extension required for public clients (the React SPA and the Flutter application). |
| Residual value | The current book value of an asset after depreciation, used as one input to the repair-versus-replace decision. |
| Safe failure | A terminal workflow state in which the agentic subsystem has failed but has recorded the failure, changed no business state, and surfaced the cause to the operator. |
| SCIM 2.0 | System for Cross-domain Identity Management — the standard REST interface used to read and provision users in ThunderID. |
| Sub-organisation | An ThunderID organisation nested beneath the root organisation, used by CoreGrid to isolate the users of one tenant institution. |
| Tool | A named, schema-validated function an agent is permitted to invoke. Tools are the only mechanism by which an agent may read or compute over system data. |
| Workflow state | The durable record of a workflow run: identifier, objective, plan, completed steps, tool results, validation results, errors, approval status and final outcome. |

## 1.6 References

| Ref | Source |
|---|---|
| R1 | SE3090 — Software Engineering Frameworks, Assignment 1 Specification, Year 3 Semester 1, 2026. SLIIT Faculty of Computing, Department of Software Engineering. |
| R2 | CoreGrid Strategic Architecture and Feasibility Report — Intelligent Public Sector Asset Lifecycle Management System (internal project document). |
| R3 | CoreGrid Delivery Plan — seven-week implementation plus one-week stabilisation schedule (internal project document). |
| R4 | CoreGrid Application Boundary Analysis — React management layer versus Flutter field layer (internal project document). |
| R5 | CoreGrid Platform Configurability Analysis — configurable asset types, attributes and workflows (internal project document). |
| R6 | ISO/IEC/IEEE 29148:2018 — Systems and software engineering: life-cycle processes, requirements engineering. |
| R7 | IEEE Std 830-1998 — Recommended Practice for Software Requirements Specifications (structural guidance). |
| R8 | OpenID Connect Core 1.0 and OAuth 2.0 (RFC 6749), OAuth 2.0 for Native Apps (RFC 8252), PKCE (RFC 7636), JSON Web Token (RFC 7519). |
| R9 | ThunderID product documentation — organisations, application onboarding, roles and SCIM 2.0 user management. |
| R10 | OWASP Application Security Verification Standard and OWASP Top 10 for Large Language Model Applications. |
| R11 | Personal Data Protection Act No. 9 of 2022 (Sri Lanka) — obligations relevant to the processing of user personal data. |
| R12 | Perkins, Furze, Roe & MacVaugh (2024) — The AI Assessment Scale and the CLEAR Framework, as applied by the SE3090 module. |
