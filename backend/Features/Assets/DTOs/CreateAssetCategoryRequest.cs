using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.Assets.DTOs;

public class CreateAssetCategoryRequest
{
    [Required, MaxLength(20)]
    public required string Code { get; set; }

    [Required, MaxLength(200)]
    public required string Name { get; set; }
}
