using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.AgentTools.Services;

namespace backend.Tests.Features.AgentTools;

public class AgentToolsServiceTests
{
    private CoreGridDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CoreGridDbContext(options);
    }

    [Fact]
    public void ComputeDepreciation_WhenZeroTimeElapsed_ReturnsZeroAccumulatedAndFullCurrentValue()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var service = new AgentToolsService(dbContext);

        var request = new ComputeDepreciationRequest
        {
            AcquisitionCost = 1000m,
            AcquisitionDate = new DateOnly(2026, 1, 1),
            UsefulLifeYears = 5,
            AsOfDate = new DateOnly(2026, 1, 1)
        };

        // Act
        var result = service.ComputeDepreciation(request);

        // Assert
        Assert.Equal(200m, result.AnnualDepreciation);
        Assert.Equal(0m, result.AccumulatedDepreciation);
        Assert.Equal(1000m, result.CurrentValue);
        Assert.Equal("straight-line", result.DepreciationMethod);
    }

    [Fact]
    public void ComputeDepreciation_WhenExactMidLife_ReturnsHalfValue()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var service = new AgentToolsService(dbContext);

        var request = new ComputeDepreciationRequest
        {
            AcquisitionCost = 1000m,
            AcquisitionDate = new DateOnly(2020, 1, 1),
            UsefulLifeYears = 4,
            AsOfDate = new DateOnly(2022, 1, 1) // 2 years elapsed out of 4
        };

        // Act
        var result = service.ComputeDepreciation(request);

        // Assert
        Assert.Equal(250m, result.AnnualDepreciation);
        Assert.Equal(500m, result.AccumulatedDepreciation);
        Assert.Equal(500m, result.CurrentValue);
    }

    [Fact]
    public void ComputeDepreciation_WhenExactEndOfLife_ReturnsFullAccumulatedAndZeroCurrentValue()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var service = new AgentToolsService(dbContext);

        var request = new ComputeDepreciationRequest
        {
            AcquisitionCost = 1200m,
            AcquisitionDate = new DateOnly(2020, 1, 1),
            UsefulLifeYears = 3,
            AsOfDate = new DateOnly(2023, 1, 1) // Exactly 3 years elapsed
        };

        // Act
        var result = service.ComputeDepreciation(request);

        // Assert
        Assert.Equal(400m, result.AnnualDepreciation);
        Assert.Equal(1200m, result.AccumulatedDepreciation);
        Assert.Equal(0m, result.CurrentValue);
    }

    [Fact]
    public void ComputeDepreciation_WhenBeyondUsefulLife_FloorsCurrentValueAtZero()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var service = new AgentToolsService(dbContext);

        var request = new ComputeDepreciationRequest
        {
            AcquisitionCost = 5000m,
            AcquisitionDate = new DateOnly(2015, 1, 1),
            UsefulLifeYears = 5,
            AsOfDate = new DateOnly(2026, 1, 1) // 11 years elapsed (useful life is 5)
        };

        // Act
        var result = service.ComputeDepreciation(request);

        // Assert
        Assert.Equal(1000m, result.AnnualDepreciation);
        Assert.Equal(5000m, result.AccumulatedDepreciation);
        Assert.Equal(0m, result.CurrentValue);
    }

    [Fact]
    public void ComputeDepreciation_WhenInvalidParameters_HandlesSafely()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var service = new AgentToolsService(dbContext);

        var request = new ComputeDepreciationRequest
        {
            AcquisitionCost = 0m,
            AcquisitionDate = new DateOnly(2020, 1, 1),
            UsefulLifeYears = 0
        };

        // Act
        var result = service.ComputeDepreciation(request);

        // Assert
        Assert.Equal(0m, result.AccumulatedDepreciation);
        Assert.Equal(0m, result.CurrentValue);
    }

    [Fact]
    public async Task GetAssetFinancialsAsync_WhenAssetExists_ReturnsComputedDepreciationAndDetails()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();

        var assetType = new AssetType
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Code = "SRV",
            Name = "Server",
            UsefulLifeYears = 5
        };

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AssetTypeId = assetType.Id,
            AssetCode = "AST-SRV-01",
            Name = "Database Server",
            Status = "ACTIVE",
            Condition = "GOOD",
            AcquisitionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)),
            AcquisitionCost = 10000m,
            ResidualValue = 2000m,
            CumulativeMaintenanceCost = 450m,
            QrPayload = "qr"
        };

        dbContext.AssetTypes.Add(assetType);
        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();

        var service = new AgentToolsService(dbContext);

        // Act
        var result = await service.GetAssetFinancialsAsync(orgId, asset.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(asset.Id, result.AssetId);
        Assert.Equal(10000m, result.AcquisitionCost);
        Assert.Equal(5, result.UsefulLifeYears);
        Assert.Equal(4000m, result.AccumulatedDepreciation); // 2 years * 2000/year
        Assert.Equal(6000m, result.ResidualBookValue);
        Assert.Equal(450m, result.CumulativeMaintenanceCost);
        Assert.Null(result.ReplacementEstimate);
        Assert.NotNull(result.ReplacementEstimateNote);
    }

    [Fact]
    public async Task GetDepartmentBudgetSummaryAsync_WhenDepartmentExists_ReturnsStatusWithSchemaGapNote()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var orgId = Guid.NewGuid();

        var department = new Department
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Code = "IT-DEPT",
            Name = "Information Technology"
        };

        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync();

        var service = new AgentToolsService(dbContext);

        // Act
        var result = await service.GetDepartmentBudgetSummaryAsync(orgId, department.Id, 2026);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(department.Id, result.DepartmentId);
        Assert.Equal("IT-DEPT", result.DepartmentCode);
        Assert.Equal(2026, result.FiscalYear);
        Assert.Null(result.AllocatedMaintenanceBudget);
        Assert.Equal("NOT_CONFIGURED", result.Status);
        Assert.NotNull(result.Note);
    }
}
