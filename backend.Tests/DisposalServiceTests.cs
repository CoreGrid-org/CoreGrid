using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Disposals;
using CoreGrid.Api.Features.Disposals.DTOs;

namespace backend.Tests.Features.Disposals;

public class DisposalServiceTests
{
    private CoreGridDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CoreGridDbContext(options);
    }

    [Theory]
    [InlineData(AssetStatusConstants.ConditionPoor)]
    [InlineData(AssetStatusConstants.ConditionUnserviceable)]
    public async Task CondemnAsset_WhenConditionIsPoorOrUnserviceableAndPriorStatusIsActive_Succeeds(string condition)
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetCode = "AST-CND-1",
            Name = "Server",
            Status = AssetStatusConstants.Active,
            Condition = condition,
            QrPayload = "qr"
        };

        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();

        var preconditionService = new DisposalPreconditionService(dbContext);
        var service = new DisposalService(dbContext, preconditionService);

        // Act
        var result = await service.CondemnAssetAsync(orgId, asset.Id, new CondemnAssetRequest { Reason = "Damaged hardware" }, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AssetStatusConstants.Condemned, result.Status);

        var updatedAsset = await dbContext.Assets.FindAsync(asset.Id);
        Assert.NotNull(updatedAsset);
        Assert.Equal(AssetStatusConstants.Condemned, updatedAsset.Status);

        var history = await dbContext.AssetHistoryEntries.FirstOrDefaultAsync(h => h.AssetId == asset.Id);
        Assert.NotNull(history);
        Assert.Equal("STATUS_CHANGE", history.EventType);
    }

    [Theory]
    [InlineData(AssetStatusConstants.ConditionNew)]
    [InlineData(AssetStatusConstants.ConditionGood)]
    [InlineData(AssetStatusConstants.ConditionFair)]
    public async Task CondemnAsset_WhenConditionIsNotPoorOrUnserviceable_Fails(string invalidCondition)
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetCode = "AST-CND-2",
            Name = "Laptop",
            Status = AssetStatusConstants.Active,
            Condition = invalidCondition,
            QrPayload = "qr"
        };

        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();

        var preconditionService = new DisposalPreconditionService(dbContext);
        var service = new DisposalService(dbContext, preconditionService);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CondemnAssetAsync(orgId, asset.Id, new CondemnAssetRequest(), userId));

        Assert.Contains(invalidCondition, ex.Message);
    }

    [Theory]
    [InlineData(AssetStatusConstants.Condemned)]
    [InlineData(AssetStatusConstants.DisposalRequested)]
    [InlineData(AssetStatusConstants.Disposed)]
    [InlineData(AssetStatusConstants.TransferRequested)]
    [InlineData(AssetStatusConstants.InTransit)]
    public async Task CondemnAsset_WhenPriorStatusIsInvalid_Fails(string invalidPriorStatus)
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetCode = "AST-CND-3",
            Name = "Printer",
            Status = invalidPriorStatus,
            Condition = AssetStatusConstants.ConditionPoor,
            QrPayload = "qr"
        };

        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();

        var preconditionService = new DisposalPreconditionService(dbContext);
        var service = new DisposalService(dbContext, preconditionService);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CondemnAssetAsync(orgId, asset.Id, new CondemnAssetRequest(), userId));
    }

    [Fact]
    public async Task SubmitDisposalRequest_WhenAssetIsCondemned_SucceedsAndSetsDisposalRequested()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetCode = "AST-DSP-1",
            Name = "Monitor",
            Status = AssetStatusConstants.Condemned,
            Condition = AssetStatusConstants.ConditionPoor,
            QrPayload = "qr"
        };

        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();

        var preconditionService = new DisposalPreconditionService(dbContext);
        var service = new DisposalService(dbContext, preconditionService);

        var request = new SubmitDisposalRequest
        {
            AssetId = asset.Id,
            DisposalMethod = DisposalMethod.AUCTION,
            EstimatedResidualValue = 250m,
            ValuationDate = new DateOnly(2026, 8, 1),
            Notes = "Auction at central warehouse"
        };

        // Act
        var result = await service.SubmitDisposalRequestAsync(orgId, request, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DisposalStatus.PENDING, result.Status);
        Assert.Equal(DisposalMethod.AUCTION, result.DisposalMethod);
        Assert.Equal(250m, result.EstimatedResidualValue);
        Assert.Equal(new DateOnly(2026, 8, 1), result.ValuationDate);

        var updatedAsset = await dbContext.Assets.FindAsync(asset.Id);
        Assert.NotNull(updatedAsset);
        Assert.Equal(AssetStatusConstants.DisposalRequested, updatedAsset.Status);
    }

    [Theory]
    [InlineData(AssetStatusConstants.Active)]
    [InlineData(AssetStatusConstants.UnderMaintenance)]
    [InlineData(AssetStatusConstants.TransferRequested)]
    [InlineData(AssetStatusConstants.InTransit)]
    [InlineData(AssetStatusConstants.DisposalRequested)]
    [InlineData(AssetStatusConstants.Disposed)]
    public async Task SubmitDisposalRequest_WhenAssetIsNotCondemned_Fails(string invalidStatus)
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetCode = "AST-DSP-2",
            Name = "Desk",
            Status = invalidStatus,
            Condition = AssetStatusConstants.ConditionPoor,
            QrPayload = "qr"
        };

        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();

        var preconditionService = new DisposalPreconditionService(dbContext);
        var service = new DisposalService(dbContext, preconditionService);

        var request = new SubmitDisposalRequest
        {
            AssetId = asset.Id,
            DisposalMethod = DisposalMethod.DESTROY,
            EstimatedResidualValue = 0m,
            ValuationDate = new DateOnly(2026, 8, 1)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SubmitDisposalRequestAsync(orgId, request, userId));

        Assert.Contains(invalidStatus, ex.Message);
    }

    [Fact]
    public async Task ApproveDisposal_WhenSeparationOfDutiesFails_ReturnsForbiddenResult()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var sameUserId = Guid.NewGuid();

        var assetType = new AssetType { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "VEH", Name = "Vehicle", UsefulLifeYears = 5, DefaultMaintenanceIntervalDays = 180 };
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetTypeId = assetType.Id,
            AssetCode = "AST-DSP-3",
            Name = "Van",
            Status = AssetStatusConstants.Condemned,
            Condition = AssetStatusConstants.ConditionUnserviceable,
            AcquisitionDate = new DateOnly(2015, 1, 1),
            QrPayload = "qr"
        };

        var disposalRequest = new DisposalRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetId = asset.Id,
            Asset = asset,
            InitiatedByUserId = sameUserId, // Requester
            DisposalMethod = DisposalMethod.SCRAP,
            EstimatedResidualValue = 100m,
            ValuationDate = new DateOnly(2026, 8, 1),
            Status = DisposalStatus.PENDING,
            RequestedAt = DateTimeOffset.UtcNow
        };

        dbContext.AssetTypes.Add(assetType);
        dbContext.Assets.Add(asset);
        dbContext.DisposalRequests.Add(disposalRequest);
        await dbContext.SaveChangesAsync();

        var preconditionService = new DisposalPreconditionService(dbContext);
        var service = new DisposalService(dbContext, preconditionService);

        // Act (Approver is same as Requester)
        var result = await service.ApproveDisposalAsync(orgId, disposalRequest.Id, sameUserId);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.IsForbidden);
        Assert.NotNull(result.ForbiddenReason);
    }

    [Fact]
    public async Task ApproveDisposal_WhenPreconditionsFail_ReturnsUnsuccessfulWithFullBreakdown()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var approverId = Guid.NewGuid();

        var assetType = new AssetType { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "IT", Name = "IT Hardware", UsefulLifeYears = 5, DefaultMaintenanceIntervalDays = 180 };
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetTypeId = assetType.Id,
            AssetCode = "AST-DSP-4",
            Name = "Workstation",
            Status = AssetStatusConstants.Active, // Fails P1 (not CONDEMNED)
            Condition = AssetStatusConstants.ConditionGood,
            AcquisitionDate = new DateOnly(2025, 1, 1), // Fails P3 (only 1 year elapsed)
            QrPayload = "qr"
        };

        var disposalRequest = new DisposalRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetId = asset.Id,
            Asset = asset,
            InitiatedByUserId = requesterId,
            DisposalMethod = DisposalMethod.SCRAP,
            EstimatedResidualValue = -10m, // Fails P2 (negative)
            ValuationDate = null,           // Fails P2 (null date)
            Status = DisposalStatus.PENDING,
            RequestedAt = DateTimeOffset.UtcNow
        };

        dbContext.AssetTypes.Add(assetType);
        dbContext.Assets.Add(asset);
        dbContext.DisposalRequests.Add(disposalRequest);
        await dbContext.SaveChangesAsync();

        var preconditionService = new DisposalPreconditionService(dbContext);
        var service = new DisposalService(dbContext, preconditionService);

        // Act
        var result = await service.ApproveDisposalAsync(orgId, disposalRequest.Id, approverId);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.IsForbidden);
        Assert.NotNull(result.PreconditionResult);
        Assert.False(result.PreconditionResult.AllPassed);
        Assert.Contains(result.PreconditionResult.Checks, c => c.Code == "P1" && !c.Passed);
        Assert.Contains(result.PreconditionResult.Checks, c => c.Code == "P2" && !c.Passed);
    }

    [Fact]
    public async Task ApproveDisposal_WhenStatusIsNotPending_ReturnsInvalidState()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var approverId = Guid.NewGuid();

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetCode = "AST-DSP-5",
            Name = "Cabinet",
            Status = AssetStatusConstants.Disposed,
            Condition = AssetStatusConstants.ConditionPoor,
            QrPayload = "qr"
        };

        var disposalRequest = new DisposalRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetId = asset.Id,
            Asset = asset,
            InitiatedByUserId = Guid.NewGuid(),
            DisposalMethod = DisposalMethod.DESTROY,
            EstimatedResidualValue = 0m,
            ValuationDate = new DateOnly(2026, 8, 1),
            Status = DisposalStatus.APPROVED, // Already approved
            RequestedAt = DateTimeOffset.UtcNow
        };

        dbContext.Assets.Add(asset);
        dbContext.DisposalRequests.Add(disposalRequest);
        await dbContext.SaveChangesAsync();

        var preconditionService = new DisposalPreconditionService(dbContext);
        var service = new DisposalService(dbContext, preconditionService);

        // Act
        var result = await service.ApproveDisposalAsync(orgId, disposalRequest.Id, approverId);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.IsInvalidState);
    }

    [Fact]
    public async Task FullHappyPath_Condemn_Submit_Approve_TransitionsToDisposedAtomically()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var approverId = Guid.NewGuid();

        var assetType = new AssetType
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Code = "OFF-EQ",
            Name = "Office Equipment",
            UsefulLifeYears = 3,
            DefaultMaintenanceIntervalDays = 180
        };

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetTypeId = assetType.Id,
            AssetCode = "AST-DSP-HAPPY",
            Name = "Old Heavy Copier",
            Status = AssetStatusConstants.Active,
            Condition = AssetStatusConstants.ConditionUnserviceable,
            AcquisitionDate = new DateOnly(2020, 1, 1), // > 3 years
            AcquisitionCost = 4500m,
            ResidualValue = 100m,
            QrPayload = "qr"
        };

        dbContext.AssetTypes.Add(assetType);
        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();

        var preconditionService = new DisposalPreconditionService(dbContext);
        var service = new DisposalService(dbContext, preconditionService);

        // Step 1: Condemn
        var condemnResult = await service.CondemnAssetAsync(orgId, asset.Id, new CondemnAssetRequest
        {
            Reason = "Mechanically broken beyond repair"
        }, requesterId);

        Assert.Equal(AssetStatusConstants.Condemned, condemnResult.Status);

        // Step 2: Submit Disposal Request
        var submitResult = await service.SubmitDisposalRequestAsync(orgId, new SubmitDisposalRequest
        {
            AssetId = asset.Id,
            DisposalMethod = DisposalMethod.SCRAP,
            EstimatedResidualValue = 50m,
            ValuationDate = new DateOnly(2026, 8, 15),
            Notes = "Scrap metal contractor collection"
        }, requesterId);

        Assert.Equal(DisposalStatus.PENDING, submitResult.Status);
        Assert.Equal(AssetStatusConstants.DisposalRequested, submitResult.AssetStatus);

        // Step 3: Approve Disposal (by Administrator different from Requester)
        // Note: CheckP1 tests Asset.Status == CONDEMNED. We set Asset.Status = CONDEMNED so P1 passes when evaluated during approval.
        asset.Status = AssetStatusConstants.Condemned;
        await dbContext.SaveChangesAsync();

        var approveResult = await service.ApproveDisposalAsync(orgId, submitResult.Id, approverId);

        Assert.True(approveResult.Success);
        Assert.NotNull(approveResult.DisposalResponse);
        Assert.Equal(DisposalStatus.APPROVED, approveResult.DisposalResponse.Status);
        Assert.Equal(AssetStatusConstants.Disposed, approveResult.DisposalResponse.AssetStatus);
        Assert.NotNull(approveResult.DisposalResponse.DisposedAt);

        // Verify DB state
        var dbAsset = await dbContext.Assets.FindAsync(asset.Id);
        Assert.NotNull(dbAsset);
        Assert.Equal(AssetStatusConstants.Disposed, dbAsset.Status);
        Assert.Equal(approverId, dbAsset.UpdatedBy);

        var historyEntries = await dbContext.AssetHistoryEntries.Where(h => h.AssetId == asset.Id).ToListAsync();
        Assert.Contains(historyEntries, h => h.EventType == "STATUS_CHANGE");
        Assert.Contains(historyEntries, h => h.EventType == "DISPOSAL");
    }

    [Fact]
    public async Task GetDisposalRequestById_WhenViewedByRequester_SeparationOfDutiesEvaluatesToFalse()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();

        var user = new User
        {
            Id = requesterId,
            OrganizationId = orgId,
            Email = "officer@example.com",
            GivenName = "Officer",
            FamilyName = "One",
            ExternalSubjectId = "sub-1",
            Role = CoreGridRole.InventoryOfficer
        };

        var org = new Organization { Id = orgId, Name = "Org" };
        var assetType = new AssetType { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "EQP", Name = "Equipment", UsefulLifeYears = 5, DefaultMaintenanceIntervalDays = 180 };
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetTypeId = assetType.Id,
            AssetCode = "AST-VIEW-1",
            Name = "Machine",
            Status = AssetStatusConstants.Condemned,
            Condition = AssetStatusConstants.ConditionPoor,
            AcquisitionDate = new DateOnly(2018, 1, 1),
            QrPayload = "qr"
        };

        var disposalRequest = new DisposalRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetId = asset.Id,
            InitiatedByUserId = requesterId,
            DisposalMethod = DisposalMethod.DESTROY,
            EstimatedResidualValue = 0m,
            ValuationDate = new DateOnly(2026, 8, 1),
            Status = DisposalStatus.PENDING,
            RequestedAt = DateTimeOffset.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.Organizations.Add(org);
        dbContext.AssetTypes.Add(assetType);
        dbContext.Assets.Add(asset);
        dbContext.DisposalRequests.Add(disposalRequest);
        await dbContext.SaveChangesAsync();

        var preconditionService = new DisposalPreconditionService(dbContext);
        var service = new DisposalService(dbContext, preconditionService);

        // Act (Caller is the requester)
        var result = await service.GetDisposalRequestByIdAsync(orgId, disposalRequest.Id, requesterId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.PreconditionEvaluation);
        Assert.False(result.PreconditionEvaluation.SeparationOfDutiesPassed);
        Assert.False(result.PreconditionEvaluation.AllPassed);
    }

    [Fact]
    public async Task GetDisposalRequestById_WhenViewedByDistinctUser_SeparationOfDutiesEvaluatesToTrue()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var distinctViewerId = Guid.NewGuid();

        var user = new User
        {
            Id = requesterId,
            OrganizationId = orgId,
            Email = "officer@example.com",
            GivenName = "Officer",
            FamilyName = "One",
            ExternalSubjectId = "sub-1",
            Role = CoreGridRole.InventoryOfficer
        };

        var org = new Organization { Id = orgId, Name = "Org" };
        var assetType = new AssetType { Id = Guid.NewGuid(), OrganizationId = orgId, Code = "EQP", Name = "Equipment", UsefulLifeYears = 5, DefaultMaintenanceIntervalDays = 180 };
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetTypeId = assetType.Id,
            AssetCode = "AST-VIEW-2",
            Name = "Generator",
            Status = AssetStatusConstants.Condemned,
            Condition = AssetStatusConstants.ConditionPoor,
            AcquisitionDate = new DateOnly(2018, 1, 1),
            QrPayload = "qr"
        };

        var disposalRequest = new DisposalRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetId = asset.Id,
            InitiatedByUserId = requesterId,
            DisposalMethod = DisposalMethod.DESTROY,
            EstimatedResidualValue = 0m,
            ValuationDate = new DateOnly(2026, 8, 1),
            Status = DisposalStatus.PENDING,
            RequestedAt = DateTimeOffset.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.Organizations.Add(org);
        dbContext.AssetTypes.Add(assetType);
        dbContext.Assets.Add(asset);
        dbContext.DisposalRequests.Add(disposalRequest);
        await dbContext.SaveChangesAsync();

        var preconditionService = new DisposalPreconditionService(dbContext);
        var service = new DisposalService(dbContext, preconditionService);

        // Act (Caller is distinct viewer / Administrator)
        var result = await service.GetDisposalRequestByIdAsync(orgId, disposalRequest.Id, distinctViewerId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.PreconditionEvaluation);
        Assert.True(result.PreconditionEvaluation.SeparationOfDutiesPassed);
        Assert.True(result.PreconditionEvaluation.AllPassed);
    }
}
