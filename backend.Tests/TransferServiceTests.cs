using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Transfers.DTOs;
using CoreGrid.Api.Features.Transfers.Services;

namespace backend.Tests.Features.Transfers;

public class TransferServiceTests
{
    private CoreGridDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CoreGridDbContext(options);
    }

    [Fact]
    public async Task InitiateTransfer_WhenAssetStatusIsActive_SucceedsAndSetsTransferRequested()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var deptFrom = new Department { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "D-FROM", Name = "From Dept" };
        var deptTo = new Department { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "D-TO", Name = "To Dept" };
        var locFrom = new Location { Id = Guid.NewGuid(), OrganizationId = orgId, DepartmentId = deptFrom.Id, Name = "Loc 1", Type = "ROOM" };
        var locTo = new Location { Id = Guid.NewGuid(), OrganizationId = orgId, DepartmentId = deptTo.Id, Name = "Loc 2", Type = "ROOM" };

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            DepartmentId = deptFrom.Id,
            LocationId = locFrom.Id,
            AssetCode = "AST-001",
            Name = "Laptop",
            Status = AssetStatusConstants.Active,
            Condition = AssetStatusConstants.ConditionGood,
            QrPayload = "qr"
        };

        dbContext.Departments.AddRange(deptFrom, deptTo);
        dbContext.Locations.AddRange(locFrom, locTo);
        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();

        var service = new TransferService(dbContext);
        var request = new InitiateTransferRequest
        {
            AssetId = asset.Id,
            ToDepartmentId = deptTo.Id,
            ToLocationId = locTo.Id
        };

        // Act
        var result = await service.InitiateTransferAsync(orgId, request, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TransferStatus.REQUESTED, result.Status);
        Assert.Equal(deptTo.Id, result.ToDepartmentId);
        Assert.Equal(locTo.Id, result.ToLocationId);

        var updatedAsset = await dbContext.Assets.FindAsync(asset.Id);
        Assert.NotNull(updatedAsset);
        Assert.Equal(AssetStatusConstants.TransferRequested, updatedAsset.Status);
    }

    [Theory]
    [InlineData(AssetStatusConstants.UnderMaintenance)]
    [InlineData(AssetStatusConstants.TransferRequested)]
    [InlineData(AssetStatusConstants.InTransit)]
    [InlineData(AssetStatusConstants.Condemned)]
    [InlineData(AssetStatusConstants.DisposalRequested)]
    [InlineData(AssetStatusConstants.Disposed)]
    public async Task InitiateTransfer_WhenAssetStatusIsNotActive_Fails(string invalidStatus)
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var deptFrom = new Department { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "D-FROM", Name = "From Dept" };
        var deptTo = new Department { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "D-TO", Name = "To Dept" };
        var locFrom = new Location { Id = Guid.NewGuid(), OrganizationId = orgId, DepartmentId = deptFrom.Id, Name = "Loc 1", Type = "ROOM" };
        var locTo = new Location { Id = Guid.NewGuid(), OrganizationId = orgId, DepartmentId = deptTo.Id, Name = "Loc 2", Type = "ROOM" };

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            DepartmentId = deptFrom.Id,
            LocationId = locFrom.Id,
            AssetCode = "AST-002",
            Name = "Monitor",
            Status = invalidStatus,
            Condition = AssetStatusConstants.ConditionGood,
            QrPayload = "qr"
        };

        dbContext.Departments.AddRange(deptFrom, deptTo);
        dbContext.Locations.AddRange(locFrom, locTo);
        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();

        var service = new TransferService(dbContext);
        var request = new InitiateTransferRequest
        {
            AssetId = asset.Id,
            ToDepartmentId = deptTo.Id,
            ToLocationId = locTo.Id
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.InitiateTransferAsync(orgId, request, userId));

        Assert.Contains(invalidStatus, ex.Message);

        // Verify transfer was not created and asset status unchanged
        var transfersCount = await dbContext.AssetTransfers.CountAsync();
        Assert.Equal(0, transfersCount);

        var unaffectedAsset = await dbContext.Assets.FindAsync(asset.Id);
        Assert.NotNull(unaffectedAsset);
        Assert.Equal(invalidStatus, unaffectedAsset.Status);
    }

    [Fact]
    public async Task ApproveTransfer_WhenTransferIsRequested_SucceedsAndSetsAssetInTransit()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var approverId = Guid.NewGuid();

        var deptFrom = new Department { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "D-FROM", Name = "From Dept" };
        var deptTo = new Department { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "D-TO", Name = "To Dept" };
        var locFrom = new Location { Id = Guid.NewGuid(), OrganizationId = orgId, DepartmentId = deptFrom.Id, Name = "Loc 1", Type = "ROOM" };
        var locTo = new Location { Id = Guid.NewGuid(), OrganizationId = orgId, DepartmentId = deptTo.Id, Name = "Loc 2", Type = "ROOM" };

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            DepartmentId = deptFrom.Id,
            LocationId = locFrom.Id,
            AssetCode = "AST-003",
            Name = "Printer",
            Status = AssetStatusConstants.TransferRequested,
            Condition = AssetStatusConstants.ConditionGood,
            QrPayload = "qr"
        };

        var transfer = new AssetTransfer
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetId = asset.Id,
            Asset = asset,
            FromDepartmentId = deptFrom.Id,
            ToDepartmentId = deptTo.Id,
            FromLocationId = locFrom.Id,
            ToLocationId = locTo.Id,
            InitiatedByUserId = Guid.NewGuid(),
            Status = TransferStatus.REQUESTED,
            RequestedAt = DateTimeOffset.UtcNow
        };

        dbContext.Departments.AddRange(deptFrom, deptTo);
        dbContext.Locations.AddRange(locFrom, locTo);
        dbContext.Assets.Add(asset);
        dbContext.AssetTransfers.Add(transfer);
        await dbContext.SaveChangesAsync();

        var service = new TransferService(dbContext);

        // Act
        var result = await service.ApproveTransferAsync(orgId, transfer.Id, approverId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TransferStatus.APPROVED, result.Status);
        Assert.Equal(approverId, result.ApprovedByUserId);
        Assert.NotNull(result.ApprovedAt);

        var updatedAsset = await dbContext.Assets.FindAsync(asset.Id);
        Assert.NotNull(updatedAsset);
        Assert.Equal(AssetStatusConstants.InTransit, updatedAsset.Status);
        Assert.Equal(approverId, updatedAsset.UpdatedBy);
    }

    [Theory]
    [InlineData(TransferStatus.APPROVED)]
    [InlineData(TransferStatus.IN_TRANSIT)]
    [InlineData(TransferStatus.COMPLETED)]
    [InlineData(TransferStatus.REJECTED)]
    [InlineData(TransferStatus.CANCELLED)]
    public async Task ApproveTransfer_WhenTransferIsNotRequested_Fails(TransferStatus invalidStatus)
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var approverId = Guid.NewGuid();

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetCode = "AST-004",
            Name = "Chair",
            Status = AssetStatusConstants.InTransit,
            Condition = AssetStatusConstants.ConditionGood,
            QrPayload = "qr"
        };

        var transfer = new AssetTransfer
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetId = asset.Id,
            Asset = asset,
            FromDepartmentId = Guid.NewGuid(),
            ToDepartmentId = Guid.NewGuid(),
            FromLocationId = Guid.NewGuid(),
            ToLocationId = Guid.NewGuid(),
            InitiatedByUserId = Guid.NewGuid(),
            Status = invalidStatus,
            RequestedAt = DateTimeOffset.UtcNow
        };

        dbContext.Assets.Add(asset);
        dbContext.AssetTransfers.Add(transfer);
        await dbContext.SaveChangesAsync();

        var service = new TransferService(dbContext);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ApproveTransferAsync(orgId, transfer.Id, approverId));

        Assert.Contains(invalidStatus.ToString(), ex.Message);
    }

    [Fact]
    public async Task ConfirmReceipt_WhenTransferIsApproved_SucceedsAndUpdatesLocationDepartmentAndActiveStatus()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var originalDeptId = Guid.NewGuid();
        var originalLocId = Guid.NewGuid();
        var targetDeptId = Guid.NewGuid();
        var targetLocId = Guid.NewGuid();

        var deptFrom = new Department { Id = originalDeptId, OrganizationId = orgId, Code = "D-FROM", Name = "From Dept" };
        var deptTo = new Department { Id = targetDeptId, OrganizationId = orgId, Code = "D-TO", Name = "To Dept" };
        var locFrom = new Location { Id = originalLocId, OrganizationId = orgId, DepartmentId = originalDeptId, Name = "Loc 1", Type = "ROOM" };
        var locTo = new Location { Id = targetLocId, OrganizationId = orgId, DepartmentId = targetDeptId, Name = "Loc 2", Type = "ROOM" };

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            DepartmentId = originalDeptId,
            LocationId = originalLocId,
            AssetCode = "AST-005",
            Name = "Projector",
            Status = AssetStatusConstants.InTransit,
            Condition = AssetStatusConstants.ConditionGood,
            QrPayload = "qr"
        };

        var transfer = new AssetTransfer
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetId = asset.Id,
            Asset = asset,
            FromDepartmentId = originalDeptId,
            ToDepartmentId = targetDeptId,
            FromLocationId = originalLocId,
            ToLocationId = targetLocId,
            InitiatedByUserId = Guid.NewGuid(),
            ApprovedByUserId = Guid.NewGuid(),
            Status = TransferStatus.APPROVED,
            RequestedAt = DateTimeOffset.UtcNow.AddHours(-2),
            ApprovedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        dbContext.Departments.AddRange(deptFrom, deptTo);
        dbContext.Locations.AddRange(locFrom, locTo);
        dbContext.Assets.Add(asset);
        dbContext.AssetTransfers.Add(transfer);
        await dbContext.SaveChangesAsync();

        var service = new TransferService(dbContext);

        // Act
        var result = await service.ConfirmReceiptAsync(orgId, transfer.Id, receiverId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TransferStatus.COMPLETED, result.Status);
        Assert.Equal(receiverId, result.ConfirmedByUserId);
        Assert.NotNull(result.ConfirmedAt);

        var updatedAsset = await dbContext.Assets.FindAsync(asset.Id);
        Assert.NotNull(updatedAsset);
        Assert.Equal(targetDeptId, updatedAsset.DepartmentId);
        Assert.Equal(targetLocId, updatedAsset.LocationId);
        Assert.Equal(AssetStatusConstants.Active, updatedAsset.Status);
        Assert.Equal(receiverId, updatedAsset.UpdatedBy);
    }

    [Theory]
    [InlineData(TransferStatus.REQUESTED)]
    [InlineData(TransferStatus.IN_TRANSIT)]
    [InlineData(TransferStatus.COMPLETED)]
    [InlineData(TransferStatus.REJECTED)]
    [InlineData(TransferStatus.CANCELLED)]
    public async Task ConfirmReceipt_WhenTransferIsNotApproved_Fails(TransferStatus invalidStatus)
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetCode = "AST-006",
            Name = "Desk",
            Status = AssetStatusConstants.TransferRequested,
            Condition = AssetStatusConstants.ConditionGood,
            QrPayload = "qr"
        };

        var transfer = new AssetTransfer
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetId = asset.Id,
            Asset = asset,
            FromDepartmentId = Guid.NewGuid(),
            ToDepartmentId = Guid.NewGuid(),
            FromLocationId = Guid.NewGuid(),
            ToLocationId = Guid.NewGuid(),
            InitiatedByUserId = Guid.NewGuid(),
            Status = invalidStatus,
            RequestedAt = DateTimeOffset.UtcNow
        };

        dbContext.Assets.Add(asset);
        dbContext.AssetTransfers.Add(transfer);
        await dbContext.SaveChangesAsync();

        var service = new TransferService(dbContext);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConfirmReceiptAsync(orgId, transfer.Id, receiverId));

        Assert.Contains(invalidStatus.ToString(), ex.Message);
    }

    [Fact]
    public async Task AtomicityIntent_WhenInitiateFailsValidation_NoPartialStateIsSaved()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetCode = "AST-007",
            Name = "Router",
            Status = AssetStatusConstants.Condemned, // Invalid for transfer
            Condition = AssetStatusConstants.ConditionPoor,
            QrPayload = "qr"
        };

        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();

        var service = new TransferService(dbContext);
        var request = new InitiateTransferRequest
        {
            AssetId = asset.Id,
            ToDepartmentId = Guid.NewGuid(),
            ToLocationId = Guid.NewGuid()
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.InitiateTransferAsync(orgId, request, userId));

        // Verify dbContext has no added transfer and asset is still CONDEMNED
        Assert.Empty(dbContext.AssetTransfers);
        var persistentAsset = await dbContext.Assets.FindAsync(asset.Id);
        Assert.NotNull(persistentAsset);
        Assert.Equal(AssetStatusConstants.Condemned, persistentAsset.Status);
    }
}
