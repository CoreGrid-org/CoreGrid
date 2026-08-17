namespace CoreGrid.Api.Features.Assets.DTOs;

public class AssetCategoryDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int TypeCount { get; set; }

    public int AssetCount { get; set; }

    public bool IsActive { get; set; }
}