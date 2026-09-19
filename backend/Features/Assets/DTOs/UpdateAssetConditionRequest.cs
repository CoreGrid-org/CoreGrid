using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.Assets.DTOs;

public class UpdateAssetConditionRequest
{
    [Required, MaxLength(20)]
    public required string Condition { get; set; }
}