using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.Agents.Services;
using Xunit;

namespace backend.Tests.Features.Agents;

// Deterministic recommendation heuristic feeding the Policy Compliance
// Agent (SRS §7.3, node 4) — no LLM, no database. Same-inputs-same-output
// unit tests, same convention as PolicyRuleEngineTests.
public class AssetActionRecommendationEngineTests
{
    private readonly AssetActionRecommendationEngine _engine = new();

    private static AssetComplianceStateDto Compliance(
        string condition, bool isCondemned = false, decimal elapsedYears = 3, int openTransferCount = 0) => new()
    {
        AssetId = Guid.NewGuid(),
        AssetCode = "AST-0001",
        CurrentStatus = "ACTIVE",
        CurrentCondition = condition,
        IsCondemned = isCondemned,
        HasValuation = false,
        ValuationDate = null,
        OpenMaintenanceCount = 0,
        OpenTransferCount = openTransferCount,
        ElapsedServiceLifeYears = elapsedYears
    };

    private static OrganizationPolicyFactsDto Policy(decimal minimumServiceLifeYears = 5) => new()
    {
        AssetTypeId = Guid.NewGuid(),
        RepairToReplaceCostThreshold = 0.65m,
        MinimumServiceLifeYears = minimumServiceLifeYears,
        MaxAcceptableFailureFrequency = 2,
        ValuationValidityWindowDays = 90,
        ConfidenceFloor = 0.7m
    };

    [Fact]
    public void Unserviceable_PastMinimumServiceLife_ProposesDispose()
    {
        var result = _engine.Propose(Compliance("UNSERVICEABLE", elapsedYears: 6), Policy(minimumServiceLifeYears: 5));

        Assert.Equal("DISPOSE", result.Recommendation);
    }

    [Fact]
    public void Condemned_PastMinimumServiceLife_ProposesDispose()
    {
        var result = _engine.Propose(Compliance("POOR", isCondemned: true, elapsedYears: 6), Policy(minimumServiceLifeYears: 5));

        Assert.Equal("DISPOSE", result.Recommendation);
    }

    [Fact]
    public void Unserviceable_BeforeMinimumServiceLife_ProposesRepairInstead()
    {
        var result = _engine.Propose(Compliance("UNSERVICEABLE", elapsedYears: 2), Policy(minimumServiceLifeYears: 5));

        Assert.Equal("REPAIR", result.Recommendation);
    }

    [Theory]
    [InlineData("POOR")]
    [InlineData("FAIR")]
    public void DegradedButNotUnserviceable_ProposesRepair(string condition)
    {
        var result = _engine.Propose(Compliance(condition), Policy());

        Assert.Equal("REPAIR", result.Recommendation);
    }

    [Fact]
    public void GoodConditionWithOpenTransfer_ProposesTransfer()
    {
        var result = _engine.Propose(Compliance("GOOD", openTransferCount: 1), Policy());

        Assert.Equal("TRANSFER", result.Recommendation);
    }

    [Fact]
    public void GoodConditionWithNoOpenRecords_ProposesRetain()
    {
        var result = _engine.Propose(Compliance("GOOD"), Policy());

        Assert.Equal("RETAIN", result.Recommendation);
    }

    [Fact]
    public void AlwaysReturnsANonEmptyRationale()
    {
        var result = _engine.Propose(Compliance("NEW"), Policy());

        Assert.False(string.IsNullOrWhiteSpace(result.Rationale));
    }
}
