using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Verification.DTOs;
using CoreGrid.Api.Features.Verification.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Features.Verification;

// FR-062 (§6.7 acceptance criteria). AC1 (403 for an Inventory Officer) is
// an authorization concern, covered in AuthorizationMatrixTests; these are
// the service-level business rules.
public class DiscrepancyResolutionServiceTests
{
    private CoreGridDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CoreGridDbContext(options);
    }

    private static (Asset asset, VerificationTask task, Discrepancy discrepancy) BuildSeed(
        Guid orgId,
        DiscrepancyType type = DiscrepancyType.ConditionMismatch,
        bool taskCompletedAsMissing = false)
    {
        var assetId = Guid.NewGuid();
        var asset = new Asset
        {
            Id = assetId, OrganizationId = orgId,
            AssetCode = "AST-DISC-1", Name = "Test Asset", Status = "ACTIVE", Condition = "GOOD",
            QrPayload = "qr"
        };

        var task = new VerificationTask
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, CampaignId = Guid.NewGuid(), AssetId = assetId,
            DueDate = new DateOnly(2026, 6, 1),
            Status = taskCompletedAsMissing ? VerificationTaskStatus.Completed : VerificationTaskStatus.Pending,
            AssertedPresent = taskCompletedAsMissing ? false : null,
            AssertedCondition = "POOR"
        };

        var discrepancy = new Discrepancy
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, CampaignId = task.CampaignId, VerificationTaskId = task.Id, AssetId = assetId,
            Type = type, IsAutomatic = true, Description = "Test discrepancy", Status = DiscrepancyStatus.Open
        };

        return (asset, task, discrepancy);
    }

    [Fact]
    public async Task Resolve_WithUnrecognizedResolutionType_Throws()
    {
        var orgId = Guid.NewGuid();
        using var db = CreateInMemoryDbContext();
        var (asset, task, discrepancy) = BuildSeed(orgId);
        db.AddRange(asset, task, discrepancy);
        await db.SaveChangesAsync();

        var service = new DiscrepancyService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveAsync(
            orgId, discrepancy.Id, Guid.NewGuid(),
            new ResolveDiscrepancyRequest { ResolutionType = "NOT_A_REAL_TYPE", ResolutionExplanation = "Some explanation here" }));
    }

    // FR-062 AC3: NO_ACTION without a justification of the required length.
    [Fact]
    public async Task Resolve_NoActionWithShortJustification_Throws()
    {
        var orgId = Guid.NewGuid();
        using var db = CreateInMemoryDbContext();
        var (asset, task, discrepancy) = BuildSeed(orgId);
        db.AddRange(asset, task, discrepancy);
        await db.SaveChangesAsync();

        var service = new DiscrepancyService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveAsync(
            orgId, discrepancy.Id, Guid.NewGuid(),
            new ResolveDiscrepancyRequest { ResolutionType = "NO_ACTION", ResolutionExplanation = "Too short" }));
    }

    [Fact]
    public async Task Resolve_NoActionWithSufficientJustification_Succeeds()
    {
        var orgId = Guid.NewGuid();
        using var db = CreateInMemoryDbContext();
        var (asset, task, discrepancy) = BuildSeed(orgId);
        db.AddRange(asset, task, discrepancy);
        await db.SaveChangesAsync();

        var service = new DiscrepancyService(db);

        var result = await service.ResolveAsync(
            orgId, discrepancy.Id, Guid.NewGuid(),
            new ResolveDiscrepancyRequest
            {
                ResolutionType = "NO_ACTION",
                ResolutionExplanation = "This is a sufficiently long justification for accepting the difference."
            });

        Assert.NotNull(result);
        Assert.Equal(DiscrepancyStatus.Resolved, result!.Status);
    }

    // BR2: WRITTEN_OFF requires the asset to have been verified Missing in
    // at least one completed verification.
    [Fact]
    public async Task Resolve_WrittenOffWithoutPriorMissingVerification_Throws()
    {
        var orgId = Guid.NewGuid();
        using var db = CreateInMemoryDbContext();
        var (asset, task, discrepancy) = BuildSeed(orgId, taskCompletedAsMissing: false);
        db.AddRange(asset, task, discrepancy);
        await db.SaveChangesAsync();

        var service = new DiscrepancyService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveAsync(
            orgId, discrepancy.Id, Guid.NewGuid(),
            new ResolveDiscrepancyRequest { ResolutionType = "WRITTEN_OFF", ResolutionExplanation = "Asset cannot be located." }));
    }

    [Fact]
    public async Task Resolve_WrittenOffWithPriorMissingVerification_Succeeds()
    {
        var orgId = Guid.NewGuid();
        using var db = CreateInMemoryDbContext();
        var (asset, task, discrepancy) = BuildSeed(orgId, taskCompletedAsMissing: true);
        db.AddRange(asset, task, discrepancy);
        await db.SaveChangesAsync();

        var service = new DiscrepancyService(db);

        var result = await service.ResolveAsync(
            orgId, discrepancy.Id, Guid.NewGuid(),
            new ResolveDiscrepancyRequest { ResolutionType = "WRITTEN_OFF", ResolutionExplanation = "Asset cannot be located." });

        Assert.NotNull(result);
        Assert.Equal(DiscrepancyStatus.Resolved, result!.Status);
    }

    // FR-062 AC2 (as implemented — see DiscrepancyService.ApplyRegisterCorrection):
    // applying the correction for a ConditionMismatch writes exactly one
    // AssetHistory entry and updates the asset's condition.
    [Fact]
    public async Task Resolve_ConditionMismatchWithApplyCorrection_UpdatesAssetAndWritesOneHistoryEntry()
    {
        var orgId = Guid.NewGuid();
        using var db = CreateInMemoryDbContext();
        var (asset, task, discrepancy) = BuildSeed(orgId, DiscrepancyType.ConditionMismatch);
        db.AddRange(asset, task, discrepancy);
        await db.SaveChangesAsync();

        var service = new DiscrepancyService(db);

        var result = await service.ResolveAsync(
            orgId, discrepancy.Id, Guid.NewGuid(),
            new ResolveDiscrepancyRequest
            {
                ResolutionType = "CONDITION_UPDATED",
                ResolutionExplanation = "Register corrected to the verified condition.",
                ApplyCorrection = true
            });

        Assert.NotNull(result);
        Assert.True(result!.RegisterCorrected);

        var updatedAsset = await db.Assets.AsNoTracking().FirstAsync(a => a.Id == asset.Id);
        Assert.Equal("POOR", updatedAsset.Condition); // the task's AssertedCondition, seeded above

        var historyCount = await db.AssetHistoryEntries.CountAsync(h => h.AssetId == asset.Id);
        Assert.Equal(1, historyCount);
    }

    [Fact]
    public async Task Resolve_ApplyCorrectionOnUnsupportedType_Throws()
    {
        var orgId = Guid.NewGuid();
        using var db = CreateInMemoryDbContext();
        var (asset, task, discrepancy) = BuildSeed(orgId, DiscrepancyType.Missing);
        db.AddRange(asset, task, discrepancy);
        await db.SaveChangesAsync();

        var service = new DiscrepancyService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveAsync(
            orgId, discrepancy.Id, Guid.NewGuid(),
            new ResolveDiscrepancyRequest { ResolutionType = "WRITTEN_OFF", ResolutionExplanation = "N/A here.", ApplyCorrection = true }));
    }

    // BR3: a resolved discrepancy cannot be reopened; a new discrepancy must be raised.
    [Fact]
    public async Task Resolve_AlreadyResolvedDiscrepancy_Throws()
    {
        var orgId = Guid.NewGuid();
        using var db = CreateInMemoryDbContext();
        var (asset, task, discrepancy) = BuildSeed(orgId);
        db.AddRange(asset, task, discrepancy);
        await db.SaveChangesAsync();

        var service = new DiscrepancyService(db);
        var userId = Guid.NewGuid();

        await service.ResolveAsync(
            orgId, discrepancy.Id, userId,
            new ResolveDiscrepancyRequest { ResolutionType = "NO_ACTION", ResolutionExplanation = "First resolution, accepted as-is for testing." });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveAsync(
            orgId, discrepancy.Id, userId,
            new ResolveDiscrepancyRequest { ResolutionType = "NO_ACTION", ResolutionExplanation = "Second attempt should be rejected outright." }));
    }
}
