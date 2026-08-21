using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Disposals;

namespace backend.Tests.Features.Disposals;

public class DisposalPreconditionServiceTests
{
    private CoreGridDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CoreGridDbContext(options);
    }

    [Fact]
    public void CheckP1_WhenAssetStatusIsCondemned_ReturnsPassed()
    {
        // Arrange
        var service = new DisposalPreconditionService(CreateInMemoryDbContext());
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            AssetCode = "AST-001",
            Name = "Server Rack",
            Status = AssetStatusConstants.Condemned,
            Condition = AssetStatusConstants.ConditionUnserviceable,
            QrPayload = "payload"
        };

        // Act
        var result = service.CheckP1AssetCondemned(asset);

        // Assert
        Assert.True(result.Passed);
        Assert.Null(result.FailureReason);
        Assert.Equal("P1", result.Code);
    }

    [Fact]
    public void CheckP1_WhenAssetStatusIsNotCondemned_ReturnsFailed()
    {
        // Arrange
        var service = new DisposalPreconditionService(CreateInMemoryDbContext());
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            AssetCode = "AST-002",
            Name = "Laptop",
            Status = AssetStatusConstants.Active,
            Condition = AssetStatusConstants.ConditionGood,
            QrPayload = "payload"
        };

        // Act
        var result = service.CheckP1AssetCondemned(asset);

        // Assert
        Assert.False(result.Passed);
        Assert.NotNull(result.FailureReason);
        Assert.Contains("ACTIVE", result.FailureReason);
    }

    [Fact]
    public void CheckP2_WhenAmountAndDateAreBothPresent_ReturnsPassed()
    {
        // Arrange
        var service = new DisposalPreconditionService(CreateInMemoryDbContext());
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            AssetCode = "AST-001",
            Name = "Asset",
            Status = AssetStatusConstants.Condemned,
            Condition = AssetStatusConstants.ConditionPoor,
            QrPayload = "payload"
        };

        var request = new DisposalRequest
        {
            Id = Guid.NewGuid(),
            EstimatedResidualValue = 250.00m,
            ValuationDate = new DateOnly(2026, 8, 1)
        };

        // Act
        var result = service.CheckP2ValuationRecorded(request, asset);

        // Assert
        Assert.True(result.Passed);
        Assert.Null(result.FailureReason);
        Assert.Equal("P2", result.Code);
    }

    [Fact]
    public void CheckP2_WhenAmountIsMissingOrNegative_ReturnsFailed()
    {
        // Arrange
        var service = new DisposalPreconditionService(CreateInMemoryDbContext());
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            AssetCode = "AST-001",
            Name = "Asset",
            Status = AssetStatusConstants.Condemned,
            Condition = AssetStatusConstants.ConditionPoor,
            QrPayload = "payload"
        };

        var request = new DisposalRequest
        {
            Id = Guid.NewGuid(),
            EstimatedResidualValue = -1.00m,
            ValuationDate = new DateOnly(2026, 8, 1)
        };

        // Act
        var result = service.CheckP2ValuationRecorded(request, asset);

        // Assert
        Assert.False(result.Passed);
        Assert.NotNull(result.FailureReason);
        Assert.Contains("amount", result.FailureReason);
    }

    [Fact]
    public void CheckP2_WhenDateIsMissing_ReturnsFailed()
    {
        // Arrange
        var service = new DisposalPreconditionService(CreateInMemoryDbContext());
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            AssetCode = "AST-001",
            Name = "Asset",
            Status = AssetStatusConstants.Condemned,
            Condition = AssetStatusConstants.ConditionPoor,
            QrPayload = "payload"
        };

        var request = new DisposalRequest
        {
            Id = Guid.NewGuid(),
            EstimatedResidualValue = 250.00m,
            ValuationDate = null
        };

        // Act
        var result = service.CheckP2ValuationRecorded(request, asset);

        // Assert
        Assert.False(result.Passed);
        Assert.NotNull(result.FailureReason);
        Assert.Contains("date", result.FailureReason);
    }

    [Fact]
    public void CheckP2_WhenBothAmountAndDateAreMissing_ReturnsFailed()
    {
        // Arrange
        var service = new DisposalPreconditionService(CreateInMemoryDbContext());
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            AssetCode = "AST-001",
            Name = "Asset",
            Status = AssetStatusConstants.Condemned,
            Condition = AssetStatusConstants.ConditionPoor,
            QrPayload = "payload"
        };

        var request = new DisposalRequest
        {
            Id = Guid.NewGuid(),
            EstimatedResidualValue = -1.00m,
            ValuationDate = null
        };

        // Act
        var result = service.CheckP2ValuationRecorded(request, asset);

        // Assert
        Assert.False(result.Passed);
        Assert.NotNull(result.FailureReason);
        Assert.Contains("Both", result.FailureReason);
    }

    [Fact]
    public void CheckP3_WhenServiceLifeMeetsOrExceedsRequirement_ReturnsPassed()
    {
        // Arrange
        var service = new DisposalPreconditionService(CreateInMemoryDbContext());
        var evaluationDate = new DateOnly(2026, 8, 15);
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            AssetCode = "AST-003",
            Name = "Old Vehicle",
            Status = AssetStatusConstants.Condemned,
            Condition = AssetStatusConstants.ConditionPoor,
            AcquisitionDate = new DateOnly(2020, 1, 1), // 6+ years ago
            QrPayload = "payload"
        };

        var assetType = new AssetType
        {
            Id = Guid.NewGuid(),
            Code = "VEH",
            Name = "Vehicle",
            UsefulLifeYears = 5
        };

        var policy = new OrganizationPolicy
        {
            MinimumServiceLifeYears = 5
        };

        // Act
        var result = service.CheckP3ServiceLifeElapsed(asset, policy, assetType, evaluationDate);

        // Assert
        Assert.True(result.Passed);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public void CheckP3_WhenServiceLifeIsLessThanRequirement_ReturnsFailed()
    {
        // Arrange
        var service = new DisposalPreconditionService(CreateInMemoryDbContext());
        var evaluationDate = new DateOnly(2026, 8, 15);
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            AssetCode = "AST-004",
            Name = "New Vehicle",
            Status = AssetStatusConstants.Condemned,
            Condition = AssetStatusConstants.ConditionPoor,
            AcquisitionDate = new DateOnly(2024, 1, 1), // Only 2 years elapsed
            QrPayload = "payload"
        };

        var assetType = new AssetType
        {
            Id = Guid.NewGuid(),
            Code = "VEH",
            Name = "Vehicle",
            UsefulLifeYears = 5
        };

        var policy = new OrganizationPolicy
        {
            MinimumServiceLifeYears = 5
        };

        // Act
        var result = service.CheckP3ServiceLifeElapsed(asset, policy, assetType, evaluationDate);

        // Assert
        Assert.False(result.Passed);
        Assert.NotNull(result.FailureReason);
        Assert.Contains("less than required minimum", result.FailureReason);
    }

    [Fact]
    public async Task CheckP4_WhenNoMaintenanceRecordExists_ReturnsPassed()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var assetId = Guid.NewGuid();
        var service = new DisposalPreconditionService(dbContext);

        // Act
        var result = await service.CheckP4NoOpenMaintenanceAsync(assetId);

        // Assert
        Assert.True(result.Passed);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task CheckP4_WhenOnlyClosedMaintenanceRecordsExist_ReturnsPassed()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var assetId = Guid.NewGuid();

        dbContext.MaintenanceRecords.AddRange(
            new MaintenanceRecord
            {
                Id = Guid.NewGuid(),
                AssetId = assetId,
                Status = MaintenanceStatus.COMPLETED,
                Description = "Fix motor"
            },
            new MaintenanceRecord
            {
                Id = Guid.NewGuid(),
                AssetId = assetId,
                Status = MaintenanceStatus.CANCELLED,
                Description = "Inspection cancelled"
            }
        );
        await dbContext.SaveChangesAsync();

        var service = new DisposalPreconditionService(dbContext);

        // Act
        var result = await service.CheckP4NoOpenMaintenanceAsync(assetId);

        // Assert
        Assert.True(result.Passed);
        Assert.Null(result.FailureReason);
    }

    [Theory]
    [InlineData(MaintenanceStatus.REQUESTED)]
    [InlineData(MaintenanceStatus.APPROVED)]
    [InlineData(MaintenanceStatus.IN_PROGRESS)]
    public async Task CheckP4_WhenOpenMaintenanceRecordExists_ReturnsFailed(MaintenanceStatus openStatus)
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var assetId = Guid.NewGuid();

        dbContext.MaintenanceRecords.Add(new MaintenanceRecord
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Status = openStatus,
            Description = "Active repair work"
        });
        await dbContext.SaveChangesAsync();

        var service = new DisposalPreconditionService(dbContext);

        // Act
        var result = await service.CheckP4NoOpenMaintenanceAsync(assetId);

        // Assert
        Assert.False(result.Passed);
        Assert.NotNull(result.FailureReason);
        Assert.Contains(openStatus.ToString(), result.FailureReason);
    }

    [Fact]
    public async Task CheckP5_WhenNoOpenTransferExists_ReturnsPassed()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var assetId = Guid.NewGuid();

        // Closed transfer (COMPLETED)
        dbContext.AssetTransfers.Add(new AssetTransfer
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Status = TransferStatus.COMPLETED
        });
        await dbContext.SaveChangesAsync();

        var service = new DisposalPreconditionService(dbContext);

        // Act
        var result = await service.CheckP5NoOpenTransfersAsync(assetId);

        // Assert
        Assert.True(result.Passed);
        Assert.Null(result.FailureReason);
    }

    [Theory]
    [InlineData(TransferStatus.REQUESTED)]
    [InlineData(TransferStatus.APPROVED)]
    [InlineData(TransferStatus.IN_TRANSIT)]
    public async Task CheckP5_WhenOpenTransferExists_ReturnsFailed(TransferStatus openStatus)
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var assetId = Guid.NewGuid();

        dbContext.AssetTransfers.Add(new AssetTransfer
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Status = openStatus
        });
        await dbContext.SaveChangesAsync();

        var service = new DisposalPreconditionService(dbContext);

        // Act
        var result = await service.CheckP5NoOpenTransfersAsync(assetId);

        // Assert
        Assert.False(result.Passed);
        Assert.NotNull(result.FailureReason);
        Assert.Contains("active transfer", result.FailureReason);
    }

    [Fact]
    public void CheckSeparationOfDuties_WhenApproverIsDifferentUser_ReturnsPassed()
    {
        // Arrange
        var service = new DisposalPreconditionService(CreateInMemoryDbContext());
        var requesterId = Guid.NewGuid();
        var approverId = Guid.NewGuid();

        var request = new DisposalRequest
        {
            Id = Guid.NewGuid(),
            InitiatedByUserId = requesterId
        };

        // Act
        var (passed, reason) = service.CheckSeparationOfDuties(request, approverId);

        // Assert
        Assert.True(passed);
        Assert.Null(reason);
    }

    [Fact]
    public void CheckSeparationOfDuties_WhenApproverIsRequester_ReturnsFailed()
    {
        // Arrange
        var service = new DisposalPreconditionService(CreateInMemoryDbContext());
        var requesterId = Guid.NewGuid();

        var request = new DisposalRequest
        {
            Id = Guid.NewGuid(),
            InitiatedByUserId = requesterId
        };

        // Act
        var (passed, reason) = service.CheckSeparationOfDuties(request, requesterId);

        // Assert
        Assert.False(passed);
        Assert.NotNull(reason);
        Assert.Contains("Separation of duties violation", reason);
    }

    [Fact]
    public async Task EvaluateAsync_WhenAllPreconditionsPass_ReturnsAllPassedTrue()
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
            Code = "DESK",
            Name = "Desk",
            UsefulLifeYears = 3
        };

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetTypeId = assetType.Id,
            AssetType = assetType,
            AssetCode = "AST-DESK-01",
            Name = "Office Desk",
            Status = AssetStatusConstants.Condemned,
            Condition = AssetStatusConstants.ConditionUnserviceable,
            AcquisitionDate = new DateOnly(2020, 1, 1),
            QrPayload = "payload"
        };

        var disposalRequest = new DisposalRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetId = asset.Id,
            Asset = asset,
            InitiatedByUserId = requesterId,
            EstimatedResidualValue = 500.00m,
            ValuationDate = new DateOnly(2026, 8, 1),
            Status = DisposalStatus.PENDING
        };

        dbContext.AssetTypes.Add(assetType);
        dbContext.Assets.Add(asset);
        dbContext.DisposalRequests.Add(disposalRequest);
        await dbContext.SaveChangesAsync();

        var service = new DisposalPreconditionService(dbContext);

        // Act
        var result = await service.EvaluateAsync(disposalRequest.Id, approverId);

        // Assert
        Assert.True(result.SeparationOfDutiesPassed);
        Assert.True(result.AllPassed);
        Assert.Equal(6, result.Checks.Count);
        Assert.All(result.Checks, c => Assert.True(c.Passed));
    }
}
