using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using backend.Tests;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Paging;
using CoreGrid.Api.Features.Shared.Scoping;
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

        return new CoreGridDbContext(options, new NullCurrentOrganizationProvider());
    }

    [Fact]
    public async Task InitiateTransfer_WhenAssetStatusIsActive_SucceedsAndSetsTransferRequested()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Required FK for AssetTransfer.InitiatedByUser — every real caller
        // has a Users row (RoleEnrichmentMiddleware guarantees it before
        // any controller runs); this mirrors that invariant.
        var user = new User { Id = userId, OrganizationId = orgId, ExternalSubjectId = "sub-init", GivenName = "I", FamilyName = "U", Email = "init@test.com", Role = CoreGridRole.InventoryOfficer };

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

        dbContext.Users.Add(user);
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
        var result = await service.InitiateTransferAsync(orgId, request, userId, CancellationToken.None);

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
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.InitiateTransferAsync(orgId, request, userId, CancellationToken.None));

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
        var initiatorId = Guid.NewGuid();

        // Required FKs for AssetTransfer.InitiatedByUser / the approver read
        // back via GetTransferByIdAsync's Include chain.
        var initiator = new User { Id = initiatorId, OrganizationId = orgId, ExternalSubjectId = "sub-initiator", GivenName = "I", FamilyName = "N", Email = "initiator@test.com", Role = CoreGridRole.Staff };
        var approver = new User { Id = approverId, OrganizationId = orgId, ExternalSubjectId = "sub-approver", GivenName = "A", FamilyName = "P", Email = "approver@test.com", Role = CoreGridRole.Administrator };

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
            InitiatedByUserId = initiatorId,
            Status = TransferStatus.REQUESTED,
            RequestedAt = DateTimeOffset.UtcNow
        };

        dbContext.Users.AddRange(initiator, approver);
        dbContext.Departments.AddRange(deptFrom, deptTo);
        dbContext.Locations.AddRange(locFrom, locTo);
        dbContext.Assets.Add(asset);
        dbContext.AssetTransfers.Add(transfer);
        await dbContext.SaveChangesAsync();

        var service = new TransferService(dbContext);

        // Act
        var result = await service.ApproveTransferAsync(orgId, transfer.Id, approverId, CancellationToken.None);

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
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.ApproveTransferAsync(orgId, transfer.Id, approverId, CancellationToken.None));

        Assert.Contains(invalidStatus.ToString(), ex.Message);
    }

    [Fact]
    public async Task ConfirmReceipt_WhenTransferIsApproved_SucceedsAndUpdatesLocationDepartmentAndActiveStatus()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var initiatorId = Guid.NewGuid();
        var approverId = Guid.NewGuid();

        // Required FK for AssetTransfer.InitiatedByUser, read back via
        // GetTransferByIdAsync's Include chain.
        var initiator = new User { Id = initiatorId, OrganizationId = orgId, ExternalSubjectId = "sub-initiator2", GivenName = "I", FamilyName = "N", Email = "initiator2@test.com", Role = CoreGridRole.Staff };
        var approver = new User { Id = approverId, OrganizationId = orgId, ExternalSubjectId = "sub-approver2", GivenName = "A", FamilyName = "P", Email = "approver2@test.com", Role = CoreGridRole.Administrator };

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
            InitiatedByUserId = initiatorId,
            ApprovedByUserId = approverId,
            Status = TransferStatus.APPROVED,
            RequestedAt = DateTimeOffset.UtcNow.AddHours(-2),
            ApprovedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        dbContext.Users.AddRange(initiator, approver);
        dbContext.Departments.AddRange(deptFrom, deptTo);
        dbContext.Locations.AddRange(locFrom, locTo);
        dbContext.Assets.Add(asset);
        dbContext.AssetTransfers.Add(transfer);
        await dbContext.SaveChangesAsync();

        var service = new TransferService(dbContext);

        // Act
        var result = await service.ConfirmReceiptAsync(orgId, transfer.Id, receiverId, CoreGridRole.Administrator, null, CancellationToken.None);

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
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.ConfirmReceiptAsync(orgId, transfer.Id, receiverId, CoreGridRole.Administrator, null, CancellationToken.None));

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
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.InitiateTransferAsync(orgId, request, userId, CancellationToken.None));

        // Verify dbContext has no added transfer and asset is still CONDEMNED
        Assert.Empty(dbContext.AssetTransfers);
        var persistentAsset = await dbContext.Assets.FindAsync(asset.Id);
        Assert.NotNull(persistentAsset);
        Assert.Equal(AssetStatusConstants.Condemned, persistentAsset.Status);
    }

    // =========================================================================
    // FR-047: Asset transfer history (GetTransferHistoryForAssetAsync)
    // =========================================================================

    [Fact]
    public async Task GetTransferHistoryForAsset_WhenTransfersExist_ReturnsAllOrderedByRequestedAtDesc()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var otherAssetId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            ExternalSubjectId = "sub-test",
            GivenName = "Test",
            FamilyName = "User",
            Email = "user@test.com",
            Role = CoreGridRole.InventoryOfficer
        };
        var asset = new Asset
        {
            Id = assetId,
            OrganizationId = orgId,
            AssetCode = "AST-001",
            Name = "Laptop",
            Status = AssetStatusConstants.Active,
            Condition = AssetStatusConstants.ConditionGood,
            QrPayload = "qr"
        };
        var otherAsset = new Asset
        {
            Id = otherAssetId,
            OrganizationId = orgId,
            AssetCode = "AST-002",
            Name = "Monitor",
            Status = AssetStatusConstants.Active,
            Condition = AssetStatusConstants.ConditionGood,
            QrPayload = "qr"
        };

        var deptA = new Department { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "D1", Name = "Dept 1" };
        var deptB = new Department { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "D2", Name = "Dept 2" };
        var locA = new Location { Id = Guid.NewGuid(), OrganizationId = orgId, DepartmentId = deptA.Id, Name = "Loc 1", Type = "Office" };
        var locB = new Location { Id = Guid.NewGuid(), OrganizationId = orgId, DepartmentId = deptB.Id, Name = "Loc 2", Type = "Office" };

        var t1 = new AssetTransfer
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetId = assetId,
            FromDepartmentId = deptA.Id,
            ToDepartmentId = deptB.Id,
            FromLocationId = locA.Id,
            ToLocationId = locB.Id,
            InitiatedByUserId = user.Id,
            Status = TransferStatus.COMPLETED,
            RequestedAt = DateTimeOffset.UtcNow.AddDays(-10)
        };

        var t2 = new AssetTransfer
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetId = assetId,
            FromDepartmentId = deptB.Id,
            ToDepartmentId = deptA.Id,
            FromLocationId = locB.Id,
            ToLocationId = locA.Id,
            InitiatedByUserId = user.Id,
            Status = TransferStatus.REJECTED,
            RequestedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };

        var otherTransfer = new AssetTransfer
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetId = otherAssetId,
            FromDepartmentId = deptA.Id,
            ToDepartmentId = deptB.Id,
            FromLocationId = locA.Id,
            ToLocationId = locB.Id,
            InitiatedByUserId = user.Id,
            Status = TransferStatus.APPROVED,
            RequestedAt = DateTimeOffset.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.Assets.AddRange(asset, otherAsset);
        dbContext.Departments.AddRange(deptA, deptB);
        dbContext.Locations.AddRange(locA, locB);
        dbContext.AssetTransfers.AddRange(t1, t2, otherTransfer);
        await dbContext.SaveChangesAsync();

        var service = new TransferService(dbContext);

        // Act
        var result = await service.GetTransferHistoryForAssetAsync(orgId, DepartmentScope.Unrestricted, assetId, new PagedQuery(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(t2.Id, result.Items[0].Id); // Most recent first
        Assert.Equal(t1.Id, result.Items[1].Id);
        Assert.All(result.Items, r => Assert.Equal(assetId, r.AssetId));
    }

    [Fact]
    public async Task GetTransferHistoryForAsset_WhenNoTransfersExist_ReturnsEmptyList()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var service = new TransferService(dbContext);
        var orgId = Guid.NewGuid();
        var assetId = Guid.NewGuid();

        // Act
        var result = await service.GetTransferHistoryForAssetAsync(orgId, DepartmentScope.Unrestricted, assetId, new PagedQuery(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetTransfers_WithPagination_ReturnsPagedResult()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var user = new User { Id = Guid.NewGuid(), OrganizationId = orgId, ExternalSubjectId = "sub-page", GivenName = "P", FamilyName = "U", Email = "p@test.com", Role = CoreGridRole.InventoryOfficer };
        var dept = new Department { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "D-P", Name = "Dept P" };
        var loc = new Location { Id = Guid.NewGuid(), OrganizationId = orgId, DepartmentId = dept.Id, Name = "Loc P", Type = "Office" };
        var asset = new Asset { Id = Guid.NewGuid(), OrganizationId = orgId, AssetCode = "AST-P", Name = "P-Asset", Status = AssetStatusConstants.Active, Condition = AssetStatusConstants.ConditionGood, QrPayload = "qr" };

        for (int i = 0; i < 25; i++)
        {
            dbContext.AssetTransfers.Add(new AssetTransfer
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                AssetId = asset.Id,
                FromDepartmentId = dept.Id,
                ToDepartmentId = dept.Id,
                FromLocationId = loc.Id,
                ToLocationId = loc.Id,
                InitiatedByUserId = user.Id,
                Status = TransferStatus.REQUESTED,
                RequestedAt = DateTimeOffset.UtcNow.AddMinutes(i)
            });
        }

        dbContext.Users.Add(user);
        dbContext.Departments.Add(dept);
        dbContext.Locations.Add(loc);
        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();

        var service = new TransferService(dbContext);

        // Act
        var result = await service.GetTransfersAsync(orgId, DepartmentScope.Unrestricted, new TransferQueryParameters { Page = 2, PageSize = 10 }, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(10, result.Items.Count);
    }
}
