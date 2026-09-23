using System.ComponentModel.DataAnnotations;
using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Assets.DTOs;

// Defines the request for verifying an asset.
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

   // Lists discrepancies identified during verification.
    public List<DiscrepancyType> RaisedDiscrepancyTypes { get; set; } = [];

    public DateTimeOffset VerifiedAt { get; set; }
}
