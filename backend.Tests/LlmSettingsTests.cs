using CoreGrid.Api.Features.Agents;
using Microsoft.Extensions.Configuration;

namespace backend.Tests.Features.Agents;

public class LlmSettingsTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void DefaultsToGeminiFlashOnTheOpenAiCompatibleEndpoint()
    {
        var settings = LlmSettings.For(Config(new()), "Planner");

        Assert.Equal("gemini-3.5-flash", settings.Model);
        Assert.Equal("https://generativelanguage.googleapis.com/v1beta/openai/chat/completions", settings.Endpoint);
        Assert.False(settings.HasApiKey);
    }

    [Fact]
    public void SharedSectionAppliesToEveryAgent_AndAgentSectionOverridesIt()
    {
        var config = Config(new()
        {
            ["Llm:ApiKey"] = "shared-key",
            ["Llm:Model"] = "gemini-3.5-flash",
            ["Budget:Model"] = "gemini-3.5-pro",
        });

        Assert.Equal("shared-key", LlmSettings.For(config, "Planner").ApiKey);
        Assert.Equal("gemini-3.5-flash", LlmSettings.For(config, "Planner").Model);
        Assert.Equal("gemini-3.5-pro", LlmSettings.For(config, "Budget").Model);
    }

    [Fact]
    public void BlankPlaceholdersDoNotHideARealValue()
    {
        var config = Config(new()
        {
            ["Planner:ApiKey"] = "",
            ["Planner:Model"] = "  ",
            ["Llm:ApiKey"] = "from-user-secrets",
        });

        var settings = LlmSettings.For(config, "Planner");
        Assert.Equal("from-user-secrets", settings.ApiKey);
        Assert.Equal(LlmSettings.DefaultModel, settings.Model);
    }

    [Fact]
    public void LegacyKeyNamesAreStillHonoured()
    {
        var config = Config(new() { ["Planner:OpenAiApiKey"] = "legacy" });

        Assert.Equal("legacy", LlmSettings.For(config, "Planner", "Planner:OpenAiApiKey").ApiKey);
    }
}
