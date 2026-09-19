using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Features.Shared;

// B8/FR-006 regression: AssetAttributeDefinition, AssetAttributeValue,
// AgentExecutionStep and AgentApproval carry no OrganizationId column of
// their own — before Phase 4's fix, querying any of them directly (rather
// than always starting from their org-filtered parent) returned every
// organization's rows. Each test here seeds two organizations, opens a
// DbContext scoped to one of them, and proves a query starting directly
// from the child DbSet still excludes the other organization's rows.
public class QueryFilterTests
{
    private static CoreGridDbContext CreateDbContext(string databaseName, Guid organizationId)
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new CoreGridDbContext(options, new FixedCurrentOrganizationProvider(organizationId));
    }

    [Fact]
    public async Task AssetAttributeDefinitions_QueriedDirectly_ExcludesAnotherOrganizations_Rows()
    {
        var databaseName = Guid.NewGuid().ToString();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        await using (var seed = CreateDbContext(databaseName, orgA))
        {
            var typeA = new AssetType { Id = Guid.NewGuid(), OrganizationId = orgA, AssetCategoryId = Guid.NewGuid(), Code = "TYA", Name = "Type A", UsefulLifeYears = 5 };
            var typeB = new AssetType { Id = Guid.NewGuid(), OrganizationId = orgB, AssetCategoryId = Guid.NewGuid(), Code = "TYB", Name = "Type B", UsefulLifeYears = 5 };
            seed.AssetTypes.AddRange(typeA, typeB);
            seed.AssetAttributeDefinitions.AddRange(
                new AssetAttributeDefinition { Id = Guid.NewGuid(), AssetTypeId = typeA.Id, Name = "Serial (org A)", DataType = "TEXT" },
                new AssetAttributeDefinition { Id = Guid.NewGuid(), AssetTypeId = typeB.Id, Name = "Serial (org B)", DataType = "TEXT" });
            await seed.SaveChangesAsync();
        }

        await using var db = CreateDbContext(databaseName, orgA);
        var definitions = await db.AssetAttributeDefinitions.ToListAsync();

        var definition = Assert.Single(definitions);
        Assert.Equal("Serial (org A)", definition.Name);
    }

    [Fact]
    public async Task AssetAttributeValues_QueriedDirectly_ExcludesAnotherOrganizations_Rows()
    {
        var databaseName = Guid.NewGuid().ToString();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        await using (var seed = CreateDbContext(databaseName, orgA))
        {
            var assetA = new Asset
            {
                Id = Guid.NewGuid(), OrganizationId = orgA, AssetTypeId = Guid.NewGuid(), DepartmentId = Guid.NewGuid(), LocationId = Guid.NewGuid(),
                AssetCode = "AST-A", Name = "Asset A", Status = AssetStatuses.Active, Condition = AssetConditions.Good, QrPayload = "qr-a",
            };
            var assetB = new Asset
            {
                Id = Guid.NewGuid(), OrganizationId = orgB, AssetTypeId = Guid.NewGuid(), DepartmentId = Guid.NewGuid(), LocationId = Guid.NewGuid(),
                AssetCode = "AST-B", Name = "Asset B", Status = AssetStatuses.Active, Condition = AssetConditions.Good, QrPayload = "qr-b",
            };
            seed.Assets.AddRange(assetA, assetB);
            seed.AssetAttributeValues.AddRange(
                new AssetAttributeValue { Id = Guid.NewGuid(), AssetId = assetA.Id, AssetAttributeDefinitionId = Guid.NewGuid(), ValueText = "org A value" },
                new AssetAttributeValue { Id = Guid.NewGuid(), AssetId = assetB.Id, AssetAttributeDefinitionId = Guid.NewGuid(), ValueText = "org B value" });
            await seed.SaveChangesAsync();
        }

        await using var db = CreateDbContext(databaseName, orgA);
        var values = await db.AssetAttributeValues.ToListAsync();

        var value = Assert.Single(values);
        Assert.Equal("org A value", value.ValueText);
    }

    [Fact]
    public async Task AgentExecutionSteps_QueriedDirectly_ExcludesAnotherOrganizations_Rows()
    {
        var databaseName = Guid.NewGuid().ToString();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        await using (var seed = CreateDbContext(databaseName, orgA))
        {
            var workflowA = NewWorkflow(orgA);
            var workflowB = NewWorkflow(orgB);
            seed.AgentWorkflows.AddRange(workflowA, workflowB);
            seed.AgentExecutionSteps.AddRange(
                new AgentExecutionStep { Id = Guid.NewGuid(), WorkflowId = workflowA.Id, Agent = AgentNames.Planner, Sequence = 1, Status = "SUCCESS" },
                new AgentExecutionStep { Id = Guid.NewGuid(), WorkflowId = workflowB.Id, Agent = AgentNames.Planner, Sequence = 1, Status = "SUCCESS" });
            await seed.SaveChangesAsync();
        }

        await using var db = CreateDbContext(databaseName, orgA);
        var steps = await db.AgentExecutionSteps.ToListAsync();

        var step = Assert.Single(steps);
        Assert.Equal(orgA, (await db.AgentWorkflows.FindAsync(step.WorkflowId))!.OrganizationId);
    }

    [Fact]
    public async Task AgentApprovals_QueriedDirectly_ExcludesAnotherOrganizations_Rows()
    {
        var databaseName = Guid.NewGuid().ToString();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        await using (var seed = CreateDbContext(databaseName, orgA))
        {
            var workflowA = NewWorkflow(orgA);
            var workflowB = NewWorkflow(orgB);
            seed.AgentWorkflows.AddRange(workflowA, workflowB);
            seed.AgentApprovals.AddRange(
                new AgentApproval { Id = Guid.NewGuid(), WorkflowId = workflowA.Id, Decision = "APPROVE", DecidedByUserId = Guid.NewGuid(), Reason = "org A approval reason" },
                new AgentApproval { Id = Guid.NewGuid(), WorkflowId = workflowB.Id, Decision = "APPROVE", DecidedByUserId = Guid.NewGuid(), Reason = "org B approval reason" });
            await seed.SaveChangesAsync();
        }

        await using var db = CreateDbContext(databaseName, orgA);
        var approvals = await db.AgentApprovals.ToListAsync();

        var approval = Assert.Single(approvals);
        Assert.Equal("org A approval reason", approval.Reason);
    }

    private static AgentWorkflow NewWorkflow(Guid organizationId) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        AssetId = Guid.NewGuid(),
        Objective = "Evaluate for disposal",
        Status = WorkflowStatus.PLANNING,
        ApprovalStatus = ApprovalStatus.NOT_REQUIRED,
        CorrelationId = Guid.NewGuid().ToString("N"),
        InitiatedByUserId = Guid.NewGuid(),
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };
}
