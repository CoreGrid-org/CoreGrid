using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.AgentTools.Services;
using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Agents.Services;
using CoreGrid.Api.Features.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace backend.Tests.Features.Agents;

// The orchestrator (SRS §7.2.1) end to end: scope resolution, the Planner's
// plan executed node by node, the deterministic gate, the trace (§9.6), and
// the human decision. Policy Compliance runs for real (rule engine and
// recommendation engine included); only the tool reads and model-backed
// nodes are mocked.
public class AgentWorkflowServiceTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly AssetCategory _category;
    private readonly AssetType _type;
    private readonly User _initiator;
    private readonly Mock<IAgentToolsService> _tools = new();
    private readonly Mock<IMaintenanceAnalysisToolsService> _maintenance = new();
    private readonly Mock<IBudgetAgentClient> _budget = new();
    private readonly Mock<IPlannerAgentClient> _planner = new();

    private static readonly OrganizationPolicyFactsDto Policy = new()
    {
        MinimumServiceLifeYears = 5, ValuationValidityWindowDays = 90, RepairToReplaceCostThreshold = 0.65m, ConfidenceFloor = 0.7m
    };

    public AgentWorkflowServiceTests()
    {
        _category = new AssetCategory { Id = Guid.NewGuid(), OrganizationId = _orgId, Code = "IT", Name = "IT Equipment" };
        _type = new AssetType { Id = Guid.NewGuid(), OrganizationId = _orgId, AssetCategoryId = _category.Id, Code = "LAP", Name = "Laptop", UsefulLifeYears = 5 };
        _initiator = new User { Id = Guid.NewGuid(), OrganizationId = _orgId, ExternalSubjectId = "sub-init", Email = "init@test.com", GivenName = "I", FamilyName = "N", Role = CoreGridRole.InventoryOfficer };

        _planner.Setup(p => p.CreatePlanAsync(It.IsAny<EvaluationScope>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FullPlan());
        _tools.Setup(t => t.GetOrganizationPoliciesAsync(_orgId, _type.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Policy);
        _maintenance.Setup(m => m.ComputeFleetFailureStatisticsAsync(_orgId, _type.Id, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private static PlannerExecutionPlan FullPlan() => new()
    {
        InScope = true,
        Steps =
        [
            new() { Seq = 1, Agent = AgentNames.MaintenanceAnalysis, Purpose = "p", ExpectedOutput = "MaintenanceAnalysis" },
            new() { Seq = 2, Agent = AgentNames.BudgetAnalysis, Purpose = "p", ExpectedOutput = "FinancialAssessment" },
            new() { Seq = 3, Agent = AgentNames.PolicyCompliance, Purpose = "p", ExpectedOutput = "PolicyValidation" },
            new() { Seq = 4, Agent = AgentNames.DeterministicGate, Purpose = "p", ExpectedOutput = "GateResult" },
        ]
    };

    private CoreGridDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new CoreGridDbContext(options, new NullCurrentOrganizationProvider());
        db.AssetCategories.Add(_category);
        db.AssetTypes.Add(_type);
        db.Users.Add(_initiator);
        db.SaveChanges();
        return db;
    }

    private Asset AddAsset(CoreGridDbContext db, string code, string condition = AssetConditions.Good)
    {
        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, AssetTypeId = _type.Id, DepartmentId = Guid.NewGuid(), LocationId = Guid.NewGuid(),
            AssetCode = code, Name = code, Status = AssetStatuses.Active, Condition = condition, QrPayload = code
        };
        db.Assets.Add(asset);
        db.SaveChanges();
        return asset;
    }

    private AgentWorkflowService CreateService(CoreGridDbContext db) => new(
        db,
        _planner.Object,
        _maintenance.Object,
        _budget.Object,
        new PolicyComplianceEvaluator(_tools.Object, new AssetActionRecommendationEngine(), new PolicyRuleEngine()));

    private void SetupCompliance(params AssetComplianceStateDto[] states) =>
        _tools.Setup(t => t.GetFleetComplianceStateAsync(_orgId, _type.Id, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(states);

    private void SetupBudget(params AssetFinancialTriageDto[] triage) =>
        _budget.Setup(b => b.RunAssessmentAsync(It.IsAny<EvaluationScope>(), It.IsAny<IReadOnlyList<FailureStatisticsDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => BudgetScopeGuard.DeterministicAssessment(triage, Policy.RepairToReplaceCostThreshold));

    private static AssetComplianceStateDto State(Asset asset, string condition = AssetConditions.Good, decimal age = 2, int openMaintenance = 0) => new()
    {
        AssetId = asset.Id, AssetCode = asset.AssetCode, CurrentStatus = AssetStatuses.Active, CurrentCondition = condition,
        ElapsedServiceLifeYears = age, OpenMaintenanceCount = openMaintenance
    };

    private static AssetFinancialTriageDto Triage(Asset asset, string action, decimal ratio = 0m, decimal projected = 0m) => new()
    {
        AssetId = asset.Id, AssetCode = asset.AssetCode, Action = action, Ratio = ratio, ProjectedCost = projected, ResidualValue = 5000m
    };

    // ── Trace ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetExecutionSummaryAsync_ReturnsStepsInSequenceOrderAndTheDecision()
    {
        await using var db = CreateDb();
        var asset = AddAsset(db, "AST-WF-1", AssetConditions.Poor);
        var decider = new User { Id = Guid.NewGuid(), OrganizationId = _orgId, ExternalSubjectId = "sub-decider", Email = "d@test.com", GivenName = "D", FamilyName = "E", Role = CoreGridRole.Administrator };
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, AssetTypeId = _type.Id, AssetId = asset.Id,
            Objective = "Evaluate for disposal", Status = WorkflowStatus.APPROVED, ApprovalStatus = ApprovalStatus.APPROVED,
            CorrelationId = "corr-1", InitiatedByUserId = _initiator.Id, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.Users.Add(decider);
        db.AgentWorkflows.Add(workflow);
        db.AgentExecutionSteps.AddRange(
            new AgentExecutionStep { Id = Guid.NewGuid(), WorkflowId = workflow.Id, Agent = AgentNames.PolicyCompliance, Sequence = 4, Status = "SUCCESS", CreatedAt = DateTimeOffset.UtcNow },
            new AgentExecutionStep { Id = Guid.NewGuid(), WorkflowId = workflow.Id, Agent = AgentNames.Planner, Sequence = 1, Status = "SUCCESS", CreatedAt = DateTimeOffset.UtcNow });
        db.AgentApprovals.Add(new AgentApproval
        {
            Id = Guid.NewGuid(), WorkflowId = workflow.Id, Decision = "APPROVE", DecidedByUserId = decider.Id,
            Reason = "Condition and elapsed service life both justify disposal.", DecidedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var summary = await CreateService(db).GetExecutionSummaryAsync(_orgId, workflow.Id, CancellationToken.None);

        Assert.NotNull(summary);
        Assert.Equal("ASSET", summary!.Workflow.Scope);
        Assert.Equal("Laptop", summary.Workflow.AssetTypeName);
        Assert.Equal("IT Equipment", summary.Workflow.CategoryName);
        Assert.Equal(new[] { AgentNames.Planner, AgentNames.PolicyCompliance }, summary.Steps.Select(s => s.Agent));
        var approval = Assert.Single(summary.Approvals);
        Assert.Equal("d@test.com", approval.DecidedByEmail);
    }

    [Fact]
    public async Task GetExecutionSummaryAsync_WorkflowNotFound_ReturnsNull()
    {
        await using var db = CreateDb();
        Assert.Null(await CreateService(db).GetExecutionSummaryAsync(_orgId, Guid.NewGuid(), CancellationToken.None));
    }

    // ── Create: scope ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateWorkflowAsync_WithoutTypeOrAsset_IsAValidationError()
    {
        await using var db = CreateDb();
        await Assert.ThrowsAsync<ValidationException>(() => CreateService(db).CreateWorkflowAsync(
            _orgId, _initiator.Id, new CreateAgentWorkflowRequest { Objective = "Evaluate lifecycle" }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateWorkflowAsync_AssetOfAnotherType_IsAValidationError()
    {
        await using var db = CreateDb();
        var asset = AddAsset(db, "AST-X");
        await Assert.ThrowsAsync<ValidationException>(() => CreateService(db).CreateWorkflowAsync(
            _orgId, _initiator.Id,
            new CreateAgentWorkflowRequest { AssetTypeId = Guid.NewGuid(), AssetId = asset.Id, Objective = "Evaluate lifecycle" },
            CancellationToken.None));
    }

    // ── Create: full pipeline ────────────────────────────────────────────────

    // The reported case: a GOOD asset with no repairs used to end NEEDS_REVISION
    // (budget proposed REPAIR, PR-05 had no headroom to compare against). Now
    // it's triaged RETAIN, passes, and completes as advisory in one request.
    [Fact]
    public async Task CreateWorkflowAsync_SingleHealthyAsset_RetainsAndCompletesAdvisory()
    {
        await using var db = CreateDb();
        var asset = AddAsset(db, "AST-OK");
        SetupCompliance(State(asset));
        SetupBudget(Triage(asset, "RETAIN"));

        var result = await CreateService(db).CreateWorkflowAsync(
            _orgId, _initiator.Id,
            new CreateAgentWorkflowRequest { AssetTypeId = _type.Id, AssetId = asset.Id, Objective = "Evaluate lifecycle" },
            CancellationToken.None);

        Assert.Equal("COMPLETED_ADVISORY", result.Status);
        Assert.Equal("RETAIN", result.Recommendation);
        Assert.Equal("PASS", result.ValidationResult!.Verdict);
        Assert.Equal("ASSET", result.Scope);

        var steps = await db.AgentExecutionSteps.Where(s => s.WorkflowId == result.Id).OrderBy(s => s.Sequence).ToListAsync();
        Assert.Equal(
            new[] { AgentNames.Planner, AgentNames.MaintenanceAnalysis, AgentNames.BudgetAnalysis, AgentNames.PolicyCompliance, AgentNames.DeterministicGate },
            steps.Select(s => s.Agent));
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, steps.Select(s => s.Sequence));
        Assert.All(steps, s => Assert.Equal("SUCCESS", s.Status));
    }

    [Fact]
    public async Task CreateWorkflowAsync_RepairBlockedByOpenRecord_RevisesToNextPermittedCandidateOrDefers()
    {
        await using var db = CreateDb();
        var repairing = AddAsset(db, "AST-REP", AssetConditions.Fair);
        SetupCompliance(State(repairing, AssetConditions.Fair, openMaintenance: 1));
        SetupBudget(Triage(repairing, "REPAIR", ratio: 0.2m, projected: 1000m));

        var result = await CreateService(db).CreateWorkflowAsync(
            _orgId, _initiator.Id,
            new CreateAgentWorkflowRequest { AssetId = repairing.Id, Objective = "Evaluate lifecycle" },
            CancellationToken.None);

        // PR-07 blocks every action while maintenance is open — nothing is
        // permitted, so it ends as REVISION_REQUESTED rather than looping.
        Assert.Equal("REVISION_REQUESTED", result.Status);
        Assert.Contains("PR-07", result.FailureReason);
        Assert.Equal(1, result.Fleet!.DeferredCount);
    }

    [Fact]
    public async Task CreateWorkflowAsync_AssetTypeFleet_EvaluatesEveryAssetAndPausesForHighImpact()
    {
        await using var db = CreateDb();
        var healthy = AddAsset(db, "LAP-001");
        var worn = AddAsset(db, "LAP-002", AssetConditions.Unserviceable);
        var busy = AddAsset(db, "LAP-003", AssetConditions.Fair);

        var wornState = State(worn, AssetConditions.Unserviceable, age: 7);
        wornState.HasValuation = true;
        wornState.ValuationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
        SetupCompliance(State(healthy), wornState, State(busy, AssetConditions.Fair, openMaintenance: 2));
        SetupBudget(Triage(healthy, "RETAIN"), Triage(worn, "DISPOSE", ratio: 3m, projected: 900m), Triage(busy, "REPAIR", 0.1m, 300m));

        var result = await CreateService(db).CreateWorkflowAsync(
            _orgId, _initiator.Id,
            new CreateAgentWorkflowRequest { AssetTypeId = _type.Id, Objective = "Evaluate lifecycle of the laptop fleet" },
            CancellationToken.None);

        Assert.Equal("ASSET_TYPE", result.Scope);
        Assert.Null(result.AssetId);
        Assert.Equal("AWAITING_APPROVAL", result.Status); // DISPOSE is always high-impact (PR-09)
        Assert.Equal("DISPOSE", result.Recommendation);
        Assert.True(result.IsHighImpact);

        var fleet = result.Fleet!;
        Assert.Equal(3, fleet.AssetCount);
        Assert.Equal(2, fleet.PassCount);
        Assert.Equal(1, fleet.DeferredCount);
        Assert.Equal(1, fleet.ActionCounts["RETAIN"]);
        Assert.Equal(1, fleet.ActionCounts["DISPOSE"]);
        Assert.Equal("LAP-002", fleet.Assets[0].AssetCode); // most consequential first

        // The fleet-wide trace is still one row per node.
        Assert.Equal(5, await db.AgentExecutionSteps.CountAsync(s => s.WorkflowId == result.Id));
        // Only the non-trivial permitted action is written to asset history.
        Assert.Equal(1, await db.AssetHistoryEntries.CountAsync(h => h.EventType == AssetHistoryEventTypes.AgentRecommendation));
    }

    [Fact]
    public async Task CreateWorkflowAsync_WhenBudgetFails_PolicyFallsBackToConditionAndStillRoutes()
    {
        await using var db = CreateDb();
        var asset = AddAsset(db, "AST-T", AssetConditions.Fair);
        SetupCompliance(State(asset, AssetConditions.Fair));
        _budget.Setup(b => b.RunAssessmentAsync(It.IsAny<EvaluationScope>(), It.IsAny<IReadOnlyList<FailureStatisticsDto>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Budget assessment LLM service timeout"));

        var result = await CreateService(db).CreateWorkflowAsync(
            _orgId, _initiator.Id, new CreateAgentWorkflowRequest { AssetId = asset.Id, Objective = "Evaluate lifecycle" }, CancellationToken.None);

        Assert.Equal("COMPLETED_ADVISORY", result.Status); // FAIR → REPAIR; PR-05 N/A without a tracked budget
        Assert.Equal("REPAIR", result.Recommendation);
        Assert.Null(result.BudgetAnalysis);

        var budgetStep = await db.AgentExecutionSteps.SingleAsync(s => s.WorkflowId == result.Id && s.Agent == AgentNames.BudgetAnalysis);
        Assert.Equal("FAILED", budgetStep.Status);
        Assert.Contains("timeout", budgetStep.Error);
    }

    [Fact]
    public async Task CreateWorkflowAsync_PlannerRejectsObjective_FailsSafeWithoutRunningNodes()
    {
        await using var db = CreateDb();
        _planner.Setup(p => p.CreatePlanAsync(It.IsAny<EvaluationScope>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlannerExecutionPlan { InScope = false, RejectionReason = "Out of scope." });

        var result = await CreateService(db).CreateWorkflowAsync(
            _orgId, _initiator.Id, new CreateAgentWorkflowRequest { AssetTypeId = _type.Id, Objective = "Delete database" }, CancellationToken.None);

        Assert.Equal("FAILED_SAFE", result.Status);
        Assert.Equal("Out of scope.", result.FailureReason);
        _budget.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWorkflowAsync_SecondRunForSameTarget_Conflicts()
    {
        await using var db = CreateDb();
        db.AgentWorkflows.Add(new AgentWorkflow
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, AssetTypeId = _type.Id, Objective = "x", Status = WorkflowStatus.AWAITING_APPROVAL,
            CorrelationId = "c", InitiatedByUserId = _initiator.Id, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => CreateService(db).CreateWorkflowAsync(
            _orgId, _initiator.Id, new CreateAgentWorkflowRequest { AssetTypeId = _type.Id, Objective = "Evaluate lifecycle" }, CancellationToken.None));
    }

    // ── Resume, re-run, decide ───────────────────────────────────────────────

    [Fact]
    public async Task ResumeAsync_StuckAnalyzingWorkflow_RunsRemainingPlanToARoutedState()
    {
        await using var db = CreateDb();
        var asset = AddAsset(db, "AST-OLD");
        SetupCompliance(State(asset));
        SetupBudget(Triage(asset, "RETAIN"));
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, AssetTypeId = _type.Id, AssetId = asset.Id, Objective = "Evaluate",
            Status = WorkflowStatus.ANALYZING, CorrelationId = "c", InitiatedByUserId = _initiator.Id,
            Plan = System.Text.Json.JsonSerializer.Serialize(FullPlan()), CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync();

        var policyAgent = new PolicyComplianceAgentService(CreateService(db));
        var result = await policyAgent.RunAsync(_orgId, workflow.Id, CancellationToken.None);

        Assert.Equal("COMPLETED_ADVISORY", result!.Status);
        _planner.Verify(p => p.CreatePlanAsync(It.IsAny<EvaluationScope>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunBudgetAnalysisAsync_ReRun_RecordsStepAtItsPlanSequence()
    {
        await using var db = CreateDb();
        var asset = AddAsset(db, "AST-B");
        SetupBudget(Triage(asset, "REPAIR", 0.35m, 1000m));
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, AssetTypeId = _type.Id, AssetId = asset.Id, Objective = "Evaluate",
            Status = WorkflowStatus.ANALYZING, CorrelationId = "c", InitiatedByUserId = _initiator.Id,
            Plan = System.Text.Json.JsonSerializer.Serialize(FullPlan()), CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync();

        var updated = await CreateService(db).RunBudgetAnalysisAsync(_orgId, workflow.Id, CancellationToken.None);

        Assert.Equal("REPAIR", updated!.BudgetAnalysis!.ProposedRecommendation);
        Assert.Null(updated.BudgetAnalysis.Assets); // per-asset rows aren't sent to clients
        Assert.Equal("ANALYZING", updated.Status);
        var step = await db.AgentExecutionSteps.SingleAsync(s => s.WorkflowId == workflow.Id);
        Assert.Equal(3, step.Sequence);
    }

    [Fact]
    public async Task DecideAsync_Revise_RerunsAnalysisWithoutReproposingTheRejectedAction()
    {
        await using var db = CreateDb();
        var asset = AddAsset(db, "AST-D", AssetConditions.Unserviceable);
        var state = State(asset, AssetConditions.Unserviceable, age: 7);
        state.HasValuation = true;
        state.ValuationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));
        SetupCompliance(state);
        SetupBudget(Triage(asset, "DISPOSE", 3m, 900m));

        var service = CreateService(db);
        var created = await service.CreateWorkflowAsync(
            _orgId, _initiator.Id, new CreateAgentWorkflowRequest { AssetId = asset.Id, Objective = "Evaluate lifecycle" }, CancellationToken.None);
        Assert.Equal("AWAITING_APPROVAL", created.Status);
        Assert.Equal("DISPOSE", created.Recommendation);

        var revised = await service.DecideAsync(
            _orgId, created.Id, Guid.NewGuid(),
            new DecideWorkflowRequest { Decision = "REVISE", Reason = "Consider keeping it in service a while longer." },
            CancellationToken.None);

        Assert.NotEqual("DISPOSE", revised!.Recommendation);
        Assert.Equal(1, revised.RevisionCount);
        Assert.NotEqual("ANALYZING", revised.Status); // re-routed in the same request
    }
}
