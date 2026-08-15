# 5. External Interface Requirements

## 5.1 User Interface Requirements

| ID | Requirement | Client |
|---|---|---|
| IF-01 | Every list view shall provide server-side search, filtering, sorting and pagination, and shall display distinct loading, empty, error and populated states. | React |
| IF-02 | Navigation shall be role-aware: an action a user is not permitted to perform shall not be rendered, and its route shall additionally be protected. | React, Flutter |
| IF-03 | Every form shall validate on the client for immediate feedback and shall surface server-side validation errors against the specific fields that failed. | React, Flutter |
| IF-04 | The React application shall be responsive from 1280px down to 768px and shall meet WCAG 2.1 Level AA for colour contrast, keyboard operability and form labelling. | React |
| IF-05 | Destructive or irreversible actions shall require explicit confirmation that names the affected asset and states the consequence. | React, Flutter |
| IF-06 | The Flutter application shall present the scanner as the primary action on the operational dashboard and shall return a result or a clear failure within three seconds of a readable code entering the frame. | Flutter |
| IF-07 | The Flutter application shall adapt to phone screens from 360dp width upward and shall remain usable one-handed for the scan-verify sequence. | Flutter |
| IF-08 | Agent recommendations shall be presented with the factors that produced them — never as an unexplained verdict — and shall be visually distinguished from decisions made by a person. | React, Flutter |
| IF-09 | Error messages shall state what happened and what the user can do next, and shall not expose stack traces, SQL, internal identifiers or provider messages. | React, Flutter |

## 5.2 Hardware Interface Requirements

| ID | Requirement |
|---|---|
| IF-10 | The Flutter application shall access the device rear camera for QR decoding, requesting permission at first use with a plain-language explanation and degrading to manual asset-code entry if permission is refused. |
| IF-11 | The Flutter application shall access the camera or photo library to attach an image to a fault report, compressing to a maximum of 1MB before upload. |
| IF-12 | The application shall function on devices without hardware acceleration for camera preview, accepting reduced scan frame rate rather than failing. |
| IF-13 | No other device hardware — location, biometrics, Bluetooth, NFC — is required by the baseline release. |

## 5.3 Software Interface Requirements

| Interface | Direction | Protocol / contract | Failure handling |
|---|---|---|---|
| ThunderID — OIDC authorisation | Clients → ThunderID | Authorisation Code with PKCE; discovery document; JWKS. | Authentication failure returns the user to sign-in with a non-technical message. JWKS retrieval failure serves the cached key set and raises a health-check warning. |
| ThunderID — SCIM 2.0 | API → ThunderID | REST/JSON with a confidential service credential, for user creation, role assignment and deactivation. | Timeout of 10 seconds; two retries with exponential backoff; on final failure the administrator is told the invitation could not be sent and no partial local state is committed. |
| LangGraph agent service | API → agent service | Internal REST/JSON: start workflow, query status, resume after approval. Shared-secret authenticated, private network path only. | Timeout of 120 seconds for a run; on timeout or error the workflow is recorded as a safe failure with the cause, and no business state changes. |
| Agent tool callbacks | Agent service → API | Allow-listed read-only tool endpoints, authenticated as the agent service principal, schema-validated request and response. | A rejected or malformed tool call returns a structured error the agent must handle; repeated failure routes the graph to safe failure. |
| Email provider | API → provider | HTTPS REST with an API key held in server configuration; templated transactional messages. | Timeout of 10 seconds; three retries with backoff; permanent failure is logged and surfaced in-app, and never blocks or rolls back the business transaction that triggered it. |
| QR generation | Internal to API | Server-side encoding of the asset code into a PNG or SVG label payload. | Deterministic; failure returns 500 and is logged. Codes remain re-generatable from the asset code at any time. |
| PostgreSQL | API → database | Npgsql over TLS; schema managed by EF Core migrations. | Connection resilience with a bounded retry policy; on exhaustion the request fails with 503 and the health endpoint reports the dependency as unhealthy. |

## 5.4 Communication Interface Requirements

- All external communication shall use HTTPS with TLS 1.2 or above. Plain HTTP shall be redirected, and HSTS shall be enabled on the API and the static host.
- The API shall expose a REST interface using conventional resource routing, correct HTTP verbs and accurate status codes: 200, 201, 204 for success; 400 for validation failure; 401 for authentication failure; 403 for authorisation denial; 404 for absence; 409 for a state-machine or concurrency conflict; 422 for a business-rule rejection; 429 for rate limiting; 503 for dependency unavailability.
- Request and response bodies shall be JSON with camel-case property naming, ISO 8601 UTC timestamps and a documented error envelope carrying a machine-readable code, a human-readable message, and a field-level detail array where applicable.
- All list endpoints shall accept `page`, `pageSize`, `sortBy`, `sortDirection`, `search` and resource-specific filter parameters, and shall return items together with `totalCount`, `page` and `pageSize`.
- Every request shall carry a correlation identifier, generated by the API when absent, propagated to the agent service and included in every log entry and audit record, so that one user action can be traced across all components.
- Cross-origin access shall be restricted to the configured React origins; the mobile client is not subject to CORS.
- Long-running agent workflows shall be polled rather than held open: the initiating request returns immediately with a workflow identifier, and the client polls a status endpoint at a bounded interval.
