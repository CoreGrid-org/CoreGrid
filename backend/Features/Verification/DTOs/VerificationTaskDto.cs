using System.ComponentModel.DataAnnotations;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared.Paging;

namespace CoreGrid.Api.Features.Verification.DTOs;
// Represents a verification task.
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
    public string? AssertedLocationName { get; set; }
    public string? AssertedCondition { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}

public class CompleteVerificationTaskRequest
{
   // Indicates whether the asset was present during verification.
    [Required]
    public bool? AssertedPresent { get; set; }

    public Guid? AssertedLocationId { get; set; }

    [MaxLength(20)]
    public string? AssertedCondition { get; set; }
}

// Defines filters for verification task queries.
public class VerificationTaskQueryParameters : PagedQuery
{
    public Guid? CampaignId { get; set; }
    public bool Mine { get; set; }
    public bool OnlyPending { get; set; }
}
