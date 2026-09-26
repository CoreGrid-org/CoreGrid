using System.Net;
using System.Text;
using System.Text.Json;
using CoreGrid.Api.Features.AgentTools.DTOs;
using CoreGrid.Api.Features.AgentTools.Services;
using CoreGrid.Api.Features.Agents.DTOs;
using CoreGrid.Api.Features.Agents.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace backend.Tests.Features.Agents;

public class BudgetAgentServiceTests
{
    private readonly Mock<IAgentToolsService> _mockAgentTools = new();
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();
    private readonly Mock<ILogger<BudgetAgentService>> _mockLogger = new();

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static FailureStatisticsDto CreateSampleStats(Guid assetId) => new()
    {
        AssetId = assetId,
        RepairCount = 3,
        ProjectedNextTwelveMonthsCost = 1200m,
        CostTrend = "STABLE",
        EvaluatedAsOf = DateOnly.FromDateTime(DateTime.UtcNow)
    };

    private static AssetFinancialsDto CreateSampleFinancials(Guid assetId) => new()
    {
        AssetId = assetId,
        AssetCode = "AST-TEST-01",
        AcquisitionCost = 10000m,
        AcquisitionDate = new DateOnly(2023, 1, 1),
        UsefulLifeYears = 5,
        AccumulatedDepreciation = 4000m,
        ResidualBookValue = 6000m,
        CumulativeMaintenanceCost = 800m
    };

    [Fact]
    public async Task RunAssessmentAsync_WhenApiKeyMissing_ReturnsFallbackWithoutHttpCall()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var stats = CreateSampleStats(assetId);
        var financials = CreateSampleFinancials(assetId);

        _mockAgentTools.Setup(t => t.GetAssetFinancialsAsync(orgId, assetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(financials);

        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Budget:ApiKey"] = null,
            ["Planner:OpenAiApiKey"] = null
        });

        var service = new BudgetAgentService(
            _mockAgentTools.Object,
            config,
            _mockHttpClientFactory.Object,
            _mockLogger.Object);

        // Act
        var result = await service.RunAssessmentAsync(orgId, assetId, null, null, stats);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("REPAIR", result.ProposedRecommendation); // Ratio 1200/6000 = 0.20 < 0.70
        Assert.Equal(6000m, result.ResidualValue);
        Assert.Equal(4, result.RankedOptions.Count);
        _mockHttpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAssessmentAsync_WhenAssetNotFound_ReturnsZeroedFallback()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var stats = CreateSampleStats(assetId);

        _mockAgentTools.Setup(t => t.GetAssetFinancialsAsync(orgId, assetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetFinancialsDto?)null);

        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Budget:ApiKey"] = "sk-mock-key"
        });

        var service = new BudgetAgentService(
            _mockAgentTools.Object,
            config,
            _mockHttpClientFactory.Object,
            _mockLogger.Object);

        // Act
        var result = await service.RunAssessmentAsync(orgId, assetId, null, null, stats);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("DISPOSE", result.ProposedRecommendation); // 0 residual -> DISPOSE
        Assert.Equal(0m, result.ResidualValue);
    }

    [Fact]
    public async Task RunAssessmentAsync_WhenHttpCallFails_ReturnsDeterministicFallback()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var stats = CreateSampleStats(assetId);
        var financials = CreateSampleFinancials(assetId);

        _mockAgentTools.Setup(t => t.GetAssetFinancialsAsync(orgId, assetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(financials);

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("{\"error\":\"Model overloaded\"}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(CoreGrid.Api.Features.Agents.AgentsModule.LlmHttpClient)).Returns(httpClient);

        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Budget:ApiKey"] = "sk-test-key",
            ["Budget:Endpoint"] = "https://api.openai.com/v1/chat/completions"
        });

        var service = new BudgetAgentService(
            _mockAgentTools.Object,
            config,
            _mockHttpClientFactory.Object,
            _mockLogger.Object);

        // Act
        var result = await service.RunAssessmentAsync(orgId, assetId, null, null, stats);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("REPAIR", result.ProposedRecommendation); // Ratio 0.20 < 0.70
        Assert.Equal(6000m, result.ResidualValue);
    }

    [Fact]
    public async Task RunAssessmentAsync_WhenHttpCallSucceeds_ParsesAndValidatesLlmAssessment()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var stats = CreateSampleStats(assetId);
        var financials = CreateSampleFinancials(assetId);

        _mockAgentTools.Setup(t => t.GetAssetFinancialsAsync(orgId, assetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(financials);

        var expectedLlmPayload = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = JsonSerializer.Serialize(new
                        {
                            residual_value = 6000.0,
                            replacement_estimate = 15000.0,
                            repair_to_replace_ratio = 0.20,
                            budget_headroom = (double?)null,
                            ranked_options = new[]
                            {
                                new { action = "REPAIR", score = 0.90, rationale = "Economic repair is optimal." },
                                new { action = "TRANSFER", score = 0.50, rationale = "Transfer is possible if required." },
                                new { action = "REPLACE", score = 0.30, rationale = "Replacement is premature." },
                                new { action = "DISPOSE", score = 0.05, rationale = "Asset still has high utility." }
                            },
                            proposed_recommendation = "REPAIR"
                        })
                    }
                }
            }
        };

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(expectedLlmPayload),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(CoreGrid.Api.Features.Agents.AgentsModule.LlmHttpClient)).Returns(httpClient);

        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Budget:ApiKey"] = "sk-test-key",
            ["Budget:Endpoint"] = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
            ["Budget:Model"] = "gemini-2.0-flash"
        });

        var service = new BudgetAgentService(
            _mockAgentTools.Object,
            config,
            _mockHttpClientFactory.Object,
            _mockLogger.Object);

        // Act
        var result = await service.RunAssessmentAsync(orgId, assetId, null, null, stats);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("REPAIR", result.ProposedRecommendation);
        Assert.Equal(6000m, result.ResidualValue);
        Assert.Equal(15000m, result.ReplacementEstimate);
        Assert.Equal(0.20m, result.RepairToReplaceRatio);
        Assert.Equal(4, result.RankedOptions.Count);
        Assert.Equal("REPAIR", result.RankedOptions[0].Action);
        Assert.Equal(0.90m, result.RankedOptions[0].Score);
    }
}
