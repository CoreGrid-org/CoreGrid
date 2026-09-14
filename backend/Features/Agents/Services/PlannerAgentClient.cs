using System.Net.Http.Json;
using CoreGrid.Api.Features.Agents.DTOs;

namespace CoreGrid.Api.Features.Agents.Services;

public sealed class PlannerAgentClient : IPlannerAgentClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public PlannerAgentClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<PlannerExecutionPlan> CreatePlanAsync(
        Guid assetId,
        string objective,
        Guid initiatedBy,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var request = new PlannerObjectiveRequest
        {
            AssetId = assetId,
            ObjectiveText = objective,
            InitiatedBy = initiatedBy,
            OrganizationId = organizationId
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, "/plan")
        {
            Content = JsonContent.Create(request)
        };

        var sharedSecret = _configuration["PlannerAgent:SharedSecret"];
        if (!string.IsNullOrWhiteSpace(sharedSecret))
        {
            message.Headers.Add("X-Planner-Secret", sharedSecret);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Planner Agent returned {(int)response.StatusCode}: {detail}");
        }

        return await response.Content.ReadFromJsonAsync<PlannerExecutionPlan>(cancellationToken)
            ?? throw new InvalidOperationException("Planner Agent returned an empty plan.");
    }
}