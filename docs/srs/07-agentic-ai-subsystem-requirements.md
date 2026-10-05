# 7. Agentic AI Subsystem Requirements

## 7.1 Purpose and Boundary

The agentic subsystem exists to answer one difficult, recurring question well: given everything the organisation knows about a particular asset, should it be repaired, replaced, transferred or disposed of — and does the answer comply with the organisation's own policy? Today that judgement is made inconsistently, by different people, with incomplete evidence, and it is rarely documented. CoreGrid's workflow assembles the evidence in a fixed sequence, produces a structured recommendation with its supporting factors, validates the recommendation against declarative rules, and then stops and asks a person.

The subsystem is deliberately not a chatbot, not a question-answering interface over documentation, and not a single-prompt summariser. It is a stateful graph with distinct nodes, controlled tools, durable state, deterministic validation and an interrupt for human approval — which is what makes the output defensible to an auditor.

| The subsystem may | The subsystem may never |
|---|---|
| Read asset, maintenance, financial and policy data through allow-listed tools | Write, update or delete any business record |
| Produce a structured plan and delegate steps to specialised agents | Choose which tools exist or extend its own permissions |
| Compute projections, comparisons and cost analyses | Approve its own recommendation |
| Evaluate configured policy predicates and report the outcome | Invent, reinterpret or override an organisation policy |
| Recommend an action and explain the factors behind it | Execute a high-impact action without human approval |
| Record a safe, explicit failure | Fail silently or leave a partially applied change |

## 7.2 The Assessed Workflow — Asset Lifecycle Decision

One workflow, the asset lifecycle evaluation, exercises every element of the subsystem: objective, plan, delegation, controlled tools, persisted state, deterministic validation, human approval, and an auditable result or safe failure. It is initiated from either client, executes through the four agents, validates deterministically, pauses for approval, and returns an updated status to the user who started it.

![CoreGrid asset lifecycle decision workflow](../diagrams/agent-workflow.png)

Figure 8 — The assessed Asset Lifecycle Decision workflow, satisfying the minimum acceptance rule end to end.

## 7.2.1 Orchestrator, Agent Nodes, and When a Node May Call a Model

**Target architecture, decided 2026-09-15, superseding ADR-005's original "Python LangGraph" scope (appendix D):** the entire agent subsystem — the Orchestrator and all four agent nodes — runs in-process inside the single ASP.NET Core API deployable. There is no separate agent runtime, container or network hop. This follows directly from CoreGrid's M0 deployment model (§4.1, §19.10): one deployment per customer, so the fewer independently-deployed services a customer has to install, patch and secure, the better — an enterprise buyer's security review has one process boundary to evaluate, not several, and AI-21's "private network path" requirement becomes structurally true rather than something to configure.

![CoreGrid agent architecture](../diagrams/agent-architecture.png)

Each agent's tool box is that agent's own allow-list (§7.4) — disjoint from every other agent's, and never touched by the Orchestrator directly.

**The Orchestrator** is `AgentWorkflowService` (the API-facing workflow service) together with `WorkflowPipeline` (node execution) and `WorkflowRouting` (the gate and decision transitions), in `Features/Agents/Services/Orchestration/`. It owns the `AgentWorkflow` row, sequences the four nodes in order, enforces the per-tool timeout and retry budget (AI-06) and the overall 120-second budget (AI-25), runs the three-stage deterministic gate (§7.6), persists every checkpoint (AI-08/AI-09) and drives the human-approval interrupt (§7.7). It holds a small set of its own tools, and they are control-plane only — it never holds a business tool and never reasons about the asset itself:

| Orchestrator tool | Purpose |
|---|---|
| `persist_workflow_state` | Write the current `AgentWorkflow`/`AgentExecutionStep` row |
| `checkpoint` / `resume` | AI-09 — resume a paused workflow without re-executing completed steps |
| `enforce_timeout` | AI-06, AI-25 — per-tool and per-workflow deadlines |
| `run_deterministic_gate` | §7.6 — schema, business-rule and authorisation stages |
| `request_human_approval` | AI-13 — raise the AWAITING_APPROVAL interrupt |

**The four agents** (§7.3) are the Orchestrator's workers, each in its own folder under `Features/Agents/Services/` (`Planner/IPlannerAgent`, `Budget/IBudgetAgent`, `Policy/IPolicyComplianceEvaluator`; the deterministic Maintenance Analysis node is `IMaintenanceTools` plus `Maintenance/MaintenanceAggregation`, run directly by `WorkflowPipeline`) with one typed input and one typed output: the contracts in §7.3's table. Each holds its own disjoint allow-list of *business* tools (§7.4) that the Orchestrator itself never touches directly. A node's own reasoning step may or may not call a model; that's a property of the node, decided by the criteria below, not a property of "being an agent" — nothing about the pattern requires a model call, and a node that doesn't need one is just a typed method.

**Model access — `LlmSettings` and the `Llm` HTTP client.** A node that needs a model uses no provider SDK. It posts to an OpenAI-compatible chat-completions endpoint through the named `HttpClient` "Llm" (registered in `AgentsModule`, 60-second timeout). Endpoint, model and key come from `LlmSettings`:

```
Llm:Endpoint / Llm:Model / Llm:ApiKey              shared defaults (Gemini's OpenAI-compatible endpoint by default)
LlmFallback:Endpoint / :Model / :ApiKey            optional second provider (Groq, openai/gpt-oss-120b by default)
<Agent>:Endpoint / <Agent>:Model / <Agent>:ApiKey  per-agent overrides, e.g. Planner:*, Budget:*
```

Any provider that exposes the OpenAI chat-completions protocol (a hosted vendor, or an on-prem/local model for a data-residency-constrained customer) can be selected by configuration per deployment. Choosing a model vendor is therefore a deployment decision, not an engineering decision baked into the product. A non-success response (including 429), a timeout or an unparseable reply makes the node retry once against an optional second provider (`LlmFallback:Endpoint` / `Model` / `ApiKey`; Groq's `openai/gpt-oss-120b` by default), and only then fall back to a deterministic result. A provider without a key is skipped. A model outage degrades a workflow; it never breaks one.

**When a node may call a model.** A node calls one only when all four hold; if any fails, it must be deterministic:

1. **Unstructured input** — the node has to interpret free text or another genuinely open-ended signal a fixed predicate can't parse (an objective typed by a human, not a set of typed fields).
2. **Unenumerable output space** — the decision can't be written as `if <field> <operator> <threshold> then <outcome>` over the node's own input contract. If it can, write that rule instead; it is faster, free, reproducible and directly satisfies AI-12 (a reviewer must be able to reconstruct *why* from the persisted artefacts, not from a model's mood on the day) — and, commercially, every rule-based node is one fewer inference call in the product's cost of goods sold.
3. **Advisory, not compliance-or-irreversible** — the node's output is never itself the verdict that permits a high-impact action. This is the existing rationale in §7.3 ("Why the Policy Agent does not decide"), generalised: nothing that gates an irreversible action may originate from a probabilistic call.
4. **Non-determinism is acceptable for that specific output** — reviewers can tolerate two runs producing differently-phrased-but-equivalent output; they cannot tolerate two runs producing different *recommendations* from identical facts.

Applied to the four agents:

| Agent | Calls a model? | Why |
|---|---|---|
| Planner | Yes | Free-text objective (criterion 1), plan shape isn't enumerable from a handful of predicates (2), output is advisory — a rejected/accepted plan, never an executed action (3) |
| Maintenance Analysis | No | Repair count, MTBF, cost trend and 12-month projection are closed-form statistics over typed maintenance records — criterion 2 fails, so a model would add cost, latency, non-reproducibility and per-run inference spend for zero benefit. Same shape as Policy Compliance. |
| Budget Analysis | No, target design — presently yes in the existing implementation | Residual value, replacement estimate, ratio and headroom are arithmetic (criterion 2 fails); the one generative part — rationale text on each ranked option — doesn't decide anything, it explains numbers already computed deterministically, so it can be templated instead of modelled. Target design moves this node in-process with the other three; the currently-built implementation (§7.3) predates this decision and is migrated on its own owner's schedule, not rewritten by this document. |
| Policy Compliance | No | The verdict gates a potentially irreversible action — criterion 3 forbids a model outright, independent of how enumerable the rules are. |

Under the target design only **one** agent, the Planner, calls a model at all. The current implementation also calls one from Budget Analysis, for option ranking and rationale, behind `BudgetAssessmentValidator` with a deterministic fallback. That is not a compromise; it is the criteria applied honestly, and it is also the cheapest and easiest-to-audit shape the subsystem could take while still keeping a genuine agentic reasoning step where one adds value.

## 7.3 Agent Specifications

An agent counts as distinct only where it has an identifiable responsibility, a defined input and output contract, controlled tool permissions and visible participation in the workflow. The four agents below satisfy that test: each consumes a different input, produces a different typed artefact, holds a different tool allow-list, and appears as a separate node with its own recorded execution in the workflow trace. None is a renamed copy of another.

| Agent | Component (§12) | Target implementation (§7.2.1) | Current implementation | Responsibility | Input contract | Output contract | Allow-listed tools |
|---|---|---|---|---|---|---|---|
| Planner Agent | A | In-process service in the API, calling the model through `LlmSettings` | `PlannerAgent` (`IPlannerAgent`) in `backend/Features/Agents/Services/Planner/`, registered in `AgentsModule`; deterministic fallback plan | Interpret the objective, confirm it is in scope, and produce an ordered, typed plan naming which agent executes each step. Rejects out-of-scope objectives before any analysis is performed. | `EvaluationObjective { assetId, objectiveText, initiatedBy, organizationId }` | `ExecutionPlan { steps[]: { seq, agent, purpose, expectedOutput }, inScope, rejectionReason? }` | `get_asset_type_summary`, `get_asset_summary` |
| Maintenance Analysis Agent | B | In-process service, no model call: closed-form statistics, the same shape as Policy Compliance | `IMaintenanceTools` + `MaintenanceAggregation` (`backend/Features/Agents/Services/Maintenance/`), run by `WorkflowPipeline` after the Planner | Quantify the asset's maintenance behaviour: how often it fails, what it has cost, whether the trend is worsening, and what the next twelve months are likely to cost. | `MaintenanceAnalysisRequest { assetId, windowMonths }` | `MaintenanceAnalysis { repairCount, cumulativeCost, meanTimeBetweenFailuresDays, costTrend, projectedAnnualCost, dataQuality, confidence }` | `get_maintenance_history`, `compute_failure_statistics` |
| Budget Analysis Agent | C | In-process service; deterministic computation, with rationale text templated rather than modelled | `BudgetAgent` (`IBudgetAgent`) in `backend/Features/Agents/Services/Budget/`, Node 3 of the pipeline; `BudgetTriage` computes the figures, the model only re-ranks them behind `BudgetAssessmentValidator`, with deterministic fallback | Convert the maintenance picture into a financial comparison: residual value against projected repair cost against replacement cost, within the department's budget reality, and rank the options. | `FinancialAssessmentRequest { assetId, maintenanceAnalysis }` | `FinancialAssessment { residualValue, replacementEstimate, repairToReplaceRatio, budgetHeadroom, rankedOptions[]: { action, score, rationale }, proposedRecommendation }` | `get_asset_financials`, `get_department_budget_summary`, `compute_depreciation` |
| Policy Compliance Agent | D | In-process service, no model call: deterministic rule engine | `PolicyComplianceEvaluator` + `PolicyRuleEngine` (`backend/Features/Agents/Services/Policy/`); matches the target | Establish whether the proposed recommendation is permitted by the organisation's configured policy and by the asset's compliance state. Assembles the facts; the verdict itself is computed deterministically. | `PolicyValidationRequest { assetId, proposedRecommendation, financialAssessment }` | `PolicyValidation { verdict: PASS \| FAIL \| NEEDS_REVISION, ruleResults[]: { ruleId, expected, actual, outcome }, blockingReasons[], isHighImpact }` | `get_organization_policies`, `get_asset_compliance_state` |

**Why the Policy Agent does not decide**

The Policy Compliance Agent gathers policy parameters and compliance facts, but the PASS / FAIL / NEEDS_REVISION verdict is produced by a rule engine evaluating declarative predicates against those facts. A language model is probabilistic; a statement about whether an organisation's policy permits an irreversible action must not be. This separation guarantees, demonstrably, that the same inputs always produce the same verdict, which is why an LLM is never trusted with a compliance decision.

## 7.4 Tool Allow-List

Tools are the only mechanism by which an agent may reach system data, and every tool is read-only with a JSON-schema-validated request and response, regardless of how it's invoked. Under the target architecture (§7.2.1) a node calls its tools in-process, and the allow-list is enforced at dependency-injection registration — a node's constructor only accepts the tool-service interfaces its row in §7.3 lists, so an out-of-allow-list call is a compile error, not a runtime one. For any agent still running out-of-process (§7.3's "current implementation" column), the same tools are exposed as `/api/agent-tools/*` endpoints, authenticated as the agent service principal, with a call outside that agent's allow-list rejected by the gateway before it reaches business logic. Both paths produce the identical audit record (AI-07).

| Tool | Available to | Input schema (summary) | Returns | Side effects |
|---|---|---|---|---|
| `get_asset_type_summary` | Planner | `assetTypeId (uuid)`, `organizationId (uuid)` | Asset type, category, useful life, active asset count and condition mix. | None |
| `get_asset_summary` | Planner | `assetId (uuid)`, `organizationId (uuid)` | Code, name, type, category, status, condition, department, location, acquisition date and cost. | None |
| `get_maintenance_history` | Maintenance | `assetId`, `windowMonths (1–120)` | Completed maintenance records with dates, classification, actual cost and resulting condition. | None |
| `compute_failure_statistics` | Maintenance | `records[] (typed)` | Repair count, mean time between failures, cost trend coefficient, projected annual cost. | None — pure computation |
| `get_asset_financials` | Budget | `assetId` | Acquisition cost, accumulated depreciation, residual value, cumulative maintenance cost, replacement estimate for the asset type. | None |
| `get_department_budget_summary` | Budget | `departmentId`, `fiscalYear` | Allocated maintenance budget, committed and remaining amounts. | None |
| `compute_depreciation` | Budget | `acquisitionCost`, `acquisitionDate`, `usefulLifeYears` | Straight-line residual value at the current date. | None — pure computation |
| `get_organization_policies` | Policy | `organizationId`, `assetTypeId` | Configured thresholds and predicates: repair-to-replace ratio limit, minimum service life, maximum failure frequency, valuation requirement. | None |
| `get_asset_compliance_state` | Policy | `assetId` | Condemnation status, valuation presence and date, open maintenance and transfer counts, elapsed service life. | None |

| ID | Tool control requirement | Priority |
|---|---|---|
| AI-01 | Every tool input shall be validated against its JSON schema before dispatch; an invalid input shall be rejected without invoking the tool and shall be recorded as a tool error. | Must |
| AI-02 | Every tool output shall be validated against its response schema; an output failing validation shall be treated as a tool failure and shall not enter agent state. | Must |
| AI-03 | A tool invocation by an agent that does not hold it in its allow-list shall be refused and recorded as a security event. | Must |
| AI-04 | Tools shall be read-only; no tool shall exist that creates, updates or deletes business data. | Must |
| AI-05 | Every tool call shall be scoped to the organisation of the initiating user, taken from the persisted workflow state and never from agent-generated content. | Must |
| AI-06 | Each tool call shall be subject to a 15-second timeout and at most two retries with exponential backoff; exhaustion routes the workflow to safe failure. | Must |
| AI-07 | Each tool call shall be recorded with tool name, calling agent, input hash, outcome, duration and retry count. | Must |

## 7.5 Workflow State Persistence

Workflow state is durable, structured and inspectable. It is held in PostgreSQL — the plan, results and validation outcomes in JSONB columns for flexibility, and the queryable facts in typed columns so that dashboards and reports do not have to parse JSON.

```
  AgentWorkflows
    Id                uuid        PK
    OrganizationId    uuid        FK, indexed, global query filter
    AssetId           uuid        FK, indexed
    Objective         text        the user-supplied objective
    Status            enum        PLANNING | ANALYZING | VALIDATING |
                                  AWAITING_APPROVAL | APPROVED | REJECTED |
                                  COMPLETED_ADVISORY | REVISION_REQUESTED |
                                  FAILED_SAFE
    Plan              jsonb       ordered typed steps from the Planner
    AgentOutputs      jsonb       keyed by agent: the typed artefact each produced
    ToolCalls         jsonb       name, agent, outcome, duration, retries
    ValidationResult  jsonb       verdict + per-rule expected/actual/outcome
    Recommendation    varchar     REPAIR | REPLACE | TRANSFER | DISPOSE | RETAIN
    IsHighImpact      boolean     drives the approval interrupt
    ApprovalStatus    enum        NOT_REQUIRED | PENDING | APPROVED | REJECTED
    RevisionCount     int         capped at 2
    FailureReason     text        populated only on FAILED_SAFE
    CorrelationId     varchar     links API logs, agent logs and audit entries
    InitiatedByUserId uuid        FK
    StartedAt / CompletedAt / CreatedAt / UpdatedAt

  AgentExecutionSteps   one row per node execution: agent, sequence,
                        input hash, output summary, duration, status, error
  AgentApprovals        decision, decider, reason, decided-at, workflow snapshot
```

| ID | State requirement | Priority |
|---|---|---|
| AI-08 | The workflow identifier, objective, plan, completed steps, tool results, validation results, errors, approval status and final outcome shall be persisted durably and shall survive a restart of the agent service. | Must |
| AI-09 | A paused workflow shall be resumable from its persisted checkpoint without re-executing completed steps. | Must |
| AI-10 | Chain-of-thought, raw prompts, raw model responses, credentials and tokens shall not be persisted. Only structured artefacts and summaries are stored. | Must |
| AI-11 | Workflow state shall be scoped to the initiating organisation and shall be subject to the same global query filter as business data. | Must |
| AI-12 | The execution trace shall be sufficient to reconstruct why a recommendation was made, from the persisted artefacts alone. | Must |

## 7.6 Deterministic Validation

Between the agents' analysis and any consequence there is a deterministic gate. It runs in three stages, and a failure at any stage prevents progression.

| Stage | Check | Failure behaviour |
|---|---|---|
| 1 — Schema | Every agent artefact conforms to its declared output contract: required fields present, types correct, enumerations within range, numeric values non-negative and finite. | Fatal. Workflow terminates as FAILED_SAFE with the offending field named. No state change. |
| 2 — Business rules | The declarative rule set below is evaluated against the collected facts. | FAIL blocks the recommendation; NEEDS_REVISION returns the workflow to analysis with the failing rules as context. |
| 3 — Authorisation | The recommended action is one the initiating user could have performed manually, and the asset is in a state that permits it. | Fatal. Recorded as a security event and terminated as FAILED_SAFE. |

| Rule | Predicate | Outcome when violated |
|---|---|---|
| PR-01 | A DISPOSE recommendation requires the asset condition to be Poor or Unserviceable. | FAIL — blocking reason recorded |
| PR-02 | A DISPOSE recommendation requires elapsed service life ≥ the minimum configured for the asset type. | FAIL |
| PR-03 | A DISPOSE recommendation requires a recorded valuation with a date within the configured validity window. | NEEDS_REVISION — valuation can be obtained |
| PR-04 | A REPLACE recommendation requires repair-to-replace ratio ≥ the organisation threshold. | NEEDS_REVISION |
| PR-05 | A REPAIR recommendation requires projected repair cost ≤ available departmental budget headroom. | NEEDS_REVISION |
| PR-06 | No recommendation may be produced for an asset in a terminal state. | FAIL — fatal |
| PR-07 | No recommendation may be produced where an open maintenance or transfer record exists. | NEEDS_REVISION |
| PR-08 | Confidence below the configured floor requires human review regardless of the recommended action. | Forces `IsHighImpact = true` |
| PR-09 | DISPOSE is always high-impact and always requires approval. | n/a — sets the interrupt |

Every rule evaluation is recorded with its identifier, the expected condition, the actual value and the outcome, so that a reviewer sees not merely that validation passed but exactly what was checked and against what.

## 7.7 Human Approval Checkpoint

| ID | Requirement | Priority |
|---|---|---|
| AI-13 | A workflow whose validated recommendation is high-impact shall interrupt before any consequence, persist a checkpoint and set its status to AWAITING_APPROVAL. | Must |
| AI-14 | Only a user holding `workflow:approve` — the Administrator role — may decide a paused workflow. Any other caller receives 403. | Must |
| AI-15 | The approval interface shall present the objective, the plan, each agent's findings, the recommendation, the supporting factors and the complete rule-by-rule validation result before the decision controls. | Must |
| AI-16 | A decision shall require a recorded reason of at least 10 characters, and shall be captured with the decider, the timestamp and a snapshot of the workflow state at the moment of decision. | Must |
| AI-17 | On approval, the API — not the agent service — shall execute the authorised action through the ordinary business service, applying the identical validation, state guards and audit logging as a manual action. | Must |
| AI-18 | A paused workflow shall change no business state; the asset shall remain fully operable through ordinary interfaces while a workflow awaits approval. | Must |
| AI-19 | A workflow awaiting approval for longer than a configurable period shall be surfaced on the administrator dashboard as overdue. | Should |
| AI-20 | Revision shall be capped at two cycles, after which the workflow terminates as REVISION_REQUESTED for manual handling. | Must |

## 7.8 Observability

- Every workflow exposes an execution summary containing the objective, the plan, each agent execution with duration and status, every tool call with its outcome and timing, the full validation result, the recommendation, the approval decision and the final outcome.
- Every log entry emitted by the agent service carries the workflow identifier and the correlation identifier of the originating request, so that a single user action can be followed from the Flutter tap through the API, the agent graph, each tool call and back to the React approval screen.
- Timings are recorded per node and per tool call, supporting the agent-latency measurements required by the performance test in Section 13.5.
- Errors, retries and their outcomes are recorded as first-class state rather than only as log lines, so that failure analysis does not depend on log retention.
- Safe failures are visible in the React workflow list with their reason, and are distinguishable at a glance from rejected and completed workflows.

## 7.9 Agent Security Requirements

| ID | Requirement | Priority |
|---|---|---|
| AI-21 | The agent service shall not be reachable from the public internet; it shall accept requests only over the private network path from the API, authenticated by a shared secret supplied through environment configuration. | Must |
| AI-22 | User-supplied objective text shall be treated as data, never as instruction. It shall be length-limited, sanitised of control characters and delimited within the prompt, and the system prompt shall instruct the model to ignore instructions appearing inside it. | Must |
| AI-23 | Agent output shall never determine which tool exists, which organisation is queried or which user is acting; those are taken exclusively from persisted workflow state. | Must |
| AI-24 | A prompt-injection attempt — an objective containing instruction-like content attempting to alter tool use, scope or approval — shall be resisted, recorded as a security event, and shall not change the workflow's tool permissions or organisation scope. This is covered by a dedicated golden case. | Must |
| AI-25 | Total workflow execution shall be bounded by a 120-second timeout; exceeding it terminates the workflow as FAILED_SAFE. | Must |
| AI-26 | Model API credentials shall be held only in the agent service environment and shall never be transmitted to a client, recorded in state or written to a log. | Must |
| AI-27 | Workflow initiation shall be rate-limited per user and per organisation to prevent cost exhaustion. | Should |
| AI-28 | The agent service principal shall hold read-only tool permissions and no business permission whatsoever, verified by an automated authorisation test. | Must |

## 7.10 Safe Failure

The system's behaviour when the agentic subsystem fails is as much a requirement as its behaviour when it succeeds. Every failure mode below terminates in a recorded state, changes no business data and leaves the asset fully manageable through ordinary interfaces.

| Failure mode | Detection | Terminal state and recorded outcome |
|---|---|---|
| Agent service unreachable | HTTP connection failure at initiation. | Initiation returns 503; no workflow record is created; the user is told the evaluation service is unavailable and may retry. |
| Model provider error or rate limit | Non-success response after two retries. | FAILED_SAFE with reason MODEL_UNAVAILABLE; steps completed so far are retained for inspection. |
| Tool timeout or repeated tool failure | Timeout or two consecutive failures on one tool. | FAILED_SAFE with reason TOOL_FAILURE and the tool named. |
| Agent output fails schema validation | Stage 1 of the deterministic gate. | FAILED_SAFE with reason SCHEMA_VIOLATION and the offending field named. |
| Insufficient data to analyse | Maintenance Analysis reports `dataQuality INSUFFICIENT`. | COMPLETED_ADVISORY with recommendation RETAIN and an explicit statement that evidence was inadequate. No approval is requested. |
| Policy validation fatal failure | Stage 2 returns FAIL on a fatal rule. | FAILED_SAFE with the blocking rule identifiers recorded. |
| Revision limit exhausted | Third revision attempt. | REVISION_REQUESTED terminal state, flagged for manual handling. |
| Overall timeout | 120-second budget exceeded. | FAILED_SAFE with reason TIMEOUT and the last completed step recorded. |
| Approval never given | No decision within the configured period. | Remains AWAITING_APPROVAL and is surfaced as overdue; it never expires into an action. |

## 7.11 Worked Example — One Evaluation, End to End

This section traces a single evaluation through the workflow as it is currently implemented in `AgentWorkflowService`. It describes the requirements above at work, not new requirements. Where the current build falls short of a requirement, the trace says so.

### 7.11.1 Starting facts

| Fact | Value |
|---|---|
| Asset | `GEN-007`, asset type *Diesel Generator* |
| Condition / status | `UNSERVICEABLE` / in service (not `DISPOSED`) |
| Elapsed service life | 9 years |
| Residual book value | 0 (fully depreciated) |
| Completed repairs, last 365 days | Total actual cost 900 |
| Valuation | Recorded 3 months ago |
| Open maintenance / transfer records | 0 / 0 |
| Organisation policy | Minimum service life 7 years · repair-to-replace threshold 0.70 · valuation validity 365 days |
| Initiator | An Inventory Officer (`RoleGroups.InitiateWorkflow`) |
| Approver | An Administrator (`RoleGroups.ApproveWorkflow`, AI-14) |

### 7.11.2 Entry — initiation

```
POST /api/agent-workflows
{ "assetId": "<GEN-007>", "objective": "Evaluate lifecycle action for this generator" }
```

1. The controller resolves the caller's organisation from the token. AI-05 and AI-23 require this: the organisation is never taken from request or agent content.
2. `AgentWorkflowService` resolves the scope: it confirms the asset exists in that organisation, belongs to the asset type and is not `DISPOSED`. A disposed asset is refused with `asset_disposed`.
3. FR-068 permits only one evaluation in flight per target. If one is running, the request returns `409 workflow_already_running`.
4. An `AgentWorkflow` row is persisted with status **`PLANNING`**, a new `CorrelationId` and the initiator. AI-27 rate-limits this endpoint.

The four nodes and the gate then run inside the same request. The response is returned already routed.

### 7.11.3 Node 1 — Planner Agent (trace sequence 1)

| Stage | What happens | Result for GEN-007 |
|---|---|---|
| Scope guard (`PlannerScopeGuard`, no model) | The objective must contain a lifecycle term and none of the rejected phrases (e.g. "delete asset", "approve disposal"). | Contains "evaluate" → in scope |
| Tool `get_asset_type_summary` | Active asset count and condition mix for the type. | At least 1 active asset → continue |
| Model call (optional) | The system prompt holds the one-line agent catalogue from `AgentRegistry`. The model replies with terse JSON: `{inScope, reason, steps[]}`. | e.g. Maintenance → Budget → Policy |
| `ValidatePlan` | Checks that steps use only registry agents, that all three analysis agents are present and that dependency order holds. The gate is always appended last. If validation fails, the next provider is tried. | Plan accepted |
| Fallback | No key configured, or every provider failed → `FallbackPlan()` (registry order). | Same four steps |

The plan is persisted to `Plan`. Status becomes **`ANALYZING`**.

### 7.11.4 Node 2 — Maintenance Analysis Agent (sequence 2, no model)

`compute_failure_statistics` (`FailureStatisticsEngine`) works over completed repairs:

| Output | Rule | GEN-007 |
|---|---|---|
| Repair count | Number of completed repairs | e.g. 4 |
| MTBF | Mean days between consecutive repairs; needs ≥ 2 repairs | e.g. 85.0 days |
| Cost trend | Average cost of the newer half vs. the older half: > +10% `INCREASING`, < −10% `DECREASING`, else `STABLE` | e.g. `INCREASING` |
| Projected next-12-month cost | Sum of actual cost over the trailing 365 days | **900** |

The output is persisted to `MaintenanceAnalysis`. This node is advisory: if it fails, the failure is recorded and later nodes continue as if there were no repair history.

### 7.11.5 Node 3 — Budget Analysis Agent (sequence 3)

1. **Tools:** `get_asset_financials` returns residual value **0**. Policy supplies threshold **0.70**. `get_department_budget_summary` returns `NOT_CONFIGURED`, so budget headroom is `null`.
2. **Deterministic triage** (`BudgetTriage.TriageAsset`):
   `ratio = projected ÷ max(residual, 1) = 900 ÷ 1 = 900`. The ratio is ≥ 0.70 and residual ≤ 0, so the result is **`DISPOSE`**.
   *The other branches are `REPLACE` (ratio ≥ threshold with residual left), `REPAIR` (ratio < threshold) and `RETAIN` (no projected cost).*
3. **Deterministic ranking:** every action is scored by its share of triage outcomes. For one asset triaged `DISPOSE`, the order is DISPOSE 0.91 › RETAIN 0.10 › REPAIR 0.09 › REPLACE 0.08 › TRANSFER 0.07.
4. **Optional model re-scoring:** the model receives a fact sheet of roughly 150 tokens and returns `{scores, pick, why}`. `Merge` keeps every figure deterministic and takes only the scores and the picked option's rationale from the model. `BudgetAssessmentValidator` rejects a reply with a missing action, a score outside 0–1, or a pick that is not the top score. In that case the next provider is tried, and then the deterministic ranking is used.

The output is persisted to `BudgetAnalysis` with `Source = DETERMINISTIC | MODEL`.

### 7.11.6 Node 4 — Policy Compliance Agent (sequence 4, no model)

Status becomes **`VALIDATING`**.

1. **Tools:** `get_asset_compliance_state` and `get_organization_policies`.
2. **Condition-based proposal** (`AssetActionRecommendationEngine`): the asset is unserviceable and 9 years ≥ 7, so it proposes **`DISPOSE`**.
3. **Candidate list** (`PolicyComplianceEvaluator`): the asset is unserviceable, so its condition outranks finance and the model's pick is not used. The candidates are DISPOSE → ranked options (RETAIN, REPAIR, REPLACE, TRANSFER) → RETAIN, with duplicates removed.
4. **Rule engine** (`PolicyRuleEngine`): each candidate is evaluated in turn, and the first `PASS` is kept.

| Rule | Expected | Actual | Outcome |
|---|---|---|---|
| PR-01 | Condition is Poor or Unserviceable | UNSERVICEABLE | PASS |
| PR-02 | Elapsed service life ≥ 7 years | 9 years | PASS |
| PR-03 | Valuation within 365 days | 3 months ago | PASS |
| PR-04 | (REPLACE only) | — | N/A |
| PR-05 | (REPAIR only) | — | N/A |
| PR-06 | Not in a terminal state | in service | PASS |
| PR-07 | No open maintenance or transfer | 0 / 0 | PASS |
| PR-08 | Confidence ≥ floor | not provided (no model pick used) | PASS |
| PR-09 | DISPOSE requires approval | DISPOSE | PASS → **high impact** |

The verdict is **`PASS`**, the recommendation is **`DISPOSE`** and `IsHighImpact = true`. `ValidationResult`, `AgentOutputs` and `Recommendation` are persisted. An `AgentRecommendation` entry is appended to GEN-007's lifecycle history (FR-027).

### 7.11.7 Deterministic gate (sequence 5)

| Verdict | Routing | Applies here? |
|---|---|---|
| `FAIL` | `FAILED_SAFE`, blocking reasons recorded | |
| `NEEDS_REVISION` (automated run) | `REVISION_REQUESTED`: every permitted candidate has already been tried | |
| `PASS`, low impact | `COMPLETED_ADVISORY` | |
| `PASS`, high impact | **`AWAITING_APPROVAL`**, `ApprovalStatus = PENDING` (AI-13) | ✔ |

The `POST` returns `201 Created` with the full workflow. No business record has changed (AI-18).

### 7.11.8 Human decision

```
PATCH /api/agent-workflows/{id}/decide
{ "decision": "APPROVE", "reason": "Unserviceable since June; valuation confirms nil value." }
```

`DecideAsync` refuses any workflow that is not `AWAITING_APPROVAL`. It requires a reason of at least 10 characters (AI-16) and persists an `AgentApproval` row holding the decision, decider, reason, timestamp and a workflow snapshot.

| Decision | Result |
|---|---|
| `APPROVE` | `APPROVED` / `ApprovalStatus = APPROVED`. **Current build:** the decision is recorded, but executing the disposal through the Disposals service (AI-17) is not yet wired, so no business record is touched. |
| `REJECT` | `REJECTED`, terminal. |
| `REVISE` | `RevisionCount + 1`. The pipeline re-runs at once with the returned action (`DISPOSE`) excluded from the candidates. On the third request, the workflow ends as `REVISION_REQUESTED` (AI-20). |

### 7.11.9 Reading the trace

`GET /api/agent-workflows/{id}/execution-summary` returns the workflow together with its ordered `AgentExecutionSteps` and `AgentApprovals`. Together these satisfy AI-12: the reason for the recommendation can be rebuilt from persisted artefacts alone.

| Seq | Agent | Output summary (abridged) |
|---|---|---|
| 1 | Planner | 4 steps: Maintenance → Budget → Policy → Gate |
| 2 | Maintenance Analysis | 4 repairs · MTBF 85 d · INCREASING · 12-mo 900 |
| 3 | Budget Analysis | DISPOSE · ratio 900 · residual 0 · DETERMINISTIC |
| 4 | Policy Compliance | DISPOSE · PASS · high impact |
| 5 | Deterministic Gate | High-impact recommendation → paused for Administrator approval |

### 7.11.10 Variations on the same asset

| Change to the starting facts | Path through the workflow | Terminal state |
|---|---|---|
| Objective is "delete this asset" | The Planner scope guard rejects it before any analysis. | `FAILED_SAFE` (Planner step only) |
| No valuation recorded | DISPOSE → PR-03 `NEEDS_REVISION`. The next candidate, RETAIN, passes. The stored reason reads *"Revised from DISPOSE (PR-03) — …"*. | `COMPLETED_ADVISORY`, RETAIN |
| Only 5 years in service | The condition proposal becomes REPAIR (too early to dispose) and is tried first. PR-05 is N/A because department budgets aren't tracked, so REPAIR passes as low impact. | `COMPLETED_ADVISORY`, REPAIR |
| An open maintenance record exists | Every candidate hits PR-07 `NEEDS_REVISION`. | `REVISION_REQUESTED` |
| Asset is `DISPOSED` | Refused at initiation (`asset_disposed`). No workflow is created. | — |
| No model key configured | The Planner and Budget nodes use their deterministic fallbacks. The outcome is the same as the main trace. | `AWAITING_APPROVAL` |
| Asset-type scope (no `assetId`) | Nodes 2–4 run per asset across the fleet. The headline action is the most common passing non-RETAIN action. The workflow is high impact if any passing asset is. | As routed by the gate |
