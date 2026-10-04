using System.Text.Json.Serialization;

namespace CoreGrid.Api.Features.Agents.DTOs;

public class PlannerExecutionPlan
{
    [JsonPropertyName("inScope")]
    public bool InScope { get; set; }

    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }

    [JsonPropertyName("steps")]
    public List<PlannerPlanStep> Steps { get; set; } = [];
}

public class PlannerPlanStep
{
    [JsonPropertyName("seq")]
    public int Seq { get; set; }

    [JsonPropertyName("agent")]
    public required string Agent { get; set; }

    [JsonPropertyName("purpose")]
    public required string Purpose { get; set; }

    [JsonPropertyName("expectedOutput")]
    public required string ExpectedOutput { get; set; }
}
