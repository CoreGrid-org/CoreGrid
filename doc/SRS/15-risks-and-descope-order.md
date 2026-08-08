# 15. Risks and Descope Order

## 15.1 Risk Register

| ID | Risk | L | I | Response |
|---|---|---|---|---|
| R-01 | Integration is deferred and the components do not fit together late in the schedule. | M | H | A vertical slice — sign-in through both clients to the API to the database to a stub agent call — is completed in week one and never allowed to break. Integration is continuous, not a phase. |
| R-02 | The agentic subsystem proves harder than expected and consumes the schedule. | M | H | The graph, contracts and the deterministic gate are built against stubbed agents first, so the workflow is demonstrable before any model call succeeds. Model integration is then an enhancement rather than a prerequisite. |
| R-03 | Asgardeo configuration or availability blocks authentication near evaluation. | L | H | The `IIdentityDirectory` abstraction and the configuration-selectable local fallback of Section 4.10, implemented and tested during the stabilisation week. |
| R-04 | Model provider rate limits or outages disrupt the demonstration. | M | M | Golden cases run against recorded fixtures; the demonstration path is rehearsed with a fallback recording; workflow timeouts terminate safely rather than hanging. |
| R-05 | The configurable attribute model expands into a general low-code platform. | M | H | Section 3.5 fixes what is configurable. Any proposal beyond categories, types, attributes and policy thresholds is a scope change requiring group approval. |
| R-06 | Uneven contribution leaves one component or one owner's evidence weak. | M | H | Requirement-referenced issues, per-component branches, mandatory review, and a weekly check that every owner has merged work across backend, database, web, mobile and tests. |
| R-07 | Flutter is built as a second React application, weakening the boundary argument. | M | M | The boundary table in Section 3.4 is normative. A management capability proposed for Flutter is rejected by default. |
| R-08 | Deployment is attempted in the final week and fails. | M | H | All five components are deployed once in week one, however minimally, and redeployed continuously thereafter. |
| R-09 | Test evidence is generated late and does not reflect real execution. | M | M | CI runs from week one; tests accompany the feature in the same pull request; a feature merged without tests is treated as incomplete. |
| R-10 | A member cannot explain AI-assisted code at the viva. | M | H | The operating rule is that AI proposes, the owner reviews, tests, understands and only then commits; the AI usage log records what was changed or rejected and how it was verified. |

## 15.2 Descope Order

If schedule pressure requires reduction, requirements are removed in the order below. Nothing above the line may be removed, because each item above it is either an assignment acceptance criterion or a dependency of one.

```
  REMOVE FIRST  ─────────────────────────────────────────────────────────
    1  NFR-29 externalised strings · NFR-08 route code-splitting
    2  FR-080 in-app notification list
    3  FR-061 manual discrepancy raising
    4  FR-048 outstanding-transfer flagging · AI-19 overdue approval flag
    5  FR-041 automatic preventive scheduling
    6  FR-085 PDF export (retain CSV)
    7  FR-034 photographic attachments (retain textual fault reporting)
  ═══════════════════ HARD FLOOR — NOTHING BELOW MAY BE REMOVED ═════════
    Four business components · four distinct agents · deterministic
    validation · human approval · persisted workflow state · both clients
    on the shared API · JWT authentication and RBAC · third-party
    integration · CI · deployment · the golden end-to-end workflow
```
