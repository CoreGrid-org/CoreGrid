# 10. Non-Functional Requirements

Non-functional requirements are stated so that each is verifiable. Where a threshold is given it is measurable by the performance or security test described in Section 13; where a property is structural it is verifiable by inspection or by an automated test.

## 10.1 Performance

| ID | Requirement | Priority |
|---|---|---|
| NFR-01 | Ninety-five per cent of read requests returning a single resource shall complete within 500 ms server-side, measured at the API excluding client rendering and network latency. | Must |
| NFR-02 | Ninety-five per cent of paginated list requests of up to fifty items shall complete within 800 ms server-side with the seeded dataset. | Must |
| NFR-03 | A QR code lookup shall resolve within 1 second server-side, and the mobile client shall render the asset detail within 3 seconds of a successful decode under normal network conditions. | Must |
| NFR-04 | The system shall sustain 50 concurrent authenticated users performing mixed read and write operations with a success rate of at least 99% and no deadlocks. | Must |
| NFR-05 | A complete agentic workflow run, excluding human approval time, shall complete within 60 seconds at the median and shall be hard-bounded at 120 seconds. | Must |
| NFR-06 | Report generation over the seeded dataset shall complete within 5 seconds; export shall stream rather than buffer the entire result in memory. | Should |
| NFR-07 | Every list query shall be paginated at the database level; no endpoint shall load an unbounded result set into memory. | Must |
| NFR-08 | The React initial bundle shall be code-split by route so that first meaningful paint does not require loading administrative or reporting modules. | Should |

## 10.2 Security

| ID | Requirement | Priority |
|---|---|---|
| NFR-09 | All traffic shall use HTTPS with TLS 1.2 or above; HTTP shall be redirected and HSTS enabled. | Must |
| NFR-10 | Every endpoint except `/health` and `/swagger` shall require authentication and shall declare an explicit authorisation policy; there shall be no endpoint whose protection depends only on the client not calling it. | Must |
| NFR-11 | All input shall be validated server-side with FluentValidation regardless of client-side validation, and validation failure shall return a structured 400 identifying the offending fields. | Must |
| NFR-12 | All database access shall use parameterised queries through EF Core; no string-concatenated SQL shall exist in the codebase. | Must |
| NFR-13 | File uploads shall be restricted by MIME type and size, shall be re-encoded rather than stored as received, and shall never be served from a path that permits execution. | Must |
| NFR-14 | Error responses shall not disclose stack traces, SQL, connection strings, internal paths, provider messages or the existence of resources in other organisations. | Must |
| NFR-15 | Secrets shall be supplied exclusively through environment variables; a repository scan for committed secrets shall run in CI and shall fail the build on detection. | Must |
| NFR-16 | Rate limiting shall be applied to authentication-adjacent endpoints, workflow initiation and report export. | Should |
| NFR-17 | Dependencies shall be scanned for known vulnerabilities in CI, and any critical advisory shall be resolved before the baseline is submitted. | Should |
| NFR-18 | The system shall be assessed against the OWASP Top 10 and the LLM-specific risks of prompt injection, insecure output handling and excessive agency, with findings recorded in the security section of the consolidated report. | Must |

## 10.3 Availability and Reliability

| ID | Requirement | Priority |
|---|---|---|
| NFR-19 | All deployed components shall be available throughout the evaluation period, and all evaluator-facing URLs shall remain accessible for at least three weeks after submission. | Must |
| NFR-20 | The `/health` endpoint shall report the reachability of the database, the agent service and the identity provider individually, so that a partial outage is diagnosable. | Must |
| NFR-21 | Failure of the agent service shall degrade the system gracefully: every non-agentic function shall remain fully operable and the unavailability shall be surfaced to the user. | Must |
| NFR-22 | Failure of the email provider shall never roll back or block a business transaction. | Must |
| NFR-23 | No business operation shall leave data in a partially applied state; multi-entity operations shall be transactional and shall roll back completely on failure. | Must |
| NFR-24 | Transient database failures shall be retried under a bounded policy; exhaustion shall return 503 rather than an unhandled exception. | Must |

## 10.4 Usability and Accessibility

| ID | Requirement | Priority |
|---|---|---|
| NFR-25 | A user holding a single role shall be able to complete the primary task of that role without training beyond a one-page guide. | Should |
| NFR-26 | The React application shall meet WCAG 2.1 Level AA for colour contrast, keyboard operability, focus visibility and programmatic form labelling. | Should |
| NFR-27 | The scan-to-verify sequence in the mobile application shall be completable one-handed in no more than four taps from the dashboard. | Should |
| NFR-28 | Every operation lasting more than 300 ms shall display a loading state, and every failure shall present an actionable message rather than a technical code. | Must |
| NFR-29 | Interface text shall be externalised from components to permit later translation, though only English is delivered in the baseline. | Could |

The React client is built on the IBM Carbon Design System (Section 3.6, ADR-008); its components are WCAG 2.1 AA compliant by construction, which is the primary mechanism by which NFR-26 is satisfied without bespoke accessibility work.

## 10.5 Maintainability

| ID | Requirement | Priority |
|---|---|---|
| NFR-30 | The backend shall maintain a layered structure with dependencies pointing inward; the domain layer shall reference no infrastructure package. | Must |
| NFR-31 | External dependencies — email, agent service, identity directory, QR generation — shall be reached only through interfaces defined in the application layer. | Must |
| NFR-32 | React components shall be organised by feature with shared presentational components extracted; no component shall exceed roughly 300 lines without decomposition. | Should |
| NFR-33 | Flutter widgets shall separate presentation from business logic through Riverpod providers; no widget shall call an HTTP client directly. | Must |
| NFR-34 | Every agent shall be a separate module with an explicitly declared input and output contract, so that an agent can be modified or replaced without touching the graph definition. | Must |
| NFR-35 | Code shall pass the configured linter and analyser with no errors; CI shall enforce this. | Must |
| NFR-36 | Every architecturally significant decision shall be recorded as an ADR before the corresponding implementation is merged. | Must |

## 10.6 Auditability

| ID | Requirement | Priority |
|---|---|---|
| NFR-37 | Every state-changing operation shall produce an audit record identifying actor, organisation, entity, operation, changed values, timestamp and correlation identifier. | Must |
| NFR-38 | Audit records and asset history shall be append-only; no API path shall permit their modification or deletion, verified by an automated test. | Must |
| NFR-39 | Every agentic recommendation shall be reconstructable from persisted state alone, without reference to logs or to the model provider. | Must |
| NFR-40 | Every approval decision shall record the decider, the decision, the reason and a snapshot of the state on which the decision was made. | Must |
| NFR-41 | A single user action shall be traceable across client, API, database and agent service through one correlation identifier. | Must |

## 10.7 Scalability and Portability

| ID | Requirement | Priority |
|---|---|---|
| NFR-42 | The API shall be stateless with respect to sessions so that additional instances can be added without affinity. | Must |
| NFR-43 | The data model shall support additional organisations without schema change; organisation isolation shall be enforced by query filter rather than by deployment separation. | Must |
| NFR-44 | A new asset domain shall be introducible through configuration alone — categories, types and attribute definitions — with no application code change and no migration. | Must |
| NFR-45 | Every component shall be runnable in a container with configuration supplied by environment variable, so that the deployment target is replaceable. | Should |
| NFR-46 | The system shall use no cloud-provider-proprietary service that would prevent redeployment to a different platform or to a sovereign cloud. | Should |

## 10.8 Privacy and Regulatory Compliance

| ID | Requirement | Priority |
|---|---|---|
| NFR-47 | Personal data collected shall be limited to what is necessary to attribute actions and route notifications, in accordance with the data-minimisation principle of the Personal Data Protection Act No. 9 of 2022. | Must |
| NFR-48 | Personal data shall be processed only for the declared purposes and shall not be reused for analytics, profiling or model training. | Must |
| NFR-49 | No personal data shall be transmitted to the model provider; agent tool response schemas shall exclude user names and email addresses. | Must |
| NFR-50 | A user deletion request shall be honoured by anonymising the mirror record while preserving the referential integrity of the audit trail. | Should |
| NFR-51 | Data shared with the email provider shall be limited to recipient address and name, asset code, and the action required. | Must |

## 10.9 Testability

| ID | Requirement | Priority |
|---|---|---|
| NFR-52 | Every business rule stated in Section 6 shall be covered by at least one automated test asserting both the permitted and the rejected path. | Must |
| NFR-53 | The agent service shall be substitutable with a deterministic stub in backend tests so that API behaviour is testable without a model provider. | Must |
| NFR-54 | Golden cases shall use fixed input fixtures so that agentic evaluation is repeatable and independent of model non-determinism for the deterministic assertions. | Must |
| NFR-55 | Database integration tests shall run against a real PostgreSQL instance, not an in-memory substitute, so that constraints, migrations and transaction behaviour are genuinely exercised. | Must |
| NFR-56 | CI shall restore, build and run the backend test suite on every push and pull request to main, and shall fail the build on any test failure. | Must |
