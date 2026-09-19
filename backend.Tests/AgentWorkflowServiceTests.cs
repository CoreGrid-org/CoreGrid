using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.Services;
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

    private static AgentWorkflowService CreateService(CoreGridDbContext db) => new(
        db,
        new Mock<IAgentToolsService>().Object,
        new Mock<IPolicyRuleEngine>().Object,
        new Mock<IPlannerAgentClient>().Object,
        new Mock<IMaintenanceAnalysisToolsService>().Object);

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
}
