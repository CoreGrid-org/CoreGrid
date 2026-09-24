using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.Assets.DTOs;

public class UpdateAssetCategoryRequest
{
    [Required, MaxLength(20)]
    public required string Code { get; set; }

    [Required, MaxLength(200)]
    public required string Name { get; set; }
}
