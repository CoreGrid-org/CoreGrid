using CoreGrid.Api.Features.Shared.Finance;

namespace backend.Tests.Features.Shared;

// Phase 6 (§8): pins the numbers StraightLineDepreciation produces, so a
// future change to its arithmetic is caught here first rather than as a
// silent drift in AssetService's residual value, AgentToolsService's
// compute-depreciation response, or DisposalPreconditionService's P3 check
// — the three call sites this one implementation replaced (§6.1's B22).
public class StraightLineDepreciationTests
{
    [Fact]
    public void ElapsedWholeYears_AnniversaryReached_CountsTheFullYear()
    {
        Assert.Equal(3, StraightLineDepreciation.ElapsedWholeYears(
            new DateOnly(2020, 1, 1), new DateOnly(2023, 1, 1)));
    }

    [Fact]
    public void ElapsedWholeYears_AnniversaryNotYetReached_DoesNotCountThePartialYear()
    {
        Assert.Equal(2, StraightLineDepreciation.ElapsedWholeYears(
            new DateOnly(2020, 6, 15), new DateOnly(2023, 1, 1)));
    }

    [Fact]
    public void ElapsedWholeYears_NeverNegative()
    {
        Assert.Equal(0, StraightLineDepreciation.ElapsedWholeYears(
            new DateOnly(2025, 1, 1), new DateOnly(2020, 1, 1)));
    }

    [Fact]
    public void ComputeSchedule_ThreeWholeYearsElapsed_PinsTheSchedule()
    {
        var result = StraightLineDepreciation.ComputeSchedule(
            acquisitionCost: 100000m,
            acquisitionDate: new DateOnly(2020, 1, 1),
            usefulLifeYears: 10,
            asOf: new DateOnly(2023, 1, 1));

        Assert.Equal(10000m, result.AnnualDepreciation);
        Assert.Equal(30000m, result.AccumulatedDepreciation);
        Assert.Equal(70000m, result.CurrentValue);
    }

    [Fact]
    public void ComputeSchedule_ElapsedYearsExceedUsefulLife_AccumulatedCapsAtAcquisitionCost()
    {
        var result = StraightLineDepreciation.ComputeSchedule(
            acquisitionCost: 50000m,
            acquisitionDate: new DateOnly(2010, 1, 1),
            usefulLifeYears: 5,
            asOf: new DateOnly(2026, 1, 1));

        Assert.Equal(10000m, result.AnnualDepreciation);
        Assert.Equal(50000m, result.AccumulatedDepreciation);
        Assert.Equal(0m, result.CurrentValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ComputeSchedule_UsefulLifeYearsNotPositive_TreatsAsNoDepreciation(int usefulLifeYears)
    {
        var result = StraightLineDepreciation.ComputeSchedule(
            acquisitionCost: 25000m,
            acquisitionDate: new DateOnly(2020, 1, 1),
            usefulLifeYears: usefulLifeYears,
            asOf: new DateOnly(2024, 1, 1));

        Assert.Equal(0m, result.AnnualDepreciation);
        Assert.Equal(0m, result.AccumulatedDepreciation);
        Assert.Equal(25000m, result.CurrentValue);
    }

    [Fact]
    public void CalculateResidualValue_FourYearsElapsed_PinsTheFractionalFigure()
    {
        // 2020-01-01 -> 2024-01-01 is exactly 1461 days (one leap year in the
        // span), and 1461 / 365.25 is exactly 4.0 — chosen so this pins an
        // exact decimal, not a value that depends on floating-point rounding.
        var residual = StraightLineDepreciation.CalculateResidualValue(
            acquisitionCost: 100000m,
            acquisitionDate: new DateOnly(2020, 1, 1),
            usefulLifeYears: 10,
            asOf: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(60000m, residual);
    }

    [Fact]
    public void CalculateResidualValue_UsefulLifeYearsNotPositive_ReturnsFullCost()
    {
        var residual = StraightLineDepreciation.CalculateResidualValue(
            acquisitionCost: 50000m,
            acquisitionDate: new DateOnly(2020, 1, 1),
            usefulLifeYears: 0,
            asOf: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(50000m, residual);
    }

    [Fact]
    public void CalculateResidualValue_NoTimeElapsed_ReturnsFullCost()
    {
        var residual = StraightLineDepreciation.CalculateResidualValue(
            acquisitionCost: 75000m,
            acquisitionDate: new DateOnly(2025, 1, 1),
            usefulLifeYears: 5,
            asOf: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(75000m, residual);
    }
}
