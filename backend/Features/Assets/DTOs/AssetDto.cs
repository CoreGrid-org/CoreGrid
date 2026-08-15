namespace CoreGrid.Api.Features.Assets.DTOs;

public class AssetDto
{
    public Guid Id { get; set; }

    public string AssetCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Guid AssetTypeId { get; set; }
    public string AssetTypeName { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;

    public DateOnly AcquisitionDate { get; set; }
    public decimal AcquisitionCost { get; set; }

    public string QrPayload { get; set; } = string.Empty;
}