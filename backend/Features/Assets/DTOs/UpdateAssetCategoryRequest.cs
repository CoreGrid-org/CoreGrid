namespace CoreGrid.Api.Features.Assets.DTOs;

public class UpdateAssetCategoryRequest
{
    public required string Code { get; set; }

    public required string Name { get; set; }
}
