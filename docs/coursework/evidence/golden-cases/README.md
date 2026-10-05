# Golden-case evidence (agentic AI evaluation)

Automated evidence for the golden cases in SRS §13.4. Run on 2026-10-05 at commit `6c3adb4`:

```bash
dotnet test backend.Tests --filter "<the tests below>" --logger "console;verbosity=normal" --logger trx
```

**Result: 25 tests, 25 passed, 0 failed.**

| File | What it is |
|---|---|
| [`golden-cases-test-run.png`](golden-cases-test-run.png) | Screenshot of the run: each passing test labelled with its golden case, plus the summary |
| [`golden-cases-test-run.log`](golden-cases-test-run.log) | Full console output of the same run, unedited |
| [`golden-cases.trx`](golden-cases.trx) | The test runner's machine-readable results file (opens in Visual Studio / Rider) |

| Golden case | Test(s) | Result |
|---|---|---|
| GC-01 Correct disposal recommendation | `AgentWorkflowServiceTests.CreateWorkflowAsync_AssetTypeFleet_EvaluatesEveryAssetAndPausesForHighImpact`, `PolicyRuleEngineTests.Dispose_AlwaysSetsHighImpact` | PASS |
| GC-02 Correct repair recommendation | `PolicyRuleEngineTests.Repair_WithCostWithinBudget_PassesPR05`, `AssetActionRecommendationEngineTests.DegradedButNotUnserviceable_ProposesRepair` (POOR, FAIR), `AgentWorkflowServiceTests.CreateWorkflowAsync_SingleHealthyAsset_RetainsAndCompletesAdvisory` | PASS |
| GC-03 Policy blocks disposal | `PolicyRuleEngineTests.Dispose_WithGoodCondition_FailsPR01` | PASS |
| GC-04 Revision path | `PolicyRuleEngineTests.Dispose_WithoutValuation_NeedsRevisionOnPR03`, `AgentWorkflowServiceTests.DecideAsync_Revise_RerunsAnalysisWithoutReproposingTheRejectedAction` | PASS |
| GC-05 Insufficient data | No automated test yet | Not covered |
| GC-06 Tool allow-list | No automated test yet | Not covered |
| GC-07 Prompt injection | `AgentWorkflowServiceTests.CreateWorkflowAsync_PlannerRejectsObjective_FailsSafeWithoutRunningNodes` | PASS |
| GC-08 Schema violation | `BudgetAgentTests.RunAssessmentAsync_WhenModelPickIsNotTheTopScore_FallsBackToDeterministic` | PASS (partial: invalid model output falls back; no missing-field stub) |
| GC-09 Tool / model timeout | `BudgetAgentTests.RunAssessmentAsync_WhenHttpCallFails_ReturnsDeterministicFallback` | PASS (partial: model call failure, not a tool timeout with retries) |
| GC-10 Approval authorisation | `AuthorizationMatrixTests.DecideAgentWorkflow_EnforcesAI14_AdministratorOnly` (Auditor, Inventory Officer, Staff → 403) | PASS |
| GC-11 Approval → execution gate | `DisposalPreconditionServiceTests.CheckP6_*` (9 cases) | PASS |
| GC-12 Rejection changes nothing | No automated test yet | Not covered |
