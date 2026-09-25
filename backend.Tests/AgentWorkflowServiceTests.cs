using System.Text.Json;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.AgentTools.Services;
using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Agents.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace backend.Tests.Features.Agents;

// SRS §9.6 / §7.8: GetExecutionSummaryAsync — "Full auditable trace: plan,
// agent outputs, tool calls, validation, decision."
public class AgentWorkflowServiceTests
{
    private static CoreGridDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CoreGridDbContext(options, new NullCurrentOrganizationProvider());
    }

    private static AgentWorkflowService CreateService(
        CoreGridDbContext db,
        IAgentToolsService? agentTools = null,
        IPolicyRuleEngine? ruleEngine = null,
        IPlannerAgentClient? plannerAgent = null,
        IMaintenanceAnalysisToolsService? maintenanceAnalysisTools = null,
        IBudgetAgentClient? budgetAgent = null) => new(
        db,
        agentTools ?? new Mock<IAgentToolsService>().Object,
        ruleEngine ?? new Mock<IPolicyRuleEngine>().Object,
        plannerAgent ?? new Mock<IPlannerAgentClient>().Object,
        maintenanceAnalysisTools ?? new Mock<IMaintenanceAnalysisToolsService>().Object,
        budgetAgent ?? new Mock<IBudgetAgentClient>().Object);

    [Fact]
    public async Task GetExecutionSummaryAsync_ReturnsStepsInSequenceOrderAndTheDecision()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var deciderId = Guid.NewGuid();

        // AgentWorkflow.AssetId/InitiatedByUserId are non-nullable, so EF
        // treats those navigations as required — against InMemory
        // specifically, MapToDto's null-checked projection still needs a
        // real matching row or the whole workflow row gets silently
        // dropped by the join.
        var initiator = new User { Id = Guid.NewGuid(), OrganizationId = orgId, ExternalSubjectId = "sub-initiator", Email = "i@test.com", GivenName = "I", FamilyName = "N", Role = CoreGridRole.InventoryOfficer };
        var decider = new User { Id = deciderId, OrganizationId = orgId, ExternalSubjectId = "sub-decider", Email = "d@test.com", GivenName = "D", FamilyName = "E", Role = CoreGridRole.Administrator };
        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetTypeId = Guid.NewGuid(), DepartmentId = Guid.NewGuid(), LocationId = Guid.NewGuid(),
            AssetCode = "AST-WF-1", Name = "Server", Status = AssetStatuses.Active, Condition = AssetConditions.Poor, QrPayload = "qr",
        };

        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetId = asset.Id,
            Objective = "Evaluate for disposal", Status = WorkflowStatus.APPROVED, ApprovalStatus = ApprovalStatus.APPROVED,
            CorrelationId = "corr-1", InitiatedByUserId = initiator.Id, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
        };

        db.Users.AddRange(initiator, decider);
        db.Assets.Add(asset);
        db.AgentWorkflows.Add(workflow);
        db.AgentExecutionSteps.AddRange(
            new AgentExecutionStep { Id = Guid.NewGuid(), WorkflowId = workflow.Id, Agent = AgentNames.PolicyCompliance, Sequence = 4, Status = "SUCCESS", CreatedAt = DateTimeOffset.UtcNow },
            new AgentExecutionStep { Id = Guid.NewGuid(), WorkflowId = workflow.Id, Agent = AgentNames.Planner, Sequence = 1, Status = "SUCCESS", CreatedAt = DateTimeOffset.UtcNow });
        db.AgentApprovals.Add(new AgentApproval
        {
            Id = Guid.NewGuid(), WorkflowId = workflow.Id, Decision = "APPROVE", DecidedByUserId = deciderId,
            Reason = "Condition and elapsed service life both justify disposal.", DecidedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var summary = await service.GetExecutionSummaryAsync(orgId, workflow.Id, CancellationToken.None);

        Assert.NotNull(summary);
        Assert.Equal(workflow.Id, summary!.Workflow.Id);
        Assert.Equal(2, summary.Steps.Count);
        Assert.Equal(AgentNames.Planner, summary.Steps[0].Agent); // ordered by Sequence, not insertion order
        Assert.Equal(AgentNames.PolicyCompliance, summary.Steps[1].Agent);

        var approval = Assert.Single(summary.Approvals);
        Assert.Equal("APPROVE", approval.Decision);
        Assert.Equal("d@test.com", approval.DecidedByEmail);
    }

    [Fact]
    public async Task GetExecutionSummaryAsync_WorkflowNotFound_ReturnsNull()
    {
        await using var db = CreateInMemoryDbContext();
        var service = CreateService(db);

        var summary = await service.GetExecutionSummaryAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Null(summary);
    }

    [Fact]
    public async Task GetExecutionSummaryAsync_NoStepsOrApprovalsYet_ReturnsEmptyCollectionsNotNull()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var initiator = new User { Id = Guid.NewGuid(), OrganizationId = orgId, ExternalSubjectId = "sub-initiator-2", Email = "i2@test.com", GivenName = "I", FamilyName = "N", Role = CoreGridRole.InventoryOfficer };
        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetTypeId = Guid.NewGuid(), DepartmentId = Guid.NewGuid(), LocationId = Guid.NewGuid(),
            AssetCode = "AST-WF-2", Name = "Server", Status = AssetStatuses.Active, Condition = AssetConditions.Good, QrPayload = "qr",
        };
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetId = asset.Id,
            Objective = "Evaluate", Status = WorkflowStatus.PLANNING, ApprovalStatus = ApprovalStatus.NOT_REQUIRED,
            CorrelationId = "corr-2", InitiatedByUserId = initiator.Id, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.Users.Add(initiator);
        db.Assets.Add(asset);
        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var summary = await service.GetExecutionSummaryAsync(orgId, workflow.Id, CancellationToken.None);

        Assert.NotNull(summary);
        Assert.Empty(summary!.Steps);
        Assert.Empty(summary.Approvals);
    }

    [Fact]
    public async Task CreateWorkflowAsync_WhenInScope_ExecutesPlannerMaintenanceAndBudgetInSequence()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var assetTypeId = Guid.NewGuid();
        var initiator = new User { Id = Guid.NewGuid(), OrganizationId = orgId, ExternalSubjectId = "sub-init-3", Email = "init3@test.com", GivenName = "I", FamilyName = "N", Role = CoreGridRole.InventoryOfficer };
        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetTypeId = assetTypeId, DepartmentId = deptId, LocationId = Guid.NewGuid(),
            AssetCode = "AST-WF-3", Name = "Substation Transformer", Status = AssetStatuses.Active, Condition = AssetConditions.Poor, QrPayload = "qr3"
        };
        db.Users.Add(initiator);
        db.Assets.Add(asset);
        await db.SaveChangesAsync();

        var plannerMock = new Mock<IPlannerAgentClient>();
        plannerMock.Setup(p => p.CreatePlanAsync(asset.Id, "Assess transformer", initiator.Id, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlannerExecutionPlan
            {
                InScope = true,
                Steps = [new PlannerPlanStep { Seq = 1, Agent = "MAINTENANCE_ANALYSIS", Purpose = "Analyze failures", ExpectedOutput = "Failure stats" }]
            });

        var maintenanceMock = new Mock<IMaintenanceAnalysisToolsService>();
        var failureStats = new FailureStatisticsDto
        {
            AssetId = asset.Id,
            RepairCount = 6,
            MeanTimeBetweenFailuresDays = 32.5m,
            ProjectedNextTwelveMonthsCost = 7200m
        };
        maintenanceMock.Setup(m => m.ComputeFailureStatisticsAsync(orgId, asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureStats);

        var budgetMock = new Mock<IBudgetAgentClient>();
        var budgetResult = new FinancialAssessmentResultDto
        {
            RepairToReplaceRatio = 0.68m,
            BudgetHeadroom = 45000m,
            ProposedRecommendation = "REPLACE",
            RankedOptions =
            [
                new RankedOptionDto { Action = "REPLACE", Score = 0.85m, Rationale = "High MTBF risk" },
                new RankedOptionDto { Action = "REPAIR", Score = 0.52m, Rationale = "Exceeds repair ratio threshold" }
            ]
        };
        budgetMock.Setup(b => b.RunAssessmentAsync(orgId, asset.Id, deptId, It.IsAny<int?>(), It.IsAny<FailureStatisticsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(budgetResult);

        var service = CreateService(
            db,
            plannerAgent: plannerMock.Object,
            maintenanceAnalysisTools: maintenanceMock.Object,
            budgetAgent: budgetMock.Object);

        var request = new CreateAgentWorkflowRequest
        {
            AssetId = asset.Id,
            Objective = "Assess transformer"
        };

        var result = await service.CreateWorkflowAsync(orgId, initiator.Id, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(WorkflowStatus.ANALYZING.ToString(), result.Status);
        Assert.NotNull(result.MaintenanceAnalysis);
        Assert.NotNull(result.BudgetAnalysis);
        Assert.Equal("REPLACE", result.BudgetAnalysis.ProposedRecommendation);
        Assert.Equal(0.68m, result.BudgetAnalysis.RepairToReplaceRatio);

        // Verify steps persisted in DB with sequences 1, 2, 3
        var steps = await db.AgentExecutionSteps
            .Where(s => s.WorkflowId == result.Id)
            .OrderBy(s => s.Sequence)
            .ToListAsync();

        Assert.Equal(3, steps.Count);
        Assert.Equal(AgentNames.Planner, steps[0].Agent);
        Assert.Equal(1, steps[0].Sequence);
        Assert.Equal("SUCCESS", steps[0].Status);

        Assert.Equal(AgentNames.MaintenanceAnalysis, steps[1].Agent);
        Assert.Equal(2, steps[1].Sequence);
        Assert.Equal("SUCCESS", steps[1].Status);

        Assert.Equal(AgentNames.BudgetAnalysis, steps[2].Agent);
        Assert.Equal(3, steps[2].Sequence);
        Assert.Equal("SUCCESS", steps[2].Status);

        // Verify budget client was called with deserialized maintenance output from Node 2
        budgetMock.Verify(b => b.RunAssessmentAsync(
            orgId,
            asset.Id,
            deptId,
            It.IsAny<int?>(),
            It.Is<FailureStatisticsDto>(s => s.RepairCount == 6 && s.MeanTimeBetweenFailuresDays == 32.5m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateWorkflowAsync_WhenBudgetAnalysisFails_DegradesGracefullyWithoutAbortingWorkflow()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var initiator = new User { Id = Guid.NewGuid(), OrganizationId = orgId, ExternalSubjectId = "sub-init-4", Email = "init4@test.com", GivenName = "I", FamilyName = "N", Role = CoreGridRole.InventoryOfficer };
        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetTypeId = Guid.NewGuid(), DepartmentId = Guid.NewGuid(), LocationId = Guid.NewGuid(),
            AssetCode = "AST-WF-4", Name = "Turbine", Status = AssetStatuses.Active, Condition = AssetConditions.Fair, QrPayload = "qr4"
        };
        db.Users.Add(initiator);
        db.Assets.Add(asset);
        await db.SaveChangesAsync();

        var plannerMock = new Mock<IPlannerAgentClient>();
        plannerMock.Setup(p => p.CreatePlanAsync(asset.Id, "Assess turbine", initiator.Id, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlannerExecutionPlan { InScope = true, Steps = [] });

        var maintenanceMock = new Mock<IMaintenanceAnalysisToolsService>();
        maintenanceMock.Setup(m => m.ComputeFailureStatisticsAsync(orgId, asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FailureStatisticsDto { AssetId = asset.Id, RepairCount = 2 });

        var budgetMock = new Mock<IBudgetAgentClient>();
        budgetMock.Setup(b => b.RunAssessmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<int?>(), It.IsAny<FailureStatisticsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Budget assessment LLM service timeout"));

        var service = CreateService(
            db,
            plannerAgent: plannerMock.Object,
            maintenanceAnalysisTools: maintenanceMock.Object,
            budgetAgent: budgetMock.Object);

        var request = new CreateAgentWorkflowRequest { AssetId = asset.Id, Objective = "Assess turbine" };

        var result = await service.CreateWorkflowAsync(orgId, initiator.Id, request, CancellationToken.None);

        Assert.NotNull(result);
        // Design Decision: Graceful degradation — workflow stays in ANALYZING, not FAILED_SAFE
        Assert.Equal(WorkflowStatus.ANALYZING.ToString(), result.Status);
        Assert.Null(result.BudgetAnalysis);

        var steps = await db.AgentExecutionSteps
            .Where(s => s.WorkflowId == result.Id)
            .OrderBy(s => s.Sequence)
            .ToListAsync();

        Assert.Equal(3, steps.Count);
        Assert.Equal(AgentNames.BudgetAnalysis, steps[2].Agent);
        Assert.Equal(3, steps[2].Sequence);
        Assert.Equal("FAILED", steps[2].Status);
        Assert.Contains("Budget assessment LLM service timeout", steps[2].Error);
    }

    [Fact]
    public async Task RunBudgetAnalysisAsync_ReRunEndpoint_SuccessfullyUpdatesWorkflowAndRecordsStep()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var initiator = new User { Id = Guid.NewGuid(), OrganizationId = orgId, ExternalSubjectId = "sub-init-5", Email = "init5@test.com", GivenName = "I", FamilyName = "N", Role = CoreGridRole.InventoryOfficer };
        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetTypeId = Guid.NewGuid(), DepartmentId = deptId, LocationId = Guid.NewGuid(),
            AssetCode = "AST-WF-5", Name = "Circuit Breaker", Status = AssetStatuses.Active, Condition = AssetConditions.Poor, QrPayload = "qr5"
        };
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetId = asset.Id,
            Objective = "Evaluate breaker", Status = WorkflowStatus.ANALYZING, ApprovalStatus = ApprovalStatus.NOT_REQUIRED,
            CorrelationId = "corr-5", InitiatedByUserId = initiator.Id, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
            MaintenanceAnalysis = JsonSerializer.Serialize(new FailureStatisticsDto { AssetId = asset.Id, RepairCount = 3, MeanTimeBetweenFailuresDays = 60m })
        };
        db.Users.Add(initiator);
        db.Assets.Add(asset);
        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync();

        var budgetMock = new Mock<IBudgetAgentClient>();
        var budgetResult = new FinancialAssessmentResultDto
        {
            ProposedRecommendation = "REPAIR",
            RepairToReplaceRatio = 0.35m,
            BudgetHeadroom = 80000m,
            RankedOptions = [new RankedOptionDto { Action = "REPAIR", Score = 0.91m }]
        };
        budgetMock.Setup(b => b.RunAssessmentAsync(orgId, asset.Id, deptId, It.IsAny<int?>(), It.IsAny<FailureStatisticsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(budgetResult);

        var service = CreateService(db, budgetAgent: budgetMock.Object);

        var updated = await service.RunBudgetAnalysisAsync(orgId, workflow.Id, CancellationToken.None);

        Assert.NotNull(updated);
        Assert.NotNull(updated!.BudgetAnalysis);
        Assert.Equal("REPAIR", updated.BudgetAnalysis.ProposedRecommendation);
        Assert.Equal(0.35m, updated.BudgetAnalysis.RepairToReplaceRatio);

        var step = await db.AgentExecutionSteps
            .FirstOrDefaultAsync(s => s.WorkflowId == workflow.Id && s.Agent == AgentNames.BudgetAnalysis);
        Assert.NotNull(step);
        Assert.Equal(3, step!.Sequence);
        Assert.Equal("SUCCESS", step.Status);
    }

    [Fact]
    public async Task PolicyComplianceAgentService_ConsumesBudgetAnalysis_AndPassesFinancialFactsWithSequence4()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var assetTypeId = Guid.NewGuid();
        var initiator = new User { Id = Guid.NewGuid(), OrganizationId = orgId, ExternalSubjectId = "sub-init-6", Email = "init6@test.com", GivenName = "I", FamilyName = "N", Role = CoreGridRole.InventoryOfficer };
        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetTypeId = assetTypeId, DepartmentId = Guid.NewGuid(), LocationId = Guid.NewGuid(),
            AssetCode = "AST-WF-6", Name = "Transformer Unit", Status = AssetStatuses.Active, Condition = AssetConditions.Poor, QrPayload = "qr6"
        };
        var budgetAnalysis = new FinancialAssessmentResultDto
        {
            ProposedRecommendation = "DISPOSE",
            RepairToReplaceRatio = 0.78m,
            BudgetHeadroom = 25000m,
            RankedOptions = [new RankedOptionDto { Action = "DISPOSE", Score = 0.88m }]
        };
        var maintenanceAnalysis = new FailureStatisticsDto
        {
            AssetId = asset.Id,
            ProjectedNextTwelveMonthsCost = 9000m
        };
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetId = asset.Id,
            Objective = "Evaluate transformer", Status = WorkflowStatus.ANALYZING, ApprovalStatus = ApprovalStatus.NOT_REQUIRED,
            CorrelationId = "corr-6", InitiatedByUserId = initiator.Id, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
            MaintenanceAnalysis = JsonSerializer.Serialize(maintenanceAnalysis),
            BudgetAnalysis = JsonSerializer.Serialize(budgetAnalysis)
        };
        db.Users.Add(initiator);
        db.Assets.Add(asset);
        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync();

        var agentToolsMock = new Mock<IAgentToolsService>();
        agentToolsMock.Setup(t => t.GetAssetComplianceStateAsync(orgId, asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetComplianceStateDto
            {
                AssetId = asset.Id, CurrentStatus = "ACTIVE", CurrentCondition = "POOR", ElapsedServiceLifeYears = 7, HasValuation = true, ValuationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10))
            });

        agentToolsMock.Setup(t => t.GetOrganizationPoliciesAsync(orgId, assetTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationPolicyFactsDto
            {
                MinimumServiceLifeYears = 5, ValuationValidityWindowDays = 90, RepairToReplaceCostThreshold = 0.65m, ConfidenceFloor = 0.7m
            });

        var ruleEngineMock = new Mock<IPolicyRuleEngine>();
        ruleEngineMock.Setup(r => r.Evaluate(It.IsAny<CoreGrid.Api.Features.Agents.DTOs.PolicyEvaluationFacts>()))
            .Returns(new PolicyValidation { Verdict = "PASS", IsHighImpact = false, RuleResults = [] });

        var recEngineMock = new Mock<IAssetActionRecommendationEngine>();
        recEngineMock.Setup(r => r.Propose(It.IsAny<AssetComplianceStateDto>(), It.IsAny<OrganizationPolicyFactsDto>()))
            .Returns(new AssetActionRecommendation("REPAIR", "Default heuristic"));

        var workflowService = CreateService(db, agentTools: agentToolsMock.Object, ruleEngine: ruleEngineMock.Object);

        var policyService = new PolicyComplianceAgentService(db, agentToolsMock.Object, recEngineMock.Object, workflowService);

        var result = await policyService.RunAsync(orgId, workflow.Id, CancellationToken.None);

        Assert.NotNull(result);

        // Verify Node 4 consumed Node 3's recommendation and financial facts
        ruleEngineMock.Verify(r => r.Evaluate(It.Is<CoreGrid.Api.Features.Agents.DTOs.PolicyEvaluationFacts>(f =>
            f.ProposedRecommendation == "DISPOSE" &&
            f.RepairToReplaceRatio == 0.78m &&
            f.BudgetHeadroom == 25000m &&
            f.ProjectedRepairCost == 9000m &&
            f.Confidence == 0.88m)), Times.Once);

        // Verify Sequence = 4 steps persisted
        var steps = await db.AgentExecutionSteps
            .Where(s => s.WorkflowId == workflow.Id)
            .OrderBy(s => s.Sequence)
            .ToListAsync();

        Assert.Contains(steps, s => s.Agent == AgentNames.PolicyComplianceRecommendation && s.Sequence == 4);
        Assert.Contains(steps, s => s.Agent == AgentNames.PolicyCompliance && s.Sequence == 4);
    }

    [Fact]
    public async Task PolicyComplianceAgentService_WhenBudgetAnalysisAbsent_GracefullyPassesNullFinancialAssessment()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var assetTypeId = Guid.NewGuid();
        var initiator = new User { Id = Guid.NewGuid(), OrganizationId = orgId, ExternalSubjectId = "sub-init-7", Email = "init7@test.com", GivenName = "I", FamilyName = "N", Role = CoreGridRole.InventoryOfficer };
        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetTypeId = assetTypeId, DepartmentId = Guid.NewGuid(), LocationId = Guid.NewGuid(),
            AssetCode = "AST-WF-7", Name = "Generator", Status = AssetStatuses.Active, Condition = AssetConditions.Good, QrPayload = "qr7"
        };
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetId = asset.Id,
            Objective = "Evaluate generator", Status = WorkflowStatus.ANALYZING, ApprovalStatus = ApprovalStatus.NOT_REQUIRED,
            CorrelationId = "corr-7", InitiatedByUserId = initiator.Id, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
            BudgetAnalysis = null
        };
        db.Users.Add(initiator);
        db.Assets.Add(asset);
        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync();

        var agentToolsMock = new Mock<IAgentToolsService>();
        agentToolsMock.Setup(t => t.GetAssetComplianceStateAsync(orgId, asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetComplianceStateDto
            {
                AssetId = asset.Id, CurrentStatus = "ACTIVE", CurrentCondition = "GOOD", ElapsedServiceLifeYears = 2, HasValuation = false
            });

        agentToolsMock.Setup(t => t.GetOrganizationPoliciesAsync(orgId, assetTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationPolicyFactsDto
            {
                MinimumServiceLifeYears = 5, ValuationValidityWindowDays = 90, RepairToReplaceCostThreshold = 0.65m, ConfidenceFloor = 0.7m
            });

        var ruleEngineMock = new Mock<IPolicyRuleEngine>();
        ruleEngineMock.Setup(r => r.Evaluate(It.IsAny<CoreGrid.Api.Features.Agents.DTOs.PolicyEvaluationFacts>()))
            .Returns(new PolicyValidation { Verdict = "PASS", IsHighImpact = false, RuleResults = [] });

        var recEngineMock = new Mock<IAssetActionRecommendationEngine>();
        recEngineMock.Setup(r => r.Propose(It.IsAny<AssetComplianceStateDto>(), It.IsAny<OrganizationPolicyFactsDto>()))
            .Returns(new AssetActionRecommendation("MAINTAIN", "Asset is in good health"));

        var workflowService = CreateService(db, agentTools: agentToolsMock.Object, ruleEngine: ruleEngineMock.Object);

        var policyService = new PolicyComplianceAgentService(db, agentToolsMock.Object, recEngineMock.Object, workflowService);

        var result = await policyService.RunAsync(orgId, workflow.Id, CancellationToken.None);

        Assert.NotNull(result);

        // Verify fallback to heuristic recommendation and null financial facts
        ruleEngineMock.Verify(r => r.Evaluate(It.Is<CoreGrid.Api.Features.Agents.DTOs.PolicyEvaluationFacts>(f =>
            f.ProposedRecommendation == "MAINTAIN" &&
            f.RepairToReplaceRatio == null &&
            f.BudgetHeadroom == null &&
            f.Confidence == null)), Times.Once);

        var steps = await db.AgentExecutionSteps
            .Where(s => s.WorkflowId == workflow.Id)
            .OrderBy(s => s.Sequence)
            .ToListAsync();

        Assert.Contains(steps, s => s.Agent == AgentNames.PolicyComplianceRecommendation && s.Sequence == 4);
        Assert.Contains(steps, s => s.Agent == AgentNames.PolicyCompliance && s.Sequence == 4);
    }
}
