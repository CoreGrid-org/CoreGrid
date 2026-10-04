# SE3090 Rubric → CoreGrid Specification

Moved from SRS §16.2 (SRS v1.8). Maps each SE3090 Assignment 1 marking criterion to the SRS sections that specify it and to the evidence shown at evaluation. Section numbers refer to `docs/srs/`. The old "Section 12 allocation" now lives in [`team/`](team/) and SRS §12 (Component Ownership). "Appendix E" in the table below meant the AI-usage disclosure, which is now [`ai-usage-disclosure.md`](ai-usage-disclosure.md).

## Rubric mapping

| Rubric criterion (marks) | Where specified | Evidence at evaluation |
|---|---|---|
| Component Design and Business Logic (10) | Section 6 in full; state machines in Figures 6 and 7; business-specific operations FR-031, FR-038, FR-051, FR-062. | All four components functional; guarded transitions demonstrated; the golden workflow completes end to end. |
| Integrated Architecture, Agent Orchestration and State Management (10) | Sections 3.1 to 3.3, Section 7 in full, Section 8.4. | Four distinct agents, persisted state inspected live, allow-listed tools, deterministic validation, safe failure, authorised approval. |
| Documentation and Deployment (10) | Sections 2.6, 14, Appendix E; this SRS. | Consolidated report, ADRs, AI usage logs, live URLs, health and Swagger, working APK, reproducible setup. |
| ASP.NET Core RESTful API Development (10) | Sections 3.2, 5.4, 9, 10.2. | DTOs, validation, policy authorisation, async operations, correct status codes, global exception handling, Swagger. |
| PostgreSQL Integration and Data Modelling (10) | Section 8 in full; DR-01 to DR-15. | ER diagram, constraints, indexes, migrations, seed data, transactions, concurrency conflict demonstrated. |
| React Web Application (10) | Sections 3.4, 5.1, FR-010 to FR-020, FR-081 to FR-086, FR-070 to FR-075. | Reusable components, routing, state management, protected routes, validation, loading and error states, approval panel. |
| Flutter Mobile Application (10) | Sections 3.4, 5.2, FR-024, FR-031, FR-033, FR-046, FR-059, FR-076. | Widgets, routing, Riverpod, secure token storage, QR scanning as the meaningful device feature, field workflows. |
| Individual Agentic AI Contribution (12) | Section 7.3 agent specifications; Section 12 allocation. | Each owner explains their agent's contract, tools, state, validation and approval interaction, and modifies it on request. |
| API Integration, Security and Cross-Platform Functionality (10) | Sections 4, 5.3, 5.4, 10.2; FR-001 to FR-009. | Both clients on one API and one identity; token handling; role denial demonstrated; the cross-platform loop traced. |
| Testing, CI and Git Workflow (8) | Section 13 in full; Section 12.1. | Passing CI run, test suite across all layers, golden cases, performance report, reviewed pull requests per owner. |



## SE3090 §9.1 minimum acceptance rule → CoreGrid (moved from SRS §7.11)

| Required element (SE3090 §9.1) | Where satisfied | Demonstrable evidence |
|---|---|---|
| Receives a domain objective | FR-067; workflow entry | Officer initiates the evaluation from Flutter with a stated objective. |
| Creates a structured multi-step plan | Planner Agent, §7.3 | Plan array persisted in `AgentWorkflows.Plan` and rendered in the React execution summary. |
| Delegates steps to distinct agent roles | §7.3, four agents with different contracts and tool sets | `AgentExecutionSteps` shows four separate executions with different inputs, outputs and tools. |
| Calls allow-listed tools with validated inputs and structured outputs | §7.4, AI-01 to AI-07 | `ToolCalls` trace; negative test showing an out-of-allow-list call refused. |
| Persists workflow state | §7.5, AI-08 to AI-12 | Database inspection during the demonstration; resumption after restart. |
| Applies deterministic checks | §7.6, three-stage gate and rules PR-01 to PR-09 | `ValidationResult` showing every rule with expected, actual and outcome. |
| Pauses a high-impact action for authorised approval | §7.7, AI-13 to AI-20; FR-071, FR-072 | AWAITING_APPROVAL state; Administrator decides in React; non-administrator receives 403. |
| Produces an auditable result or a safe recorded failure | §7.8, §7.10 | Execution summary for the success path; FAILED_SAFE golden cases for the failure paths. |
| Cross-platform end-to-end journey | FR-067 to FR-076 | Flutter initiates → API → PostgreSQL → agents → React approval → API executes → Flutter shows updated status. |
