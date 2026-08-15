using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Verification.DTOs;

public class VerificationTaskDto
{
    public Guid Id { get; set; }

    public Guid CampaignId { get; set; }
    public required string CampaignName { get; set; }

    public Guid AssetId { get; set; }
    public required string AssetCode { get; set; }
    public required string AssetName { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToEmail { get; set; }

    public DateOnly DueDate { get; set; }
    public VerificationTaskStatus Status { get; set; }

    public bool? AssertedPresent { get; set; }
    public Guid? AssertedLocationId { get; set; }
    public string? AssertedCondition { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}

public class CompleteVerificationTaskRequest
{
    public bool AssertedPresent { get; set; }
    public Guid? AssertedLocationId { get; set; }
    public string? AssertedCondition { get; set; }
}
