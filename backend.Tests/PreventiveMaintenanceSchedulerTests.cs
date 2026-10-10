using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Maintenance.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Features.Maintenance;

// FR-041: an active asset whose type has a maintenance interval gets a
// REQUESTED preventive record once the interval has elapsed since its last
// repair (or acquisition), and never a second one while the first is open.
[Trait("Component", "B")]
public class PreventiveMaintenanceSchedulerTests
{
    private static readonly DateOnly Today = new(2026, 6, 1);

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly CoreGridDbContext _db;
    private readonly PreventiveMaintenanceScheduler _scheduler;

    public PreventiveMaintenanceSchedulerTests()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CoreGridDbContext(options, new NullCurrentOrganizationProvider());
        _scheduler = new PreventiveMaintenanceScheduler(_db);
    }

    private AssetType AddType(int? intervalDays)
    {
        var type = new AssetType
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, AssetCategoryId = Guid.NewGuid(),
            Code = $"T{Guid.NewGuid():N}"[..8], Name = "Generator", UsefulLifeYears = 10, DefaultMaintenanceIntervalDays = intervalDays,
        };
        _db.AssetTypes.Add(type);
        return type;
    }

    private Asset AddAsset(AssetType type, DateOnly acquired, DateOnly? lastRepair = null, string status = AssetStatuses.Active)
    {
        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, AssetTypeId = type.Id, DepartmentId = Guid.NewGuid(), LocationId = Guid.NewGuid(),
            AssetCode = $"AST-{Guid.NewGuid():N}"[..12], Name = "Backup generator", Status = status, Condition = AssetConditions.Fair,
            QrPayload = "qr", AcquisitionDate = acquired, LastRepairDate = lastRepair,
        };
        _db.Assets.Add(asset);
        return asset;
    }

    private void AddPreventiveRecord(Asset asset, MaintenanceStatus status) => _db.MaintenanceRecords.Add(new MaintenanceRecord
    {
        Id = Guid.NewGuid(), OrganizationId = _orgId, AssetId = asset.Id, Description = "Earlier service",
        ObservedCondition = AssetConditions.Fair, Type = MaintenanceType.PREVENTIVE, Priority = MaintenancePriority.MEDIUM, Status = status,
    });

    [Fact]
    public async Task CreatesARequestedPreventiveRecord_OnceTheIntervalHasElapsedSinceAcquisition()
    {
        var asset = AddAsset(AddType(intervalDays: 90), acquired: Today.AddDays(-90));
        await _db.SaveChangesAsync();

        var created = await _scheduler.ScheduleDueMaintenanceAsync(Today, CancellationToken.None);

        Assert.Equal(1, created);
        var record = await _db.MaintenanceRecords.SingleAsync();
        Assert.Equal(asset.Id, record.AssetId);
        Assert.Equal(_orgId, record.OrganizationId);
        Assert.Equal(MaintenanceType.PREVENTIVE, record.Type);
        Assert.Equal(MaintenanceStatus.REQUESTED, record.Status);
        Assert.Equal(MaintenancePriority.MEDIUM, record.Priority);
        Assert.Equal(AssetConditions.Fair, record.ObservedCondition);
        Assert.Contains("90 days", record.Description);
    }

    [Fact]
    public async Task DoesNothing_BeforeTheIntervalHasElapsed()
    {
        AddAsset(AddType(intervalDays: 90), acquired: Today.AddDays(-89));
        await _db.SaveChangesAsync();

        Assert.Equal(0, await _scheduler.ScheduleDueMaintenanceAsync(Today, CancellationToken.None));
        Assert.Empty(_db.MaintenanceRecords);
    }

    [Fact]
    public async Task MeasuresFromTheLastRepair_WhenThereIsOne()
    {
        // Acquired long ago, but repaired recently: not due yet.
        AddAsset(AddType(intervalDays: 30), acquired: Today.AddYears(-3), lastRepair: Today.AddDays(-10));
        await _db.SaveChangesAsync();

        Assert.Equal(0, await _scheduler.ScheduleDueMaintenanceAsync(Today, CancellationToken.None));
    }

    [Fact]
    public async Task SkipsAssetTypesWithNoMaintenanceInterval()
    {
        AddAsset(AddType(intervalDays: null), acquired: Today.AddYears(-5));
        await _db.SaveChangesAsync();

        Assert.Equal(0, await _scheduler.ScheduleDueMaintenanceAsync(Today, CancellationToken.None));
    }

    [Theory]
    [InlineData(AssetStatuses.UnderMaintenance)]
    [InlineData(AssetStatuses.Condemned)]
    public async Task SkipsAssetsThatAreNotActive(string status)
    {
        AddAsset(AddType(intervalDays: 30), acquired: Today.AddYears(-1), status: status);
        await _db.SaveChangesAsync();

        Assert.Equal(0, await _scheduler.ScheduleDueMaintenanceAsync(Today, CancellationToken.None));
    }

    [Theory]
    [InlineData(MaintenanceStatus.REQUESTED)]
    [InlineData(MaintenanceStatus.APPROVED)]
    [InlineData(MaintenanceStatus.IN_PROGRESS)]
    public async Task DoesNotDuplicate_WhileAPreventiveRecordIsStillOpen(MaintenanceStatus openStatus)
    {
        var asset = AddAsset(AddType(intervalDays: 30), acquired: Today.AddYears(-1));
        AddPreventiveRecord(asset, openStatus);
        await _db.SaveChangesAsync();

        Assert.Equal(0, await _scheduler.ScheduleDueMaintenanceAsync(Today, CancellationToken.None));
        Assert.Single(_db.MaintenanceRecords);
    }

    [Theory]
    [InlineData(MaintenanceStatus.COMPLETED)]
    [InlineData(MaintenanceStatus.CANCELLED)]
    public async Task SchedulesAgain_WhenThePreviousPreventiveRecordIsClosed(MaintenanceStatus closedStatus)
    {
        var asset = AddAsset(AddType(intervalDays: 30), acquired: Today.AddYears(-1));
        AddPreventiveRecord(asset, closedStatus);
        await _db.SaveChangesAsync();

        Assert.Equal(1, await _scheduler.ScheduleDueMaintenanceAsync(Today, CancellationToken.None));
    }

    [Fact]
    public async Task RunningTwice_OnTheSameDay_CreatesOnlyOneRecord()
    {
        AddAsset(AddType(intervalDays: 30), acquired: Today.AddYears(-1));
        await _db.SaveChangesAsync();

        Assert.Equal(1, await _scheduler.ScheduleDueMaintenanceAsync(Today, CancellationToken.None));
        Assert.Equal(0, await _scheduler.ScheduleDueMaintenanceAsync(Today, CancellationToken.None));
        Assert.Single(_db.MaintenanceRecords);
    }

    [Fact]
    public async Task CountsEveryDueAsset_AndLeavesTheRestAlone()
    {
        var type = AddType(intervalDays: 60);
        AddAsset(type, acquired: Today.AddDays(-60));
        AddAsset(type, acquired: Today.AddDays(-200));
        AddAsset(type, acquired: Today.AddDays(-5));
        await _db.SaveChangesAsync();

        Assert.Equal(2, await _scheduler.ScheduleDueMaintenanceAsync(Today, CancellationToken.None));
        Assert.Equal(2, await _db.MaintenanceRecords.CountAsync());
    }
}
