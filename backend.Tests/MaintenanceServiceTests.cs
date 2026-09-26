using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Maintenance.DTOs;
using CoreGrid.Api.Features.Maintenance.Services;
using CoreGrid.Api.Features.Notifications.Services;
using CoreGrid.Api.Features.Shared.Storage;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace backend.Tests.Features.Maintenance;

// B6 regression (§0, fixed §5.4): CancelMaintenanceAsync used to write
// AssetHistory.EventType = "MAINTENANCE_CANCELLED", which is not one of
// CK_AssetHistory_EventType's allowed values — a real Postgres check
// constraint InMemory can't reproduce, so this pins the actual value
// written instead of relying on the (InMemory-invisible) DB error.
public class MaintenanceServiceTests
{
    private static CoreGridDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CoreGridDbContext(options, new NullCurrentOrganizationProvider());
    }

    [Fact]
    public async Task ListMyFaultReportsAsync_ReturnsOnlyTheAuthenticatedReportersFaults()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var reporterA = Guid.NewGuid();
        var reporterB = Guid.NewGuid();
        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetTypeId = Guid.NewGuid(), DepartmentId = Guid.NewGuid(), LocationId = Guid.NewGuid(),
            AssetCode = "AST-OWN-1", Name = "Pump", Status = AssetStatuses.Active, Condition = AssetConditions.Good, QrPayload = "qr",
        };
        var reportA = new MaintenanceRecord
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetId = asset.Id, Description = "Leak reported by A", ObservedCondition = AssetConditions.Poor,
            Type = MaintenanceType.CORRECTIVE, Priority = MaintenancePriority.MEDIUM, Status = MaintenanceStatus.REQUESTED,
            ReportedByUserId = reporterA, CreatedAt = DateTimeOffset.UtcNow,
        };
        var reportB = new MaintenanceRecord
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetId = asset.Id, Description = "Leak reported by B", ObservedCondition = AssetConditions.Poor,
            Type = MaintenanceType.CORRECTIVE, Priority = MaintenancePriority.MEDIUM, Status = MaintenanceStatus.REQUESTED,
            ReportedByUserId = reporterB, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        };
        db.AddRange(asset, reportA, reportB);
        await db.SaveChangesAsync();

        var service = new MaintenanceService(db, new Mock<INotificationService>().Object, new Mock<IFileStorageService>().Object);

        var reportsForA = await service.ListMyFaultReportsAsync(orgId, reporterA, new MaintenanceRecordFilter(), CancellationToken.None);
        var reportsForB = await service.ListMyFaultReportsAsync(orgId, reporterB, new MaintenanceRecordFilter(), CancellationToken.None);

        Assert.Collection(reportsForA.Items, report => Assert.Equal(reportA.Id, report.Id));
        Assert.Collection(reportsForB.Items, report => Assert.Equal(reportB.Id, report.Id));
    }

    [Fact]
    public async Task ApproveMaintenanceAsync_NotifiesTheOriginalFaultReporterOfTheStatusChange()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var reporterId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetTypeId = Guid.NewGuid(), DepartmentId = Guid.NewGuid(), LocationId = Guid.NewGuid(),
            AssetCode = "AST-STATUS-1", Name = "Pump", Status = AssetStatuses.Active, Condition = AssetConditions.Good, QrPayload = "qr",
        };
        var assignee = new User
        {
            Id = assigneeId, OrganizationId = orgId, ExternalSubjectId = "maintenance-assignee", Email = "assignee@example.test",
            GivenName = "Maintenance", FamilyName = "Officer", Role = CoreGridRole.InventoryOfficer, CreatedAt = DateTimeOffset.UtcNow,
        };
        var report = new MaintenanceRecord
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetId = asset.Id, Description = "Pump leak", ObservedCondition = AssetConditions.Poor,
            Type = MaintenanceType.CORRECTIVE, Priority = MaintenancePriority.MEDIUM, Status = MaintenanceStatus.REQUESTED,
            ReportedByUserId = reporterId,
        };
        db.AddRange(asset, assignee, report);
        await db.SaveChangesAsync();
        var notifications = new Mock<INotificationService>();
        var service = new MaintenanceService(db, notifications.Object, new Mock<IFileStorageService>().Object);

        var result = await service.ApproveMaintenanceAsync(orgId, approverId, report.Id,
            new ApproveMaintenanceRequest { AssigneeId = assigneeId, EstimatedCost = 100m }, CancellationToken.None);

        Assert.Equal(MaintenanceStatus.APPROVED, result!.Status);
        notifications.Verify(n => n.NotifyAsync(
            orgId, reporterId, NotificationTypes.MaintenanceStatusChanged,
            It.IsAny<string>(), It.IsAny<string>(), "MaintenanceRecord", report.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelMaintenanceAsync_OnAnInProgressRecord_SucceedsAndWritesTheAllowedEventType()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetTypeId = Guid.NewGuid(), DepartmentId = Guid.NewGuid(), LocationId = Guid.NewGuid(),
            AssetCode = "AST-MC-1", Name = "Server", Status = AssetStatuses.UnderMaintenance, Condition = AssetConditions.Good, QrPayload = "qr",
        };
        var record = new MaintenanceRecord
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetId = asset.Id,
            Description = "Fan replacement", ObservedCondition = AssetConditions.Fair,
            Type = MaintenanceType.CORRECTIVE, Priority = MaintenancePriority.MEDIUM, Status = MaintenanceStatus.IN_PROGRESS,
            CreatedBy = currentUserId, UpdatedBy = currentUserId,
        };
        db.Assets.Add(asset);
        db.MaintenanceRecords.Add(record);
        await db.SaveChangesAsync();

        var service = new MaintenanceService(db, new Mock<INotificationService>().Object, new Mock<IFileStorageService>().Object);

        // Act — this used to throw a DB check-constraint violation (500) in
        // production; against InMemory it would previously have "succeeded"
        // while silently writing a disallowed EventType.
        var result = await service.CancelMaintenanceAsync(orgId, currentUserId, record.Id, new CancelMaintenanceRequest { Reason = "No longer needed" }, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(MaintenanceStatus.CANCELLED, result!.Status);

        var updatedAsset = await db.Assets.SingleAsync(a => a.Id == asset.Id);
        Assert.Equal(AssetStatuses.Active, updatedAsset.Status);

        var historyEntry = await db.AssetHistoryEntries.SingleAsync(h => h.AssetId == asset.Id);
        Assert.Equal(AssetHistoryEventTypes.Maintenance, historyEntry.EventType);
        Assert.NotEqual("MAINTENANCE_CANCELLED", historyEntry.EventType);
    }

    // SRS §9.3: "Amend classification, priority, description."
    [Fact]
    public async Task AmendMaintenanceAsync_OnARequestedRecord_UpdatesTheAmendableFields()
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        // MaintenanceRecord.AssetId is non-nullable, so EF treats the
        // Asset navigation as required and left-joins on it even for the
        // null-checked AssetCode/AssetName in MaintenanceRecordDto's
        // projection — against InMemory specifically, a required
        // navigation with no matching row silently drops the whole row.
        var asset = new Asset
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetTypeId = Guid.NewGuid(), DepartmentId = Guid.NewGuid(), LocationId = Guid.NewGuid(),
            AssetCode = "AST-AM-1", Name = "Server", Status = AssetStatuses.Active, Condition = AssetConditions.Good, QrPayload = "qr",
        };
        var record = new MaintenanceRecord
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetId = asset.Id,
            Description = "Original description", ObservedCondition = AssetConditions.Fair,
            Type = MaintenanceType.CORRECTIVE, Priority = MaintenancePriority.LOW, Status = MaintenanceStatus.REQUESTED,
        };
        db.Assets.Add(asset);
        db.MaintenanceRecords.Add(record);
        await db.SaveChangesAsync();

        var service = new MaintenanceService(db, new Mock<INotificationService>().Object, new Mock<IFileStorageService>().Object);

        var result = await service.AmendMaintenanceAsync(
            orgId, currentUserId, record.Id,
            new AmendMaintenanceRequest { Type = MaintenanceType.PREVENTIVE, Priority = MaintenancePriority.CRITICAL, Description = "Revised description" },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(MaintenanceType.PREVENTIVE, result!.Type);
        Assert.Equal(MaintenancePriority.CRITICAL, result.Priority);
        Assert.Equal("Revised description", result.Description);

        var updated = await db.MaintenanceRecords.SingleAsync(m => m.Id == record.Id);
        Assert.Equal(currentUserId, updated.UpdatedBy);
    }

    [Theory]
    [InlineData(MaintenanceStatus.COMPLETED)]
    [InlineData(MaintenanceStatus.CANCELLED)]
    public async Task AmendMaintenanceAsync_OnATerminalRecord_ThrowsConflictException(MaintenanceStatus terminalStatus)
    {
        await using var db = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();

        var record = new MaintenanceRecord
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, AssetId = Guid.NewGuid(),
            Description = "Description", ObservedCondition = AssetConditions.Fair,
            Type = MaintenanceType.CORRECTIVE, Priority = MaintenancePriority.LOW, Status = terminalStatus,
        };
        db.MaintenanceRecords.Add(record);
        await db.SaveChangesAsync();

        var service = new MaintenanceService(db, new Mock<INotificationService>().Object, new Mock<IFileStorageService>().Object);

        await Assert.ThrowsAsync<CoreGrid.Api.Features.Shared.Exceptions.ConflictException>(() =>
            service.AmendMaintenanceAsync(
                orgId, Guid.NewGuid(), record.Id,
                new AmendMaintenanceRequest { Type = MaintenanceType.CORRECTIVE, Priority = MaintenancePriority.HIGH, Description = "New" },
                CancellationToken.None));
    }

    [Fact]
    public async Task AmendMaintenanceAsync_RecordNotFound_ReturnsNull()
    {
        await using var db = CreateInMemoryDbContext();
        var service = new MaintenanceService(db, new Mock<INotificationService>().Object, new Mock<IFileStorageService>().Object);

        var result = await service.AmendMaintenanceAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new AmendMaintenanceRequest { Type = MaintenanceType.CORRECTIVE, Priority = MaintenancePriority.LOW, Description = "N/A" },
            CancellationToken.None);

        Assert.Null(result);
    }
}
