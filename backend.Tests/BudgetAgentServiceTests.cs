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

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid TypeId = Guid.NewGuid();

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static EvaluationScope SingleAssetScope(Guid assetId) => new(OrgId, TypeId, "Laptop", "IT", assetId, "AST-TEST-01");
    private static EvaluationScope FleetScope() => new(OrgId, TypeId, "Laptop", "IT", null, null);

    private static FailureStatisticsDto CreateSampleStats(Guid assetId, decimal projected = 1200m) => new()
    {
        AssetId = assetId,
        RepairCount = 3,
        ProjectedNextTwelveMonthsCost = projected,
        CostTrend = "STABLE",
        EvaluatedAsOf = DateOnly.FromDateTime(DateTime.UtcNow)
    };

    private static AssetFinancialsDto CreateSampleFinancials(Guid assetId, decimal residual = 6000m) => new()
    {
        AssetId = assetId,
        AssetCode = $"AST-{assetId.ToString()[..4]}",
        AcquisitionCost = 10000m,
        AcquisitionDate = new DateOnly(2023, 1, 1),
        UsefulLifeYears = 5,
        AccumulatedDepreciation = 10000m - residual,
        ResidualBookValue = residual,
        CumulativeMaintenanceCost = 800m
    };

    private void SetupFinancials(params AssetFinancialsDto[] financials) =>
        _mockAgentTools.Setup(t => t.GetFleetFinancialsAsync(OrgId, TypeId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(financials);

    private static object ChatResponse(object reply) => new
    {
        choices = new[] { new { message = new { content = JsonSerializer.Serialize(reply) } } }
    };

    private static object ModelReply(string pick, string why, decimal repair = 0.9m) => new
    {
        scores = new Dictionary<string, decimal> { ["REPAIR"] = repair, ["REPLACE"] = 0.3m, ["TRANSFER"] = 0.2m, ["DISPOSE"] = 0.05m, ["RETAIN"] = 0.1m },
        pick,
        why
    };

    [Fact]
    public async Task RunAssessmentAsync_WhenApiKeyMissing_ReturnsDeterministicWithoutHttpCall()
    {
        var assetId = Guid.NewGuid();
        SetupFinancials(CreateSampleFinancials(assetId));

        var config = CreateConfiguration(new Dictionary<string, string?> { ["Budget:ApiKey"] = null, ["Planner:OpenAiApiKey"] = null });
        var service = new BudgetAgentService(_mockAgentTools.Object, config, _mockHttpClientFactory.Object, _mockLogger.Object);

        var result = await service.RunAssessmentAsync(SingleAssetScope(assetId), [CreateSampleStats(assetId)]);

        Assert.Equal("REPAIR", result.ProposedRecommendation); // 1200 / 6000 = 0.20 < 0.70
        Assert.Equal(6000m, result.ResidualValue);
        Assert.Equal("DETERMINISTIC", result.Source);
        Assert.Equal(5, result.RankedOptions.Count);
        Assert.Single(result.Assets!);
        _mockHttpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAssessmentAsync_Fleet_TriagesEveryAssetAndTotalsTheScope()
    {
        var repairing = Guid.NewGuid();
        var idle = Guid.NewGuid();
        var worn = Guid.NewGuid();
        SetupFinancials(CreateSampleFinancials(repairing), CreateSampleFinancials(idle), CreateSampleFinancials(worn, residual: 0m));

        var config = CreateConfiguration([]);
        var service = new BudgetAgentService(_mockAgentTools.Object, config, _mockHttpClientFactory.Object, _mockLogger.Object);

        var result = await service.RunAssessmentAsync(FleetScope(),
            [CreateSampleStats(repairing), CreateSampleStats(idle, projected: 0m), CreateSampleStats(worn, projected: 500m)]);

        Assert.Equal(3, result.AssetCount);
        Assert.Equal(12000m, result.ResidualValue);
        Assert.Equal(1700m, result.ProjectedRepairCost);
        var actions = result.Assets!.ToDictionary(a => a.AssetId, a => a.Action);
        Assert.Equal("REPAIR", actions[repairing]);
        Assert.Equal("RETAIN", actions[idle]);
        Assert.Equal("DISPOSE", actions[worn]);
        BudgetScopeGuard.ValidateAssessment(result);
    }

    [Fact]
    public async Task RunAssessmentAsync_WhenHttpCallFails_ReturnsDeterministicFallback()
    {
        var assetId = Guid.NewGuid();
        SetupFinancials(CreateSampleFinancials(assetId));

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError, Content = new StringContent("{\"error\":\"Model overloaded\"}") });
        _mockHttpClientFactory.Setup(f => f.CreateClient(CoreGrid.Api.Features.Agents.AgentsModule.LlmHttpClient)).Returns(new HttpClient(mockHandler.Object));

        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Budget:ApiKey"] = "sk-test-key",
            ["Budget:Endpoint"] = "https://api.openai.com/v1/chat/completions"
        });
        var service = new BudgetAgentService(_mockAgentTools.Object, config, _mockHttpClientFactory.Object, _mockLogger.Object);

        var result = await service.RunAssessmentAsync(SingleAssetScope(assetId), [CreateSampleStats(assetId)]);

        Assert.Equal("REPAIR", result.ProposedRecommendation);
        Assert.Equal("DETERMINISTIC", result.Source);
        Assert.Equal(6000m, result.ResidualValue);
    }

    [Fact]
    public async Task RunAssessmentAsync_WhenHttpCallSucceeds_MergesModelScoresOntoDeterministicFigures()
    {
        var assetId = Guid.NewGuid();
        SetupFinancials(CreateSampleFinancials(assetId));

        string? sentBody = null;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken _) =>
            {
                sentBody = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(ChatResponse(ModelReply("REPAIR", "Ratio 0.20 is well under the 0.70 threshold."))), Encoding.UTF8, "application/json")
                };
            });
        _mockHttpClientFactory.Setup(f => f.CreateClient(CoreGrid.Api.Features.Agents.AgentsModule.LlmHttpClient)).Returns(new HttpClient(mockHandler.Object));

        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Budget:ApiKey"] = "sk-test-key",
            ["Budget:Model"] = "gemini-2.0-flash"
        });
        var service = new BudgetAgentService(_mockAgentTools.Object, config, _mockHttpClientFactory.Object, _mockLogger.Object);

        var result = await service.RunAssessmentAsync(SingleAssetScope(assetId), [CreateSampleStats(assetId)]);

        Assert.Equal("REPAIR", result.ProposedRecommendation);
        Assert.Equal("MODEL", result.Source);
        Assert.Equal(6000m, result.ResidualValue); // figures stay deterministic
        Assert.Equal(0.20m, result.RepairToReplaceRatio);
        Assert.Equal("REPAIR", result.RankedOptions[0].Action);
        Assert.Equal(0.90m, result.RankedOptions[0].Score);
        Assert.Equal("Ratio 0.20 is well under the 0.70 threshold.", result.RankedOptions[0].Rationale);

        // Compact contract: no per-asset rows or rationale prose sent to the model.
        Assert.NotNull(sentBody);
        Assert.DoesNotContain("asset_id", sentBody);
        Assert.DoesNotContain("max_tokens", sentBody); // only sent when configured
    }

    [Fact]
    public async Task RunAssessmentAsync_WhenModelPickIsNotTheTopScore_FallsBackToDeterministic()
    {
        var assetId = Guid.NewGuid();
        SetupFinancials(CreateSampleFinancials(assetId));

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(ChatResponse(ModelReply("DISPOSE", "Inconsistent."))), Encoding.UTF8, "application/json")
            });
        _mockHttpClientFactory.Setup(f => f.CreateClient(CoreGrid.Api.Features.Agents.AgentsModule.LlmHttpClient)).Returns(new HttpClient(mockHandler.Object));

        var config = CreateConfiguration(new Dictionary<string, string?> { ["Budget:ApiKey"] = "sk-test-key" });
        var service = new BudgetAgentService(_mockAgentTools.Object, config, _mockHttpClientFactory.Object, _mockLogger.Object);

        var result = await service.RunAssessmentAsync(SingleAssetScope(assetId), [CreateSampleStats(assetId)]);

        Assert.Equal("DETERMINISTIC", result.Source);
        Assert.Equal("REPAIR", result.ProposedRecommendation);
    }

    [Fact]
    public async Task RunAssessmentAsync_WhenPrimaryIsRateLimited_UsesTheFallbackProvider()
    {
        var assetId = Guid.NewGuid();
        SetupFinancials(CreateSampleFinancials(assetId));

        var requested = new List<(string Host, string? Model)>();
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken _) =>
            {
                var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
                requested.Add((request.RequestUri!.Host, body.RootElement.GetProperty("model").GetString()));
                return request.RequestUri!.Host == "api.groq.com"
                    ? new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(ChatResponse(ModelReply("REPAIR", "Ranked by the fallback model.", repair: 0.88m))), Encoding.UTF8, "application/json")
                    }
                    : new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("{\"error\":\"quota\"}") };
            });
        _mockHttpClientFactory.Setup(f => f.CreateClient(CoreGrid.Api.Features.Agents.AgentsModule.LlmHttpClient))
            .Returns(new HttpClient(mockHandler.Object));

        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Llm:ApiKey"] = "gemini-key",
            ["LlmFallback:ApiKey"] = "groq-key"
        });
        var service = new BudgetAgentService(_mockAgentTools.Object, config, _mockHttpClientFactory.Object, _mockLogger.Object);

        var result = await service.RunAssessmentAsync(SingleAssetScope(assetId), [CreateSampleStats(assetId)]);

        Assert.Equal(
            new[] { ("generativelanguage.googleapis.com", (string?)"gemini-3.5-flash"), ("api.groq.com", (string?)"openai/gpt-oss-120b") },
            requested.ToArray());
        Assert.Equal("Ranked by the fallback model.", result.RankedOptions[0].Rationale);
        Assert.Equal(0.88m, result.RankedOptions[0].Score);
    }
}
