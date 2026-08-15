namespace CoreGrid.Api.Features.Assets.DTOs;

public class CreateAssetRequest
{
    public Guid AssetTypeId { get; set; }

    public Guid DepartmentId { get; set; }

    public Guid LocationId { get; set; }

    public required string Name { get; set; }

    public DateOnly AcquisitionDate { get; set; }

    public decimal AcquisitionCost { get; set; }

    public decimal ResidualValue { get; set; }

    public string Condition { get; set; } = "NEW";

    public List<AssetAttributeValueRequest> Attributes { get; set; } = [];
}