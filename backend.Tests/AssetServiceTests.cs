using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;
using CoreGrid.Api.Features.Assets.Services;
using CoreGrid.Api.Features.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Features.Assets;

// SRS §9.2 / FR-031: standalone physical verification. No AssetServiceTests
// existed before this (docs/progress.md's own Component A section flagged
// the gap) — scoped here to VerifyAssetAsync only.
public class AssetServiceTests
{
    private static CoreGridDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CoreGridDbContext(options, new NullCurrentOrganizationProvider());
    }

    private static (Asset asset, Location location) SeedAssetAndLocation(CoreGridDbContext db, Guid orgId, Guid departmentId, string condition)
    {
        var location = new Location { Id = Guid.NewGuid(), OrganizationId = orgId, DepartmentId = departmentId, Name = "Store", Type = "store" };
        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetTypeId = Guid.NewGuid(), DepartmentId = departmentId, LocationId = location.Id,
            AssetCode = "AST-VFY-1", Name = "Laptop", Status = AssetStatuses.Active, Condition = condition, QrPayload = "qr",
        };
        db.Locations.Add(location);
        db.Assets.Add(asset);
        return (asset, location);
    }

    [Fact]
    public async Task VerifyAsset_AssertionMatchesRegister_RaisesNoDiscrepancy()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var (asset, location) = SeedAssetAndLocation(db, orgId, departmentId, AssetConditions.Good);
        await db.SaveChangesAsync();

        var service = new AssetService(db);

        var result = await service.VerifyAssetAsync(
            orgId, asset.Id, Guid.NewGuid(),
            new VerifyAssetRequest { AssertedPresent = true, AssertedLocationId = location.Id, AssertedCondition = AssetConditions.Good },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result!.RaisedDiscrepancyTypes);
        Assert.Empty(await db.Discrepancies.ToListAsync());

        var historyEntry = await db.AssetHistoryEntries.SingleAsync(h => h.AssetId == asset.Id);
        Assert.Equal(AssetHistoryEventTypes.Verification, historyEntry.EventType);
    }

    [Fact]
    public async Task VerifyAsset_ConditionMismatch_RaisesOneOpenDiscrepancyAndNeverCorrectsTheRegister()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var (asset, location) = SeedAssetAndLocation(db, orgId, departmentId, AssetConditions.Good);
        await db.SaveChangesAsync();

        var service = new AssetService(db);

        var result = await service.VerifyAssetAsync(
            orgId, asset.Id, Guid.NewGuid(),
            new VerifyAssetRequest { AssertedPresent = true, AssertedLocationId = location.Id, AssertedCondition = AssetConditions.Poor },
            CancellationToken.None);

        Assert.NotNull(result);
        var raisedType = Assert.Single(result!.RaisedDiscrepancyTypes);
        Assert.Equal(DiscrepancyType.ConditionMismatch, raisedType);

        var discrepancy = await db.Discrepancies.SingleAsync();
        Assert.Equal(DiscrepancyStatus.Open, discrepancy.Status);
        Assert.Null(discrepancy.CampaignId);
        Assert.Null(discrepancy.VerificationTaskId);
        Assert.True(discrepancy.IsAutomatic);

        // Never corrects the register directly — only ResolveDiscrepancy
        // (with ApplyCorrection) does that, same as a campaign-raised one.
        var unchangedAsset = await db.Assets.SingleAsync(a => a.Id == asset.Id);
        Assert.Equal(AssetConditions.Good, unchangedAsset.Condition);
    }

    [Fact]
    public async Task VerifyAsset_LocationMismatch_RaisesLocationMismatchDiscrepancy()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var (asset, _) = SeedAssetAndLocation(db, orgId, departmentId, AssetConditions.Good);
        var otherLocation = new Location { Id = Guid.NewGuid(), OrganizationId = orgId, DepartmentId = departmentId, Name = "Other store", Type = "store" };
        db.Locations.Add(otherLocation);
        await db.SaveChangesAsync();

        var service = new AssetService(db);

        var result = await service.VerifyAssetAsync(
            orgId, asset.Id, Guid.NewGuid(),
            new VerifyAssetRequest { AssertedPresent = true, AssertedLocationId = otherLocation.Id, AssertedCondition = AssetConditions.Good },
            CancellationToken.None);

        Assert.NotNull(result);
        var raisedType = Assert.Single(result!.RaisedDiscrepancyTypes);
        Assert.Equal(DiscrepancyType.LocationMismatch, raisedType);
    }

    [Fact]
    public async Task VerifyAsset_AssertedNotPresent_RaisesMissingDiscrepancyWithoutRequiringLocationOrCondition()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var (asset, _) = SeedAssetAndLocation(db, orgId, Guid.NewGuid(), AssetConditions.Good);
        await db.SaveChangesAsync();

        var service = new AssetService(db);

        var result = await service.VerifyAssetAsync(
            orgId, asset.Id, Guid.NewGuid(),
            new VerifyAssetRequest { AssertedPresent = false },
            CancellationToken.None);

        Assert.NotNull(result);
        var raisedType = Assert.Single(result!.RaisedDiscrepancyTypes);
        Assert.Equal(DiscrepancyType.Missing, raisedType);
    }

    [Fact]
    public async Task VerifyAsset_PresentWithoutLocationOrCondition_ThrowsValidationException()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var (asset, _) = SeedAssetAndLocation(db, orgId, Guid.NewGuid(), AssetConditions.Good);
        await db.SaveChangesAsync();

        var service = new AssetService(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.VerifyAssetAsync(orgId, asset.Id, Guid.NewGuid(), new VerifyAssetRequest { AssertedPresent = true }, CancellationToken.None));
    }

    [Fact]
    public async Task VerifyAsset_AssetNotFound_ReturnsNull()
    {
        await using var db = CreateInMemoryDbContext();
        var service = new AssetService(db);

        var result = await service.VerifyAssetAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new VerifyAssetRequest { AssertedPresent = false },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsset_FuturePurchaseDate_ThrowsValidationException()
    {
        await using var db = CreateInMemoryDbContext();
        var service = new AssetService(db);

        var request = new CreateAssetRequest
        {
            AssetTypeId = Guid.NewGuid(), DepartmentId = Guid.NewGuid(), LocationId = Guid.NewGuid(),
            Name = "Laptop", AcquisitionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), AcquisitionCost = 1000m,
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAssetAsync(Guid.NewGuid(), Guid.NewGuid(), request, CancellationToken.None));
        Assert.Contains("future", ex.Message);
    }
}
