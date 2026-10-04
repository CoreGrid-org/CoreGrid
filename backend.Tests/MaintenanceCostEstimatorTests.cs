using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Maintenance.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Features.Maintenance;

// GET /api/maintenance/{id}/cost-suggestion: most specific history slice with
// enough samples wins; outliers are trimmed; no history → no suggestion.
public class MaintenanceCostEstimatorTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly AssetCategory _category;
    private readonly AssetType _laptop;
    private readonly AssetType _desktop;

    public MaintenanceCostEstimatorTests()
    {
        _category = new AssetCategory { Id = Guid.NewGuid(), OrganizationId = _orgId, Code = "IT", Name = "IT" };
        _laptop = new AssetType { Id = Guid.NewGuid(), OrganizationId = _orgId, AssetCategoryId = _category.Id, Code = "LAP", Name = "Laptop", UsefulLifeYears = 4 };
        _desktop = new AssetType { Id = Guid.NewGuid(), OrganizationId = _orgId, AssetCategoryId = _category.Id, Code = "DSK", Name = "Desktop", UsefulLifeYears = 5 };
    }

    private CoreGridDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new CoreGridDbContext(options, new NullCurrentOrganizationProvider());
        db.AssetCategories.Add(_category);
        db.AssetTypes.AddRange(_laptop, _desktop);
        db.SaveChanges();
        return db;
    }

    private Asset AddAsset(CoreGridDbContext db, AssetType type)
    {
        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, AssetTypeId = type.Id, DepartmentId = Guid.NewGuid(), LocationId = Guid.NewGuid(),
            AssetCode = $"A-{Guid.NewGuid().ToString()[..6]}", Name = "x", Status = AssetStatuses.Active, Condition = AssetConditions.Good, QrPayload = "q"
        };
        db.Assets.Add(asset);
        return asset;
    }

    private MaintenanceRecord AddRecord(CoreGridDbContext db, Asset asset, decimal? actualCost, MaintenanceStatus status = MaintenanceStatus.COMPLETED, int daysAgo = 30)
    {
        var record = new MaintenanceRecord
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, AssetId = asset.Id, Type = MaintenanceType.CORRECTIVE, Priority = MaintenancePriority.MEDIUM,
            Status = status, ActualCost = actualCost, CompletionDate = actualCost.HasValue ? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-daysAgo)) : null,
            Description = "d", ObservedCondition = AssetConditions.Fair, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        db.MaintenanceRecords.Add(record);
        return record;
    }

    [Fact]
    public async Task Suggest_PrefersSameAssetTypeHistory_AndTrimsOutliers()
    {
        await using var db = CreateDb();
        var target = AddRecord(db, AddAsset(db, _laptop), null, MaintenanceStatus.REQUESTED);
        foreach (var cost in new[] { 1000m, 1100m, 1200m, 1300m, 90000m }) AddRecord(db, AddAsset(db, _laptop), cost);
        foreach (var cost in new[] { 50000m, 60000m, 70000m }) AddRecord(db, AddAsset(db, _desktop), cost);
        await db.SaveChangesAsync();

        var suggestion = await new MaintenanceCostEstimator(db).SuggestAsync(_orgId, target.Id, CancellationToken.None);

        Assert.NotNull(suggestion);
        Assert.Equal("ASSET_TYPE", suggestion!.Basis);
        Assert.Equal(4, suggestion.SampleSize); // the 90,000 outlier is trimmed
        Assert.InRange(suggestion.SuggestedCost!.Value, 1000m, 1300m);
        Assert.True(suggestion.LowCost <= suggestion.SuggestedCost && suggestion.SuggestedCost <= suggestion.HighCost);
    }

    [Fact]
    public async Task Suggest_FallsBackToCategory_WhenTheTypeHasTooLittleHistory()
    {
        await using var db = CreateDb();
        var target = AddRecord(db, AddAsset(db, _laptop), null, MaintenanceStatus.REQUESTED);
        AddRecord(db, AddAsset(db, _laptop), 1000m);
        foreach (var cost in new[] { 2000m, 2200m, 2400m }) AddRecord(db, AddAsset(db, _desktop), cost);
        await db.SaveChangesAsync();

        var suggestion = await new MaintenanceCostEstimator(db).SuggestAsync(_orgId, target.Id, CancellationToken.None);

        Assert.Equal("CATEGORY", suggestion!.Basis);
        Assert.Equal(4, suggestion.SampleSize);
    }

    [Fact]
    public async Task Suggest_WithNoHistory_ReturnsNoValueSoTheUserEntersOne()
    {
        await using var db = CreateDb();
        var target = AddRecord(db, AddAsset(db, _laptop), null, MaintenanceStatus.REQUESTED);
        await db.SaveChangesAsync();

        var suggestion = await new MaintenanceCostEstimator(db).SuggestAsync(_orgId, target.Id, CancellationToken.None);

        Assert.Null(suggestion!.SuggestedCost);
        Assert.Equal("NONE", suggestion.Basis);
    }

    [Fact]
    public async Task Suggest_UnknownRecord_ReturnsNull()
    {
        await using var db = CreateDb();
        Assert.Null(await new MaintenanceCostEstimator(db).SuggestAsync(_orgId, Guid.NewGuid(), CancellationToken.None));
    }
}
