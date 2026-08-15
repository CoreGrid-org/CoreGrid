# 11. Third-Party Integration

## 11.1 Transactional Email — Business Purpose

CoreGrid's workflows regularly stop and wait for a specific person to act: an officer must begin an assigned repair, an administrator must decide a disposal, a requester must learn the outcome. Without notification, those waits are discovered only when somebody happens to open the dashboard, and the elapsed time between a decision becoming possible and a decision being made is the single largest source of delay in the manual process CoreGrid replaces. Email is chosen over SMS because every user in the target environment has an institutional address, because the message can carry a deep link into the relevant record, and because it leaves a durable record the recipient can act on later.

| Trigger | Recipient | Content |
|---|---|---|
| Maintenance record assigned | Assigned officer | Asset code, fault summary, priority, link to the record. |
| Transfer awaiting approval | Administrator of the source department | Asset code, origin, destination, requester, link. |
| Disposal awaiting approval | Administrator | Asset code, condemnation reason, residual value, link. |
| Agentic workflow awaiting approval | Administrator | Asset code, recommendation, validation verdict, link to the execution summary. |
| Approval decision made | Requesting officer | Decision, reason, resulting asset status. |
| Verification campaign opened | Assigned officers | Campaign name, scope, due date, task count. |

## 11.2 Integration Requirements

| ID | Requirement |
|---|---|
| INT-01 | The provider shall be reached exclusively from the ASP.NET Core backend; no client shall hold the API key or call the provider. |
| INT-02 | The API key shall be supplied through environment configuration, shall never be committed, and shall never appear in a log or an error response. |
| INT-03 | Dispatch shall be abstracted behind `INotificationService` with an `EmailNotificationService` implementation, so that the provider can be replaced without touching any calling service. |
| INT-04 | Requests shall time out after 10 seconds and shall be retried up to three times with exponential backoff. |
| INT-05 | A permanent failure shall be logged with the correlation identifier and surfaced in the in-app notification list; it shall never roll back or block the business transaction. |
| INT-06 | Rate-limit responses from the provider shall be honoured with backoff rather than retried immediately. |
| INT-07 | Message content shall be limited to the minimum data of Section 10.8 and shall never contain tokens, credentials or full records. |
| INT-08 | Every dispatch attempt and outcome shall be recorded so that "was this person told" is answerable from the system. |

## 11.3 Other External Services

Two further external dependencies exist and are specified elsewhere in this document, but are recorded here for completeness. ThunderID provides authentication, the organisation directory and user provisioning, and is specified in full in Section 4. The model provider serving the language model used by the agents is reached only from the agent service, holds no personal data, and is governed by the security requirements AI-22 to AI-27. QR encoding is performed server-side within the API rather than through an external service, which removes a network dependency from a hot path and keeps label generation deterministic and reproducible.
