using System.ComponentModel.DataAnnotations;
using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Assets.DTOs;

// SRS §9.2 / FR-031: "asserting its presence, its actual location and its
// actual condition; the system shall compare the assertion against the
// register and raise a discrepancy where they differ." Same shape and
// validation as CompleteVerificationTaskRequest (Verification/DTOs) —
// this is the same assertion, just made standalone instead of against a
// campaign task.
public class VerifyAssetRequest
{
    [Required]
    public bool? AssertedPresent { get; set; }

    public Guid? AssertedLocationId { get; set; }

    [MaxLength(20)]
    public string? AssertedCondition { get; set; }
}

public class AssetVerificationResultDto
{
    public Guid AssetId { get; set; }
    public bool AssertedPresent { get; set; }
    public Guid? AssertedLocationId { get; set; }
    public string? AssertedCondition { get; set; }

    // Empty when the assertion matched the register — nothing to reconcile.
    public List<DiscrepancyType> RaisedDiscrepancyTypes { get; set; } = [];

    public DateTimeOffset VerifiedAt { get; set; }
}
