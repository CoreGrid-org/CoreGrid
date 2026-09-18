using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Agents.Services;
using Xunit;

namespace backend.Tests.Features.Agents;

public class BudgetScopeGuardTests
{
    private static FinancialAssessmentResultDto CreateValidAssessment() => new()
    {
        ResidualValue = 5000m,
        ReplacementEstimate = 12000m,
        RepairToReplaceRatio = 0.30m,
        BudgetHeadroom = null,
        RankedOptions =
        [
            new() { Action = "REPAIR", Score = 0.85m, Rationale = "Low repair costs justify retention." },
            new() { Action = "TRANSFER", Score = 0.50m, Rationale = "Asset can be transferred if needed." },
            new() { Action = "REPLACE", Score = 0.30m, Rationale = "Replacement is premature." },
            new() { Action = "DISPOSE", Score = 0.10m, Rationale = "Asset still has substantial book value." }
        ],
        ProposedRecommendation = "REPAIR"
    };

    [Fact]
    public void ValidateAssessment_WhenValid_ReturnsResult()
    {
        var assessment = CreateValidAssessment();
        var result = BudgetScopeGuard.ValidateAssessment(assessment);
        Assert.Same(assessment, result);
    }

    [Fact]
    public void ValidateAssessment_WhenNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BudgetScopeGuard.ValidateAssessment(null!));
    }

    [Fact]
    public void ValidateAssessment_WhenNegativeResidualValue_ThrowsInvalidOperationException()
    {
        var assessment = CreateValidAssessment();
        assessment.ResidualValue = -100m;

        var ex = Assert.Throws<InvalidOperationException>(() => BudgetScopeGuard.ValidateAssessment(assessment));
        Assert.Contains("Residual value must be greater than or equal to 0", ex.Message);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void ValidateAssessment_WhenOptionsCountNotFour_ThrowsInvalidOperationException(int count)
    {
        var assessment = CreateValidAssessment();
        if (count == 3)
        {
            assessment.RankedOptions.RemoveAt(0);
        }
        else
        {
            assessment.RankedOptions.Add(new RankedOptionDto { Action = "REPAIR", Score = 0.5m, Rationale = "Extra" });
        }

        var ex = Assert.Throws<InvalidOperationException>(() => BudgetScopeGuard.ValidateAssessment(assessment));
        Assert.Contains("exactly 4 ranked lifecycle options", ex.Message);
    }

    [Fact]
    public void ValidateAssessment_WhenDuplicateAction_ThrowsInvalidOperationException()
    {
        var assessment = CreateValidAssessment();
        assessment.RankedOptions[1].Action = "REPAIR"; // duplicate REPAIR instead of TRANSFER

        var ex = Assert.Throws<InvalidOperationException>(() => BudgetScopeGuard.ValidateAssessment(assessment));
        Assert.Contains("duplicated in ranked options", ex.Message);
    }

    [Fact]
    public void ValidateAssessment_WhenDisallowedAction_ThrowsInvalidOperationException()
    {
        var assessment = CreateValidAssessment();
        assessment.RankedOptions[0].Action = "SELL"; // invalid action

        var ex = Assert.Throws<InvalidOperationException>(() => BudgetScopeGuard.ValidateAssessment(assessment));
        Assert.Contains("not an allowed lifecycle action", ex.Message);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.05)]
    public void ValidateAssessment_WhenScoreOutOfRange_ThrowsInvalidOperationException(decimal score)
    {
        var assessment = CreateValidAssessment();
        assessment.RankedOptions[0].Score = score;

        var ex = Assert.Throws<InvalidOperationException>(() => BudgetScopeGuard.ValidateAssessment(assessment));
        Assert.Contains("must be within the normalized range [0.0, 1.0]", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAssessment_WhenRationaleMissing_ThrowsInvalidOperationException(string rationale)
    {
        var assessment = CreateValidAssessment();
        assessment.RankedOptions[0].Rationale = rationale;

        var ex = Assert.Throws<InvalidOperationException>(() => BudgetScopeGuard.ValidateAssessment(assessment));
        Assert.Contains("must include an evidence-based rationale", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAssessment_WhenProposedRecommendationEmpty_ThrowsInvalidOperationException(string proposed)
    {
        var assessment = CreateValidAssessment();
        assessment.ProposedRecommendation = proposed;

        var ex = Assert.Throws<InvalidOperationException>(() => BudgetScopeGuard.ValidateAssessment(assessment));
        Assert.Contains("proposed recommendation is required", ex.Message);
    }

    [Fact]
    public void ValidateAssessment_WhenProposedRecommendationDisallowed_ThrowsInvalidOperationException()
    {
        var assessment = CreateValidAssessment();
        assessment.ProposedRecommendation = "AUCTION";

        var ex = Assert.Throws<InvalidOperationException>(() => BudgetScopeGuard.ValidateAssessment(assessment));
        Assert.Contains("not an allowed lifecycle action", ex.Message);
    }

    [Fact]
    public void ValidateAssessment_WhenProposedDoesNotMatchHighestScore_ThrowsInvalidOperationException()
    {
        var assessment = CreateValidAssessment();
        // REPAIR has highest score 0.85m, but propose DISPOSE (score 0.10m)
        assessment.ProposedRecommendation = "DISPOSE";

        var ex = Assert.Throws<InvalidOperationException>(() => BudgetScopeGuard.ValidateAssessment(assessment));
        Assert.Contains("does not match the highest-scoring ranked option", ex.Message);
    }

    [Fact]
    public void FallbackAssessment_WhenRatioBelowThreshold_RecommendsRepair()
    {
        // Projected repair = $1,500; Residual = $5,000 -> Ratio = 0.30 (< 0.70 default threshold)
        var financials = new AssetFinancialsDto
        {
            AssetId = Guid.NewGuid(),
            AssetCode = "AST-001",
            AcquisitionCost = 10000m,
            ResidualBookValue = 5000m,
            CumulativeMaintenanceCost = 1000m
        };
        var stats = new FailureStatisticsDto
        {
            AssetId = financials.AssetId,
            RepairCount = 2,
            ProjectedNextTwelveMonthsCost = 1500m,
            CostTrend = "STABLE"
        };

        var result = BudgetScopeGuard.FallbackAssessment(financials, stats);

        Assert.Equal("REPAIR", result.ProposedRecommendation);
        Assert.Equal(5000m, result.ResidualValue);
        Assert.Equal(0.30m, result.RepairToReplaceRatio);
        Assert.Equal(4, result.RankedOptions.Count);

        var topOption = result.RankedOptions.MaxBy(o => o.Score);
        Assert.NotNull(topOption);
        Assert.Equal("REPAIR", topOption.Action);

        // Verify that the fallback output itself passes ValidateAssessment!
        BudgetScopeGuard.ValidateAssessment(result);
    }

    [Fact]
    public void FallbackAssessment_WhenRatioAboveThresholdAndHasResidual_RecommendsReplace()
    {
        // Projected repair = $4,000; Residual = $5,000 -> Ratio = 0.80 (>= 0.70 threshold)
        var financials = new AssetFinancialsDto
        {
            AssetId = Guid.NewGuid(),
            AssetCode = "AST-002",
            AcquisitionCost = 10000m,
            ResidualBookValue = 5000m,
            CumulativeMaintenanceCost = 6000m
        };
        var stats = new FailureStatisticsDto
        {
            AssetId = financials.AssetId,
            RepairCount = 5,
            ProjectedNextTwelveMonthsCost = 4000m,
            CostTrend = "INCREASING"
        };

        var result = BudgetScopeGuard.FallbackAssessment(financials, stats);

        Assert.Equal("REPLACE", result.ProposedRecommendation);
        Assert.Equal(0.80m, result.RepairToReplaceRatio);

        var topOption = result.RankedOptions.MaxBy(o => o.Score);
        Assert.NotNull(topOption);
        Assert.Equal("REPLACE", topOption.Action);

        BudgetScopeGuard.ValidateAssessment(result);
    }

    [Fact]
    public void FallbackAssessment_WhenRatioAboveThresholdAndZeroResidual_RecommendsDispose()
    {
        // Fully depreciated asset: Residual = $0; Projected repair = $2,000 -> Ratio = 2000.0 (>= 0.70 threshold)
        var financials = new AssetFinancialsDto
        {
            AssetId = Guid.NewGuid(),
            AssetCode = "AST-003",
            AcquisitionCost = 10000m,
            ResidualBookValue = 0m,
            CumulativeMaintenanceCost = 8000m
        };
        var stats = new FailureStatisticsDto
        {
            AssetId = financials.AssetId,
            RepairCount = 8,
            ProjectedNextTwelveMonthsCost = 2000m,
            CostTrend = "INCREASING"
        };

        var result = BudgetScopeGuard.FallbackAssessment(financials, stats);

        Assert.Equal("DISPOSE", result.ProposedRecommendation);
        Assert.Equal(0m, result.ResidualValue);

        var topOption = result.RankedOptions.MaxBy(o => o.Score);
        Assert.NotNull(topOption);
        Assert.Equal("DISPOSE", topOption.Action);

        BudgetScopeGuard.ValidateAssessment(result);
    }

    [Fact]
    public void FallbackAssessment_CustomPolicyThreshold_IsRespected()
    {
        // Ratio = 0.50. With default threshold (0.70), this would be REPAIR.
        // With a strict policy threshold of 0.40, ratio 0.50 >= 0.40 -> REPLACE.
        var financials = new AssetFinancialsDto
        {
            AssetId = Guid.NewGuid(),
            AssetCode = "AST-004",
            AcquisitionCost = 10000m,
            ResidualBookValue = 4000m
        };
        var stats = new FailureStatisticsDto
        {
            AssetId = financials.AssetId,
            ProjectedNextTwelveMonthsCost = 2000m
        };

        var result = BudgetScopeGuard.FallbackAssessment(financials, stats, policyRepairToReplaceThreshold: 0.40m);

        Assert.Equal("REPLACE", result.ProposedRecommendation);
        Assert.Equal(0.50m, result.RepairToReplaceRatio);

        BudgetScopeGuard.ValidateAssessment(result);
    }
}
