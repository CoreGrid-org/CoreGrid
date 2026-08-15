# 18. Team Roster and Individual Work Allocation

## 18.1 Purpose

Sections 6, 7 and 12 of this SRS define the work by role — "Student 1" through "Student 4" — so that the specification reads independently of who is in the group. This section is the one place that binds those roles to the named members of Group SE3090_G\<NN\>. It changes no requirement; it only names an owner against requirements, entities, endpoints and agents that are already specified elsewhere in this document, and states the evidence each owner must produce, consistent with SE3090 §3 and the AI-use rule in Appendix E.

Component A (Asset Registry & QR Identification) was claimed first and is fixed to Jayashan Guruge. The remaining three components are allocated by carrying forward the Student 2–4 mapping already used throughout Sections 7 and 12, so nothing in the rest of this document needs to change.

## 18.2 Roster

| Role | Name | GitHub username | Branch prefix | Primary component | Requirement range owned (§12) | Agent owned (§7.3) | Business-specific operation (§6) |
|---|---|---|---|---|---|---|---|
| Student 1 | **Jayashan Guruge** | `<github-handle>` | `feature/asset-*` | A — Asset Registry & QR Identification | FR-016 to FR-032 | Planner Agent | `POST /api/assets/{id}/verify` (FR-031) |
| Student 2 | **Seneja Ramanayaka** | `<github-handle>` | `feature/maintenance-*` | B — Maintenance Management | FR-033 to FR-042, FR-077 to FR-080 | Maintenance Analysis Agent | `POST /api/maintenance/{id}/complete` (FR-038) |
| Student 3 | **Bhanuka Samarasinghe** | `<github-handle>` | `feature/transfer-*`, `feature/disposal-*` | C — Transfer & Disposal | FR-043 to FR-055 | Budget Analysis Agent | `POST /api/disposals/{id}/approve` (FR-051) |
| Student 4 | **Hasitha Erandika** (Group Leader) | `<github-handle>` | `feature/audit-*`, `feature/config-*`, `feature/ci-*` | D — Audit & Compliance, plus organisation configuration and user administration | FR-010 to FR-015, FR-056 to FR-066 | Policy Compliance Agent + human-approval checkpoint | `POST /api/discrepancies/{id}/resolve` (FR-062) |

Component D absorbs organisation configuration (departments, locations, users, policy parameters — FR-010 to FR-015) in addition to audit and compliance, exactly as allocated in §12. Giving this range to the Group Leader is a deliberate, not incidental, choice: the leader already carries the CI pipeline, the consolidated submission and cross-cutting authorisation testing (§18.5 below), and organisation configuration is the one component every other owner's demonstration data depends on, so it benefits from being built and stabilised early by whoever is coordinating the schedule.

Per §12.1, no member holds a project-management-only, testing-only or documentation-only role. Every row below delivers backend, database, React, Flutter, tests, Git evidence, documentation and a distinct agentic contribution.

### 18.2.1 What a Complete Roster Row Must Capture

The roster is not just a name-to-component lookup — it is the thing an evaluator, a teammate resuming after a break, or the admin dashboard's own "who owns this" documentation all point to. A row is incomplete unless every one of the following is present and kept current, and the front-matter summary table (§00, "Team Roster") must never drift from it:

| Field | Why it is required |
|---|---|
| Full name and student number | Ties the row to the university's own roll, required for individual marking under SE3090 §3. |
| GitHub username | Makes pull-request review assignment (§12.1, R-06) and commit attribution mechanical rather than inferred from a display name. |
| Contact email | The address the Group Leader and Lecturer-in-Charge use for scheduling and scope-change approval (§00, Purpose of Baselining). |
| Primary component and requirement range | Fixes accountability for every FR listed against the member — this is what makes "work an owner cannot explain is treated as not delivered" (§18.10) enforceable. |
| Branch prefix(es) | Lets CI, code review and the Git-evidence audit (§12.1) filter a member's contribution mechanically instead of by memory. |
| Agent owned and its tool allow-list (§7.3, §7.4) | Every member's agentic contribution must be distinct and traceable; the roster is where that distinctness is asserted, not just implied by the pipeline diagram. |
| Business-specific operation owned (§6) | Each component's one operation beyond CRUD (verify / complete / approve-disposal / resolve-discrepancy) is the single clearest demonstration artefact for that member's viva — the roster row is where a marker finds it without searching. |
| Database entities owned (§8.2) | Needed so that a schema change (see the physical design in [`system.md`](system.md)) is reviewed by the right person before merge. |
| Golden-case ownership (§18.7) | A member's agentic contribution is only demonstrable if at least one golden case exercises it; the roster is the place that mapping is asserted per member, not just per case. |

A roster row missing any of these is not "mostly done" — from a marking-evidence standpoint it is the same as not having named an owner at all, because the missing field is exactly the thing an evaluator asks for first.

## 18.3 Required Individual Evidence — Jayashan Guruge (Component A)

**Backend** — Asset, AssetType, AssetCategory, AssetAttributeDefinition and Location controllers/services (FR-016 to FR-020); asset registration, code generation and QR payload generation (FR-021 to FR-023); `GET /api/assets/qr/{code}` lookup (FR-024, FR-025); amendment with history recording (FR-026, FR-027); search/filter/sort/pagination (FR-028); condition recording (FR-029); depreciation computation (FR-030); the verification operation `POST /api/assets/{id}/verify` (FR-031) with discrepancy-raising on mismatch; deletion guard for assets with lifecycle history (FR-032).

**Database** — `AssetCategories`, `AssetTypes`, `AssetAttributeDefinitions`, `AssetAttributeValues`, `Assets`, `AssetHistory`, and (jointly with Component D on the endpoint, per the note in §18.2) `Locations` — per the entity ownership table in §8.2. Composite unique constraint on `(OrganizationId, AssetCode)` (DR-05); indexes supporting FR-028's filters (DR-08).

**React** — Asset list, detail, create and edit; dynamic attribute-driven forms rendered from `AssetAttributeDefinitions` with no domain-specific client code (FR-020); category, type and attribute configuration screens.

**Flutter** — QR scanner using the device rear camera with manual-entry fallback (IF-10, FR-024, FR-025); asset detail screen; condition update; the verification flow (FR-031).

**Agentic AI — Planner Agent** — input `EvaluationObjective { assetId, objectiveText, initiatedBy, organizationId }`, output `ExecutionPlan { steps[], inScope, rejectionReason? }`; sole allow-listed tool `get_asset_summary`; owns objective-scope rejection before any other agent runs.

**Tests** — Asset service unit tests; custom-attribute validation tests (positive and negative per FR-019); QR resolution integration test against FR-024's acceptance criteria (AC1–AC4, including the cross-organisation 404 in AC2); React asset-form component tests; Flutter scanner widget test.

**Git evidence** — Branch prefix `feature/asset-*`; issues citing FR-016 to FR-032; PRs reviewed by at least one other member (§12.1).

**Documentation** — README section for Component A; AI usage log per Appendix E; input to ADR-006 (attribute-value storage) since it is Component A's data-modelling decision.

## 18.4 Required Individual Evidence — Seneja Ramanayaka (Component B)

**Backend** — Maintenance controller/service implementing the state machine REQUESTED → APPROVED → IN_PROGRESS → COMPLETED/CANCELLED (FR-035 to FR-037, Fig. 7); attachment handling with compression (FR-034); the completion operation `POST /api/maintenance/{id}/complete` (FR-038) with the three business rules BR1–BR3 in one transaction; the UNDER_MAINTENANCE guard blocking transfer/disposal (FR-039); cumulative cost/repair-count derivation (FR-040); preventive scheduling job (FR-041, Should); list/filter/sort/pagination (FR-042); `INotificationService` and its email provider (FR-077 to FR-080).

**Database** — `MaintenanceRecords`, `MaintenanceAttachments`, `Notifications` (§8.2). Seed data with enough completed maintenance history to give the Maintenance Analysis Agent a meaningful result (DR-14).

**React** — Maintenance list (status/priority/assignee/date filters), detail, assign and complete screens; notification centre with unread state (FR-080).

**Flutter** — Fault-report screen using camera or photo-library access, compressed to 1MB (IF-11, FR-033); maintenance task list and progress-update screen (FR-037 mobile side).

**Agentic AI — Maintenance Analysis Agent** — input `MaintenanceAnalysisRequest { assetId, windowMonths }`, output `MaintenanceAnalysis { repairCount, cumulativeCost, meanTimeBetweenFailuresDays, costTrend, projectedAnnualCost, dataQuality, confidence }`; allow-listed tools `get_maintenance_history`, `compute_failure_statistics`.

**Tests** — State-machine tests for every legal and illegal transition in Fig. 7; the completion transaction test (BR3 atomicity, AC1–AC4 of FR-038); notification failure-isolation test proving a delivery failure never rolls back completion (FR-078); Flutter fault-report form-validation tests.

**Git evidence** — Branch prefix `feature/maintenance-*`; issues citing FR-033 to FR-042 and FR-077 to FR-080.

**Documentation** — README section for Component B; AI usage log; notes on the notification-provider choice (this decision is Component B's to write up even though it is not one of the seven indexed ADRs in Appendix D — record it as a supporting design note referenced from the group report).

## 18.5 Required Individual Evidence — Bhanuka Samarasinghe (Component C)

**Backend** — Transfer request/approve/reject/confirm-receipt (FR-043 to FR-047); outstanding-transfer flag job (FR-048, Should); condemnation (FR-049); disposal request (FR-050); the approval operation `POST /api/disposals/{id}/approve` (FR-051) checking preconditions P1–P6 in one transaction, with separation of duties (approver ≠ requester); disposal-revision path (FR-053); disposal outcome recording and the DISPOSED terminal-state guard (FR-054, FR-055).

**Database** — `AssetTransfers`, `DisposalRequests` (§8.2). Multi-table transaction design for transfer confirmation and disposal approval (DR-10). Component C's endpoints are the highest-contention write paths in the system (two officers confirming or approving against the same asset), so its integration tests are the right place to exercise the optimistic-concurrency requirement DR-11 (PostgreSQL `xmin` as an EF Core concurrency token) end to end, even though DR-11 itself is a system-wide requirement every write path must honour.

**React** — Transfer request/approval queue; disposal request/approval queue with evidence display; a precondition checklist showing the live P1–P6 status before Approve is enabled.

**Flutter** — Transfer request creation and scan-based receipt confirmation (FR-046); condemnation flow capturing condition and evidence from the field (FR-049).

**Agentic AI — Budget Analysis Agent** — input `FinancialAssessmentRequest { assetId, maintenanceAnalysis }`, output `FinancialAssessment { residualValue, replacementEstimate, repairToReplaceRatio, budgetHeadroom, rankedOptions[], proposedRecommendation }`; allow-listed tools `get_asset_financials`, `get_department_budget_summary`, `compute_depreciation`.

**Tests** — A dedicated negative test for each of P1–P6 (FR-051 AC3, 422 with no state change); the separation-of-duties authorisation test (AC2); a concurrency-conflict test (DR-11); React tests for the approval queue and the precondition checklist.

**Git evidence** — Branch prefixes `feature/transfer-*`, `feature/disposal-*`; issues citing FR-043 to FR-055; at least one documented merge-conflict resolution, since Component C's branches are the most likely to collide with Component A's asset-status changes.

**Documentation** — README section for Component C; AI usage log; Component C should document the concurrency-control approach (DR-11) as a supporting design note, since it is the component that proves it under contention.

## 18.6 Required Individual Evidence — Hasitha Erandika, Group Leader (Component D)

**Backend** — Department, Location and Policy controllers (FR-010 to FR-012, FR-015); user administration and the ThunderID SCIM client for invite/role-change/deactivate (FR-013, FR-014); verification campaign, task generation and assignment (FR-056, FR-057); automatic discrepancy raising on mismatch (FR-060); manual discrepancy raising (FR-061, Should); the resolution operation `POST /api/discrepancies/{id}/resolve` (FR-062) covering all five resolution types with BR1–BR3; append-only `AuditLogs` (FR-063, FR-064, DR-12); campaign reporting with PDF/CSV export (FR-065, FR-084, FR-085); campaign progress dashboard feed (FR-066, FR-081, FR-082, FR-086).

**Database** — `Departments` (shared entity, §8.2), `VerificationCampaigns`, `AuditVerifications`, `Discrepancies`, `AuditLogs`, `OrganizationPolicies`, `Users` (§8.2). Owns and documents the global `OrganizationId` query filter (DR-04) centrally, since every other component's tenant isolation depends on it being correct.

**React** — Admin screens for departments, locations, users and policy thresholds; audit dashboard; campaign management; discrepancy resolution screen; reports/export screens; the main analytics dashboard with the three required visualisations (FR-082).

**Flutter** — Verification task list ordered by due date (FR-058); the field verification flow — scan, then assert presence/location/condition (FR-059).

**Agentic AI — Policy Compliance Agent and the human-approval checkpoint** — input `PolicyValidationRequest { assetId, proposedRecommendation, financialAssessment }`, output `PolicyValidation { verdict, ruleResults[], blockingReasons[], isHighImpact }`; allow-listed tools `get_organization_policies`, `get_asset_compliance_state`. Owns the deterministic rule engine evaluating PR-01 to PR-09 (§7.6) — the verdict is computed by rules, not the model, which is the answer to the viva question about trusting an LLM with a compliance decision (§7.3) — and owns the interrupt/resume mechanics of the human-approval checkpoint (AI-13 to AI-20, §7.7).

**Tests** — Append-only tests for `AuditLogs` and `AssetHistory` (DR-12); discrepancy-resolution tests for all five resolution types (FR-062 AC1–AC4); the authorisation matrix test across all four roles plus the agent service principal (§13.2, AI-28); CI workflow ownership (§13.6 — restore/build/test on push and PR, React and Flutter jobs, secret scanning, PostgreSQL service container).

**Git evidence** — Branch prefixes `feature/audit-*`, `feature/config-*`, `feature/ci-*`; also responsible for week-one repository setup, the project board, the issue-labelling scheme, and confirming every teammate's pull requests are reviewed before merge (§12.1, R-06).

**Documentation** — Consolidated assembly of the Group Report from the four Individual Report sections; coordinates the ADR set in Appendix D (each owner drafts the decision in their own domain — ADR-006 with Jayashan, notification and concurrency notes with Seneja and Bhanuka — the leader checks the set is complete, not that it is correct); owns the final README and the demonstration script; opens every submission link in an incognito browser before the deadline, per SE3090 §15.

## 18.7 Golden-Case Test Ownership (§13.4)

Section 13.4 fixes twelve golden cases but does not name an owner for each — it specifies behaviour, not allocation. The mapping below assigns each case to whichever agent or checkpoint it actually exercises, so ownership follows the mechanism under test rather than being split evenly for its own sake. Cases that cross two components name a primary owner and a required reviewer.

| Case | Mechanism under test | Primary owner | Reviewer |
|---|---|---|---|
| GC-01 Correct disposal recommendation | Full pipeline; deterministic gate PASS; interrupt to AWAITING_APPROVAL | Hasitha (gate/checkpoint) | Jayashan, Seneja, Bhanuka (each agent's leg) |
| GC-02 Correct repair recommendation | Budget Agent ranking, `isHighImpact = false` | Bhanuka | — |
| GC-03 Policy blocks disposal (PR-01) | Rule engine | Hasitha | — |
| GC-04 Revision path (PR-03, missing valuation) | Rule engine returns NEEDS_REVISION; revision cap AI-20 | Hasitha | Seneja (re-entry to the Maintenance node) |
| GC-05 Insufficient data | Maintenance Agent `dataQuality` | Seneja | — |
| GC-06 Tool allow-list enforcement | Planner attempting a Budget-only tool (AI-03) | Jayashan | — |
| GC-07 Prompt-injection resistance | Objective-text sanitisation at Planner input (AI-22, AI-24) | Jayashan | Hasitha (security-event recording in `AuditLogs`) |
| GC-08 Schema violation | Deterministic gate stage 1 | Hasitha | — |
| GC-09 Tool timeout | `get_maintenance_history` timeout/retry (AI-06) | Seneja | — |
| GC-10 Approval authorisation | `workflow:approve` enforcement (AI-14) | Hasitha | — |
| GC-11 Approval executes correctly | Checkpoint resume; disposal executed through the Component C business service (AI-17) | Hasitha | Bhanuka (P1–P6 re-checked at execution) |
| GC-12 Rejection changes nothing | Rejection path (FR-074) | Hasitha | — |

## 18.8 Shared Agent Contract Freeze

Planner → Maintenance Analysis → Budget Analysis → Policy Compliance is a strict pipeline (§7.2): each agent's output is the next agent's input. The four input/output contracts in §7.3's table must be agreed and frozen by all four owners before any owner writes their agent's internal logic, otherwise the four agents risk being built independently and failing to chain together when the graph is first run end to end. This is not a new requirement — the contracts already exist in §7.3 — it is a scheduling instruction: treat that table as locked from week one, and raise any change to it as a group decision, not a unilateral one.

## 18.9 Suggested Delivery Rhythm

This is a suggested pacing, not a new requirement — it operationalises the seven-week-plus-stabilisation window fixed in §1's referenced delivery plan and the mitigations already recorded against R-01, R-06, R-08 and R-09 in Section 15 (a working vertical slice in week one, continuous integration from week one, tests accompanying every feature, deployment redone continuously rather than attempted at the end).

| Week | Every owner, in parallel |
|---|---|
| 1 | Vertical slice: own entity, one CRUD endpoint, one React screen, one Flutter screen, wired to real ThunderID auth and deployed, however minimally (R-01, R-08). Agent contracts in §7.3 frozen (§18.8). |
| 2 | Full CRUD with search/filter/sort/pagination for the owned component. |
| 3 | Owned state machine implemented with guarded transitions and negative tests. |
| 4 | Owned business-specific operation (verify / complete / approve-disposal / resolve-discrepancy) working end to end with its transaction and audit trail. |
| 5 | Own agent built against a stubbed model call, wired into the LangGraph graph as one node. |
| 6 | Real model call integrated; full four-agent graph run together; owned golden cases (§18.7) passing. |
| 7 | Tests complete, CI green, hardening, authorisation matrix run. |
| 8 (stabilisation) | No new features: regression testing, AI usage logs and reflections finalised, ADR set checked complete, documentation, viva preparation. |

## 18.10 Contribution Evidence Requirements

These carry forward from §12.1 unchanged and apply to every row above without exception:

- Feature branches named for the owner's component; one pull request per feature; every pull request reviewed by at least one other member before merge.
- Every requirement identifier referenced in the GitHub issue and in the commit or pull-request description that implements it.
- Each owner individually accountable at the viva for explaining, modifying and debugging their own contribution; work an owner cannot explain is treated as not delivered (SE3090 §3).
- Each owner maintains an individual AI usage log per Appendix E — date, tool and model, task, what was produced, what was changed or rejected, how it was verified.
- No member holds a project-management-only, testing-only or documentation-only role.

## 18.11 Keeping the Roster Current

The roster is a live document, not a one-time submission artefact — it must reflect reality at every checkpoint in §18.9, not just at baseline.

- **Component ownership never changes; delivery status does.** [`doc/PROGRESS.md`](../PROGRESS.md) is the up-to-date record of what each owner has actually landed against the requirement range in §18.2; this file records who is accountable, not what is finished. If PROGRESS.md shows an item as ❌ past the week it was due in §18.9, that is a schedule risk to raise, not a roster edit.
- **The front-matter Team Roster table (§00) is a derived view of §18.2.** Whenever a name, GitHub username, component, or contact detail changes here, update it there in the same edit — do not let the two tables disagree about who owns what.
- **A scope-change to who owns what** (a component reassigned, a member added or dropped) is itself a scope change under the rule in §00's "Purpose of Baselining": raise it as a GitHub issue labelled `scope-change`, get group approval, and record it in the SRS revision history before editing this section's tables.
- **Golden-case ownership (§18.7) and agent ownership (§7.3) are cross-checked against §18.2 at week 5–6** (§18.9) — the point at which every agent should be wired into the graph — so that no agent or golden case is left without a named, accountable owner going into the final two weeks.
