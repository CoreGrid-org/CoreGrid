using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Maintenance.DTOs;
using CoreGrid.Api.Features.Maintenance.Services;
using CoreGrid.Api.Features.Notifications.Services;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Scoping;
using CoreGrid.Api.Features.Shared.Storage;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace backend.Tests.Features.Maintenance;

// The maintenance state machine end to end (SRS §9.3, Fig. 7):
// REQUESTED -> APPROVED -> IN_PROGRESS -> COMPLETED, with CANCELLED reachable
// from any non-terminal state. Covers fault reporting (FR-033/034), direct
// creation, approval (FR-036), start (FR-037/039), the business-specific
// completion operation (FR-038/040, BR1-BR3), cancellation notifications
// (FR-080) and the list filters (FR-042).
[Trait("Component", "B")]
public class MaintenanceLifecycleTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly CoreGridDbContext _db;
    private readonly Mock<INotificationService> _notifications = new();
    private readonly MaintenanceService _service;

    public MaintenanceLifecycleTests()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CoreGridDbContext(options, new NullCurrentOrganizationProvider());
        _service = new MaintenanceService(_db, _notifications.Object, new Mock<IFileStorageService>().Object);
    }

    private Asset AddAsset(string code = "AST-1", Guid? departmentId = null, string status = AssetStatuses.Active, Guid? orgId = null)
    {
        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = orgId ?? _orgId, AssetTypeId = Guid.NewGuid(),
            DepartmentId = departmentId ?? Guid.NewGuid(), LocationId = Guid.NewGuid(),
            AssetCode = code, Name = "Infusion Pump", Status = status, Condition = AssetConditions.Good, QrPayload = "qr",
            AcquisitionDate = new DateOnly(2022, 1, 1),
        };
        _db.Assets.Add(asset);
        return asset;
    }

    private User AddUser(Guid? orgId = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(), OrganizationId = orgId ?? _orgId, ExternalSubjectId = $"sub-{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@example.test", GivenName = "Test", FamilyName = "User",
            Role = CoreGridRole.InventoryOfficer, CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Users.Add(user);
        return user;
    }

    private MaintenanceRecord AddRecord(Asset asset, MaintenanceStatus status, Guid? assigneeId = null, decimal? estimatedCost = null,
        Guid? reporterId = null, MaintenanceType type = MaintenanceType.CORRECTIVE, MaintenancePriority priority = MaintenancePriority.MEDIUM,
        DateTimeOffset? createdAt = null)
    {
        var record = new MaintenanceRecord
        {
            Id = Guid.NewGuid(), OrganizationId = asset.OrganizationId, AssetId = asset.Id, Description = "Pump alarm keeps sounding",
            ObservedCondition = AssetConditions.Poor, Type = type, Priority = priority, Status = status,
            AssigneeId = assigneeId, EstimatedCost = estimatedCost, ReportedByUserId = reporterId,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        };
        _db.MaintenanceRecords.Add(record);
        return record;
    }

    private static CompleteMaintenanceRequest Completion(decimal actualCost = 100m, string condition = "good", string? justification = null) => new()
    {
        ActualCost = actualCost,
        WorkPerformed = "  Replaced the faulty pressure sensor.  ",
        CompletionDate = DateOnly.FromDateTime(DateTime.UtcNow),
        ResultingCondition = condition,
        OverspendJustification = justification,
    };

    // ---- Report fault (FR-033 / FR-034) ----

    [Fact]
    public async Task ReportFaultAsync_CreatesARequestedCorrectiveRecord_AttributedToTheReporter()
    {
        var asset = AddAsset();
        await _db.SaveChangesAsync();
        var photoKey = $"{PhotoKeys.MaintenanceFolder(_orgId)}/abc/photo.jpg";

        var result = await _service.ReportFaultAsync(_orgId, DepartmentScope.Unrestricted, _actorId,
            new ReportFaultRequest { AssetId = asset.Id, Description = "  Screen flickers  ", ObservedCondition = "fair", PhotoUrl = photoKey },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(MaintenanceStatus.REQUESTED, result.Status);
        Assert.Equal(MaintenanceType.CORRECTIVE, result.Type);
        Assert.Equal(MaintenancePriority.MEDIUM, result.Priority);
        Assert.Equal("Screen flickers", result.Description);
        Assert.Equal(AssetConditions.Fair, result.ObservedCondition);
        Assert.Equal(_actorId, result.ReportedByUserId);

        var stored = await _db.MaintenanceRecords.SingleAsync();
        Assert.Equal(photoKey, stored.PhotoObjectKey);
    }

    [Fact]
    public async Task ReportFaultAsync_ForAnAssetInAnotherDepartment_IsRejectedAsNotFound_ForRestrictedStaff()
    {
        var asset = AddAsset(departmentId: Guid.NewGuid());
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.ReportFaultAsync(
            _orgId, DepartmentScope.Restricted(Guid.NewGuid()), _actorId,
            new ReportFaultRequest { AssetId = asset.Id, Description = "Broken", ObservedCondition = "POOR" },
            CancellationToken.None));

        Assert.True(ex.Errors.ContainsKey(nameof(ReportFaultRequest.AssetId)));
        Assert.Empty(_db.MaintenanceRecords);
    }

    [Fact]
    public async Task ReportFaultAsync_ForAnotherOrganisationsAsset_IsRejected()
    {
        var foreignAsset = AddAsset(orgId: Guid.NewGuid());
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<ValidationException>(() => _service.ReportFaultAsync(
            _orgId, DepartmentScope.Unrestricted, _actorId,
            new ReportFaultRequest { AssetId = foreignAsset.Id, Description = "Broken", ObservedCondition = "POOR" },
            CancellationToken.None));
    }

    [Theory]
    [InlineData("BROKEN")]
    [InlineData("")]
    [InlineData("excellent")]
    public async Task ReportFaultAsync_WithAnUnknownCondition_IsRejected(string condition)
    {
        var asset = AddAsset();
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.ReportFaultAsync(
            _orgId, DepartmentScope.Unrestricted, _actorId,
            new ReportFaultRequest { AssetId = asset.Id, Description = "Broken", ObservedCondition = condition },
            CancellationToken.None));

        Assert.True(ex.Errors.ContainsKey(nameof(ReportFaultRequest.ObservedCondition)));
    }

    [Fact]
    public async Task ReportFaultAsync_WithAPhotoKeyFromAnotherOrganisation_IsRejected()
    {
        var asset = AddAsset();
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.ReportFaultAsync(
            _orgId, DepartmentScope.Unrestricted, _actorId,
            new ReportFaultRequest
            {
                AssetId = asset.Id, Description = "Broken", ObservedCondition = "POOR",
                PhotoUrl = $"{PhotoKeys.MaintenanceFolder(Guid.NewGuid())}/abc/photo.jpg",
            },
            CancellationToken.None));

        Assert.True(ex.Errors.ContainsKey(nameof(ReportFaultRequest.PhotoUrl)));
    }

    // ---- Create (Officer) ----

    [Fact]
    public async Task CreateMaintenanceAsync_StoresTheGivenTypePriorityCostAndAssignee()
    {
        var asset = AddAsset();
        var assignee = AddUser();
        await _db.SaveChangesAsync();

        var result = await _service.CreateMaintenanceAsync(_orgId, _actorId, new CreateMaintenanceRequest
        {
            AssetId = asset.Id, Description = "Quarterly calibration", ObservedCondition = "GOOD",
            Type = MaintenanceType.PREVENTIVE, Priority = MaintenancePriority.HIGH, EstimatedCost = 250m, AssigneeId = assignee.Id,
        }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(MaintenanceStatus.REQUESTED, result.Status);
        Assert.Equal(MaintenanceType.PREVENTIVE, result.Type);
        Assert.Equal(MaintenancePriority.HIGH, result.Priority);
        Assert.Equal(250m, result.EstimatedCost);
        Assert.Equal(assignee.Id, result.AssigneeId);
        Assert.Null(result.ReportedByUserId);
    }

    [Fact]
    public async Task CreateMaintenanceAsync_WithAnAssigneeFromAnotherOrganisation_IsRejected()
    {
        var asset = AddAsset();
        var outsider = AddUser(orgId: Guid.NewGuid());
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateMaintenanceAsync(_orgId, _actorId, new CreateMaintenanceRequest
        {
            AssetId = asset.Id, Description = "Calibration", ObservedCondition = "GOOD",
            Type = MaintenanceType.PREVENTIVE, Priority = MaintenancePriority.LOW, AssigneeId = outsider.Id,
        }, CancellationToken.None));

        Assert.True(ex.Errors.ContainsKey(nameof(CreateMaintenanceRequest.AssigneeId)));
        Assert.Empty(_db.MaintenanceRecords);
    }

    [Fact]
    public async Task CreateMaintenanceAsync_ForAnUnknownAsset_IsRejected()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateMaintenanceAsync(_orgId, _actorId, new CreateMaintenanceRequest
        {
            AssetId = Guid.NewGuid(), Description = "Calibration", ObservedCondition = "GOOD",
            Type = MaintenanceType.PREVENTIVE, Priority = MaintenancePriority.LOW,
        }, CancellationToken.None));
    }

    // ---- Approve (FR-036) ----

    [Theory]
    [InlineData(MaintenanceStatus.APPROVED)]
    [InlineData(MaintenanceStatus.IN_PROGRESS)]
    [InlineData(MaintenanceStatus.COMPLETED)]
    [InlineData(MaintenanceStatus.CANCELLED)]
    public async Task ApproveMaintenanceAsync_FromAnyStatusOtherThanRequested_IsAConflict(MaintenanceStatus status)
    {
        var asset = AddAsset();
        var assignee = AddUser();
        var record = AddRecord(asset, status);
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.ApproveMaintenanceAsync(_orgId, _actorId, record.Id,
            new ApproveMaintenanceRequest { AssigneeId = assignee.Id, EstimatedCost = 10m }, CancellationToken.None));

        Assert.Equal("invalid_status_transition", ex.Code);
    }

    [Fact]
    public async Task ApproveMaintenanceAsync_WithAnAssigneeOutsideTheOrganisation_IsRejected_AndLeavesTheRecordRequested()
    {
        var asset = AddAsset();
        var outsider = AddUser(orgId: Guid.NewGuid());
        var record = AddRecord(asset, MaintenanceStatus.REQUESTED);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<ValidationException>(() => _service.ApproveMaintenanceAsync(_orgId, _actorId, record.Id,
            new ApproveMaintenanceRequest { AssigneeId = outsider.Id, EstimatedCost = 10m }, CancellationToken.None));

        Assert.Equal(MaintenanceStatus.REQUESTED, (await _db.MaintenanceRecords.SingleAsync()).Status);
    }

    [Fact]
    public async Task ApproveMaintenanceAsync_UnknownRecord_ThrowsNotFound()
    {
        var assignee = AddUser();
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() => _service.ApproveMaintenanceAsync(_orgId, _actorId, Guid.NewGuid(),
            new ApproveMaintenanceRequest { AssigneeId = assignee.Id, EstimatedCost = 10m }, CancellationToken.None));
    }

    [Fact]
    public async Task ApproveMaintenanceAsync_RecordsTheAssigneeAndEstimate_AndNotifiesTheAssignee()
    {
        var asset = AddAsset("AST-APPROVE");
        var assignee = AddUser();
        var record = AddRecord(asset, MaintenanceStatus.REQUESTED);
        await _db.SaveChangesAsync();

        var result = await _service.ApproveMaintenanceAsync(_orgId, _actorId, record.Id,
            new ApproveMaintenanceRequest { AssigneeId = assignee.Id, EstimatedCost = 480m }, CancellationToken.None);

        Assert.Equal(MaintenanceStatus.APPROVED, result!.Status);
        Assert.Equal(assignee.Id, result.AssigneeId);
        Assert.Equal(assignee.Email, result.AssigneeEmail);
        Assert.Equal(480m, result.EstimatedCost);
        _notifications.Verify(n => n.NotifyAsync(_orgId, assignee.Id, NotificationTypes.MaintenanceAssigned,
            It.IsAny<string>(), It.Is<string>(m => m.Contains("AST-APPROVE")), "MaintenanceRecord", record.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveMaintenanceAsync_WithNoFaultReporter_SendsOnlyTheAssignmentNotification()
    {
        var asset = AddAsset();
        var assignee = AddUser();
        var record = AddRecord(asset, MaintenanceStatus.REQUESTED, reporterId: null);
        await _db.SaveChangesAsync();

        await _service.ApproveMaintenanceAsync(_orgId, _actorId, record.Id,
            new ApproveMaintenanceRequest { AssigneeId = assignee.Id, EstimatedCost = 1m }, CancellationToken.None);

        _notifications.Verify(n => n.NotifyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), NotificationTypes.MaintenanceStatusChanged,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _notifications.Verify(n => n.NotifyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- Start (FR-037 / FR-039) ----

    [Fact]
    public async Task StartMaintenanceAsync_MovesToInProgress_PutsTheAssetUnderMaintenance_AndWritesHistory()
    {
        var asset = AddAsset();
        var assignee = AddUser();
        var reporterId = Guid.NewGuid();
        var record = AddRecord(asset, MaintenanceStatus.APPROVED, assigneeId: assignee.Id, reporterId: reporterId);
        await _db.SaveChangesAsync();

        var result = await _service.StartMaintenanceAsync(_orgId, _actorId, record.Id, CancellationToken.None);

        Assert.Equal(MaintenanceStatus.IN_PROGRESS, result!.Status);
        var updatedAsset = await _db.Assets.SingleAsync();
        Assert.Equal(AssetStatuses.UnderMaintenance, updatedAsset.Status);
        Assert.Equal(_actorId, updatedAsset.UpdatedBy);

        var history = await _db.AssetHistoryEntries.SingleAsync();
        Assert.Equal(AssetHistoryEventTypes.Maintenance, history.EventType);
        Assert.Contains(AssetStatuses.Active, history.PreviousValue);
        Assert.Contains(AssetStatuses.UnderMaintenance, history.NewValue);

        _notifications.Verify(n => n.NotifyAsync(_orgId, reporterId, NotificationTypes.MaintenanceStatusChanged,
            It.IsAny<string>(), It.Is<string>(m => m.Contains("IN_PROGRESS")), "MaintenanceRecord", record.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartMaintenanceAsync_WithoutAnAssignee_IsAConflict()
    {
        var asset = AddAsset();
        var record = AddRecord(asset, MaintenanceStatus.APPROVED, assigneeId: null);
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.StartMaintenanceAsync(_orgId, _actorId, record.Id, CancellationToken.None));

        Assert.Equal("missing_assignee", ex.Code);
        Assert.Equal(AssetStatuses.Active, (await _db.Assets.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(MaintenanceStatus.REQUESTED)]
    [InlineData(MaintenanceStatus.IN_PROGRESS)]
    [InlineData(MaintenanceStatus.COMPLETED)]
    [InlineData(MaintenanceStatus.CANCELLED)]
    public async Task StartMaintenanceAsync_FromAnyStatusOtherThanApproved_IsAConflict(MaintenanceStatus status)
    {
        var asset = AddAsset();
        var record = AddRecord(asset, status, assigneeId: Guid.NewGuid());
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.StartMaintenanceAsync(_orgId, _actorId, record.Id, CancellationToken.None));

        Assert.Equal("invalid_status_transition", ex.Code);
    }

    // ---- Complete: the Component B business-specific operation (FR-038 / FR-040) ----

    [Fact]
    public async Task CompleteMaintenanceAsync_CompletesTheRecord_AndUpdatesTheAssetsRunningTotals()
    {
        var asset = AddAsset(status: AssetStatuses.UnderMaintenance);
        asset.CumulativeMaintenanceCost = 300m;
        asset.RepairCount = 2;
        var record = AddRecord(asset, MaintenanceStatus.IN_PROGRESS, assigneeId: Guid.NewGuid());
        await _db.SaveChangesAsync();
        var request = Completion(actualCost: 150m, condition: "good");

        var result = await _service.CompleteMaintenanceAsync(_orgId, _actorId, record.Id, request, CancellationToken.None);

        Assert.Equal(MaintenanceStatus.COMPLETED, result!.Status);
        Assert.Equal(150m, result.ActualCost);
        Assert.Equal("Replaced the faulty pressure sensor.", result.WorkPerformed);
        Assert.Equal(AssetConditions.Good, result.ResultingCondition);
        Assert.Equal(request.CompletionDate, result.CompletionDate);

        var updatedAsset = await _db.Assets.SingleAsync();
        Assert.Equal(450m, updatedAsset.CumulativeMaintenanceCost);
        Assert.Equal(3, updatedAsset.RepairCount);
        Assert.Equal(request.CompletionDate, updatedAsset.LastRepairDate);
        Assert.Equal(AssetConditions.Good, updatedAsset.Condition);
        Assert.Equal(AssetStatuses.Active, updatedAsset.Status);
    }

    // BR2: an UNSERVICEABLE result condemns the asset instead of returning it to service.
    [Fact]
    public async Task CompleteMaintenanceAsync_WithAnUnserviceableResult_CondemnsTheAsset()
    {
        var asset = AddAsset(status: AssetStatuses.UnderMaintenance);
        var record = AddRecord(asset, MaintenanceStatus.IN_PROGRESS);
        await _db.SaveChangesAsync();

        await _service.CompleteMaintenanceAsync(_orgId, _actorId, record.Id, Completion(condition: "unserviceable"), CancellationToken.None);

        var updatedAsset = await _db.Assets.SingleAsync();
        Assert.Equal(AssetStatuses.Condemned, updatedAsset.Status);
        Assert.Equal(AssetConditions.Unserviceable, updatedAsset.Condition);
    }

    [Fact]
    public async Task CompleteMaintenanceAsync_WritesOneHistoryEntry_WithBeforeAndAfterTotals()
    {
        var asset = AddAsset(status: AssetStatuses.UnderMaintenance);
        asset.CumulativeMaintenanceCost = 100m;
        asset.RepairCount = 1;
        var record = AddRecord(asset, MaintenanceStatus.IN_PROGRESS);
        await _db.SaveChangesAsync();

        await _service.CompleteMaintenanceAsync(_orgId, _actorId, record.Id, Completion(actualCost: 50m), CancellationToken.None);

        var history = await _db.AssetHistoryEntries.SingleAsync();
        Assert.Equal(AssetHistoryEventTypes.Maintenance, history.EventType);
        Assert.Equal(_actorId, history.ActorUserId);
        Assert.Contains("\"cumulativeMaintenanceCost\":100", history.PreviousValue);
        Assert.Contains("\"repairCount\":1", history.PreviousValue);
        Assert.Contains("\"cumulativeMaintenanceCost\":150", history.NewValue);
        Assert.Contains("\"repairCount\":2", history.NewValue);
    }

    [Fact]
    public async Task CompleteMaintenanceAsync_TwiceOnTheSameRecord_IsAnAlreadyCompletedConflict()
    {
        var asset = AddAsset(status: AssetStatuses.UnderMaintenance);
        var record = AddRecord(asset, MaintenanceStatus.IN_PROGRESS);
        await _db.SaveChangesAsync();
        await _service.CompleteMaintenanceAsync(_orgId, _actorId, record.Id, Completion(), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _service.CompleteMaintenanceAsync(_orgId, _actorId, record.Id, Completion(), CancellationToken.None));

        Assert.Equal("already_completed", ex.Code);
        Assert.Equal(1, (await _db.Assets.SingleAsync()).RepairCount);
    }

    [Theory]
    [InlineData(MaintenanceStatus.REQUESTED)]
    [InlineData(MaintenanceStatus.APPROVED)]
    [InlineData(MaintenanceStatus.CANCELLED)]
    public async Task CompleteMaintenanceAsync_FromANonInProgressStatus_IsAConflict(MaintenanceStatus status)
    {
        var asset = AddAsset();
        var record = AddRecord(asset, status);
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _service.CompleteMaintenanceAsync(_orgId, _actorId, record.Id, Completion(), CancellationToken.None));

        Assert.Equal("invalid_status_transition", ex.Code);
    }

    [Fact]
    public async Task CompleteMaintenanceAsync_WithAFutureCompletionDate_IsRejected()
    {
        var asset = AddAsset(status: AssetStatuses.UnderMaintenance);
        var record = AddRecord(asset, MaintenanceStatus.IN_PROGRESS);
        await _db.SaveChangesAsync();
        var request = Completion();
        request.CompletionDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CompleteMaintenanceAsync(_orgId, _actorId, record.Id, request, CancellationToken.None));

        Assert.True(ex.Errors.ContainsKey(nameof(CompleteMaintenanceRequest.CompletionDate)));
        Assert.Equal(MaintenanceStatus.IN_PROGRESS, (await _db.MaintenanceRecords.SingleAsync()).Status);
    }

    [Fact]
    public async Task CompleteMaintenanceAsync_WithAnInvalidResultingCondition_IsRejected()
    {
        var asset = AddAsset(status: AssetStatuses.UnderMaintenance);
        var record = AddRecord(asset, MaintenanceStatus.IN_PROGRESS);
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CompleteMaintenanceAsync(_orgId, _actorId, record.Id, Completion(condition: "fixed"), CancellationToken.None));

        Assert.True(ex.Errors.ContainsKey(nameof(CompleteMaintenanceRequest.ResultingCondition)));
    }

    [Fact]
    public async Task CompleteMaintenanceAsync_UnknownRecord_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CompleteMaintenanceAsync(_orgId, _actorId, Guid.NewGuid(), Completion(), CancellationToken.None));
    }

    // BR1: an overrun above the organisation's variance tolerance needs a justification.
    [Fact]
    public async Task CompleteMaintenanceAsync_OverTheVarianceTolerance_WithoutJustification_IsRejected_AndChangesNothing()
    {
        var asset = AddAsset(status: AssetStatuses.UnderMaintenance);
        var record = AddRecord(asset, MaintenanceStatus.IN_PROGRESS, estimatedCost: 100m);
        _db.OrganizationPolicies.Add(new OrganizationPolicy { Id = Guid.NewGuid(), OrganizationId = _orgId, CostVarianceTolerancePercent = 10m });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CompleteMaintenanceAsync(_orgId, _actorId, record.Id, Completion(actualCost: 120m), CancellationToken.None));

        Assert.Equal("cost_variance_exceeded", ex.Code);
        Assert.Equal(MaintenanceStatus.IN_PROGRESS, (await _db.MaintenanceRecords.SingleAsync()).Status);
        Assert.Equal(0, (await _db.Assets.SingleAsync()).RepairCount);
        Assert.Empty(_db.AssetHistoryEntries);
    }

    [Fact]
    public async Task CompleteMaintenanceAsync_OverTheVarianceTolerance_WithJustification_Succeeds()
    {
        var asset = AddAsset(status: AssetStatuses.UnderMaintenance);
        var record = AddRecord(asset, MaintenanceStatus.IN_PROGRESS, estimatedCost: 100m);
        _db.OrganizationPolicies.Add(new OrganizationPolicy { Id = Guid.NewGuid(), OrganizationId = _orgId, CostVarianceTolerancePercent = 10m });
        await _db.SaveChangesAsync();

        var result = await _service.CompleteMaintenanceAsync(_orgId, _actorId, record.Id,
            Completion(actualCost: 120m, justification: "Supplier price rise on the replacement part."), CancellationToken.None);

        Assert.Equal(MaintenanceStatus.COMPLETED, result!.Status);
    }

    [Fact]
    public async Task CompleteMaintenanceAsync_WithinTheVarianceTolerance_NeedsNoJustification()
    {
        var asset = AddAsset(status: AssetStatuses.UnderMaintenance);
        var record = AddRecord(asset, MaintenanceStatus.IN_PROGRESS, estimatedCost: 100m);
        _db.OrganizationPolicies.Add(new OrganizationPolicy { Id = Guid.NewGuid(), OrganizationId = _orgId, CostVarianceTolerancePercent = 10m });
        await _db.SaveChangesAsync();

        var result = await _service.CompleteMaintenanceAsync(_orgId, _actorId, record.Id, Completion(actualCost: 110m), CancellationToken.None);

        Assert.Equal(MaintenanceStatus.COMPLETED, result!.Status);
    }

    // An asset-type-specific policy wins over the organisation-wide default.
    [Fact]
    public async Task CompleteMaintenanceAsync_PrefersTheAssetTypePolicy_OverTheOrganisationDefault()
    {
        var asset = AddAsset(status: AssetStatuses.UnderMaintenance);
        var record = AddRecord(asset, MaintenanceStatus.IN_PROGRESS, estimatedCost: 100m);
        _db.OrganizationPolicies.AddRange(
            new OrganizationPolicy { Id = Guid.NewGuid(), OrganizationId = _orgId, AssetTypeId = null, CostVarianceTolerancePercent = 50m },
            new OrganizationPolicy { Id = Guid.NewGuid(), OrganizationId = _orgId, AssetTypeId = asset.AssetTypeId, CostVarianceTolerancePercent = 5m });
        await _db.SaveChangesAsync();

        // 20% over: inside the 50% default, but outside the type's own 5%.
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CompleteMaintenanceAsync(_orgId, _actorId, record.Id, Completion(actualCost: 120m), CancellationToken.None));
    }

    [Fact]
    public async Task CompleteMaintenanceAsync_WithNoEstimate_SkipsTheVarianceCheck()
    {
        var asset = AddAsset(status: AssetStatuses.UnderMaintenance);
        var record = AddRecord(asset, MaintenanceStatus.IN_PROGRESS, estimatedCost: null);
        _db.OrganizationPolicies.Add(new OrganizationPolicy { Id = Guid.NewGuid(), OrganizationId = _orgId, CostVarianceTolerancePercent = 1m });
        await _db.SaveChangesAsync();

        var result = await _service.CompleteMaintenanceAsync(_orgId, _actorId, record.Id, Completion(actualCost: 10_000m), CancellationToken.None);

        Assert.Equal(MaintenanceStatus.COMPLETED, result!.Status);
    }

    [Fact]
    public async Task CompleteMaintenanceAsync_NotifiesTheFaultReporter()
    {
        var asset = AddAsset("AST-DONE", status: AssetStatuses.UnderMaintenance);
        var reporterId = Guid.NewGuid();
        var record = AddRecord(asset, MaintenanceStatus.IN_PROGRESS, reporterId: reporterId);
        await _db.SaveChangesAsync();

        await _service.CompleteMaintenanceAsync(_orgId, _actorId, record.Id, Completion(), CancellationToken.None);

        _notifications.Verify(n => n.NotifyAsync(_orgId, reporterId, NotificationTypes.MaintenanceStatusChanged,
            It.IsAny<string>(), It.Is<string>(m => m.Contains("AST-DONE") && m.Contains("COMPLETED")),
            "MaintenanceRecord", record.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- Cancel ----

    [Fact]
    public async Task CancelMaintenanceAsync_OnARequestedRecord_LeavesTheAssetStatusAlone_AndWritesNoHistory()
    {
        var asset = AddAsset();
        var record = AddRecord(asset, MaintenanceStatus.REQUESTED);
        await _db.SaveChangesAsync();

        var result = await _service.CancelMaintenanceAsync(_orgId, _actorId, record.Id,
            new CancelMaintenanceRequest { Reason = "Duplicate report" }, CancellationToken.None);

        Assert.Equal(MaintenanceStatus.CANCELLED, result!.Status);
        Assert.Equal("Duplicate report", result.CancellationReason);
        Assert.Equal(AssetStatuses.Active, (await _db.Assets.SingleAsync()).Status);
        Assert.Empty(_db.AssetHistoryEntries);
    }

    [Theory]
    [InlineData(MaintenanceStatus.COMPLETED)]
    [InlineData(MaintenanceStatus.CANCELLED)]
    public async Task CancelMaintenanceAsync_OnATerminalRecord_IsAConflict(MaintenanceStatus status)
    {
        var asset = AddAsset();
        var record = AddRecord(asset, status);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => _service.CancelMaintenanceAsync(_orgId, _actorId, record.Id,
            new CancelMaintenanceRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task CancelMaintenanceAsync_NotifiesTheAssignee_WithTheReason()
    {
        var asset = AddAsset("AST-CANCEL");
        var assignee = AddUser();
        var record = AddRecord(asset, MaintenanceStatus.APPROVED, assigneeId: assignee.Id);
        await _db.SaveChangesAsync();

        await _service.CancelMaintenanceAsync(_orgId, _actorId, record.Id,
            new CancelMaintenanceRequest { Reason = "Asset replaced" }, CancellationToken.None);

        _notifications.Verify(n => n.NotifyAsync(_orgId, assignee.Id, NotificationTypes.MaintenanceCancelled,
            It.IsAny<string>(), It.Is<string>(m => m.Contains("AST-CANCEL") && m.Contains("Asset replaced")),
            "MaintenanceRecord", record.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelMaintenanceAsync_ByTheAssigneeThemselves_DoesNotNotifyThem()
    {
        var asset = AddAsset();
        var assignee = AddUser();
        var record = AddRecord(asset, MaintenanceStatus.APPROVED, assigneeId: assignee.Id);
        await _db.SaveChangesAsync();

        await _service.CancelMaintenanceAsync(_orgId, assignee.Id, record.Id, new CancelMaintenanceRequest(), CancellationToken.None);

        _notifications.Verify(n => n.NotifyAsync(It.IsAny<Guid>(), assignee.Id, NotificationTypes.MaintenanceCancelled,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Read and list (FR-042) ----

    [Fact]
    public async Task GetMaintenanceRecordByIdAsync_HidesAnotherDepartmentsRecord_FromRestrictedStaff()
    {
        var departmentId = Guid.NewGuid();
        var asset = AddAsset(departmentId: departmentId);
        var record = AddRecord(asset, MaintenanceStatus.REQUESTED);
        await _db.SaveChangesAsync();

        var own = await _service.GetMaintenanceRecordByIdAsync(_orgId, DepartmentScope.Restricted(departmentId), record.Id, CancellationToken.None);
        var other = await _service.GetMaintenanceRecordByIdAsync(_orgId, DepartmentScope.Restricted(Guid.NewGuid()), record.Id, CancellationToken.None);

        Assert.NotNull(own);
        Assert.Null(other);
    }

    [Fact]
    public async Task GetMaintenanceRecordByIdAsync_DoesNotReturnAnotherOrganisationsRecord()
    {
        var asset = AddAsset(orgId: Guid.NewGuid());
        var record = AddRecord(asset, MaintenanceStatus.REQUESTED);
        await _db.SaveChangesAsync();

        Assert.Null(await _service.GetMaintenanceRecordByIdAsync(_orgId, DepartmentScope.Unrestricted, record.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ListMaintenanceRecordsAsync_FiltersByStatusTypeAndPriority()
    {
        var asset = AddAsset();
        var match = AddRecord(asset, MaintenanceStatus.APPROVED, type: MaintenanceType.PREVENTIVE, priority: MaintenancePriority.HIGH);
        AddRecord(asset, MaintenanceStatus.REQUESTED, type: MaintenanceType.PREVENTIVE, priority: MaintenancePriority.HIGH);
        AddRecord(asset, MaintenanceStatus.APPROVED, type: MaintenanceType.CORRECTIVE, priority: MaintenancePriority.HIGH);
        AddRecord(asset, MaintenanceStatus.APPROVED, type: MaintenanceType.PREVENTIVE, priority: MaintenancePriority.LOW);
        await _db.SaveChangesAsync();

        var result = await _service.ListMaintenanceRecordsAsync(_orgId, DepartmentScope.Unrestricted, new MaintenanceRecordFilter
        {
            Status = MaintenanceStatus.APPROVED, Type = MaintenanceType.PREVENTIVE, Priority = MaintenancePriority.HIGH,
        }, CancellationToken.None);

        Assert.Equal(match.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task ListMaintenanceRecordsAsync_FiltersByTheOwningAssetsDepartment()
    {
        var departmentId = Guid.NewGuid();
        var inDepartment = AddRecord(AddAsset("AST-IN", departmentId: departmentId), MaintenanceStatus.REQUESTED);
        AddRecord(AddAsset("AST-OUT"), MaintenanceStatus.REQUESTED);
        await _db.SaveChangesAsync();

        var result = await _service.ListMaintenanceRecordsAsync(_orgId, DepartmentScope.Unrestricted,
            new MaintenanceRecordFilter { DepartmentId = departmentId }, CancellationToken.None);

        Assert.Equal(inDepartment.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task ListMaintenanceRecordsAsync_DateRange_IncludesTheWholeToDay()
    {
        var asset = AddAsset();
        var day = new DateOnly(2026, 3, 10);
        var lateOnTheDay = AddRecord(asset, MaintenanceStatus.REQUESTED, createdAt: new DateTimeOffset(2026, 3, 10, 23, 30, 0, TimeSpan.Zero));
        AddRecord(asset, MaintenanceStatus.REQUESTED, createdAt: new DateTimeOffset(2026, 3, 11, 0, 30, 0, TimeSpan.Zero));
        AddRecord(asset, MaintenanceStatus.REQUESTED, createdAt: new DateTimeOffset(2026, 3, 9, 23, 30, 0, TimeSpan.Zero));
        await _db.SaveChangesAsync();

        var result = await _service.ListMaintenanceRecordsAsync(_orgId, DepartmentScope.Unrestricted,
            new MaintenanceRecordFilter { DateFrom = day, DateTo = day }, CancellationToken.None);

        Assert.Equal(lateOnTheDay.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task ListMaintenanceRecordsAsync_DefaultsToNewestFirst()
    {
        var asset = AddAsset();
        var older = AddRecord(asset, MaintenanceStatus.REQUESTED, createdAt: DateTimeOffset.UtcNow.AddDays(-2));
        var newer = AddRecord(asset, MaintenanceStatus.REQUESTED, createdAt: DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync();

        var result = await _service.ListMaintenanceRecordsAsync(_orgId, DepartmentScope.Unrestricted, new MaintenanceRecordFilter(), CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], result.Items.Select(r => r.Id));
    }

    [Fact]
    public async Task ListMyFaultReportsAsync_ExcludesPreventiveRecords()
    {
        var asset = AddAsset();
        var reporterId = Guid.NewGuid();
        var corrective = AddRecord(asset, MaintenanceStatus.REQUESTED, reporterId: reporterId, type: MaintenanceType.CORRECTIVE);
        AddRecord(asset, MaintenanceStatus.REQUESTED, reporterId: reporterId, type: MaintenanceType.PREVENTIVE);
        await _db.SaveChangesAsync();

        var result = await _service.ListMyFaultReportsAsync(_orgId, reporterId, new MaintenanceRecordFilter(), CancellationToken.None);

        Assert.Equal(corrective.Id, Assert.Single(result.Items).Id);
    }
}
