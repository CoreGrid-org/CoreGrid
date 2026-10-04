using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.Agents.DTOs;

public class CreateAgentWorkflowRequest
{
    public Guid? AssetTypeId { get; set; }

    public Guid? AssetId { get; set; }

    [Required, MaxLength(2000)]
    public required string Objective { get; set; }
}

public class EvaluatePolicyRequest
{
    [Required, MaxLength(50)]
    public required string ProposedRecommendation { get; set; }

    public FinancialAssessmentFacts? FinancialAssessment { get; set; }
}

public class DecideWorkflowRequest
{
    [Required, MaxLength(20)]
    public required string Decision { get; set; }

    [Required, MaxLength(2000)]
    public required string Reason { get; set; }
}
