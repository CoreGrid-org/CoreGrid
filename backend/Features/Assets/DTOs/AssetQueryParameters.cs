namespace CoreGrid.Api.Features.Assets.DTOs;

public class AssetQueryParameters
{
    // Search by asset code or asset name
    public string? Search { get; set; }

    // Filters
    public Guid? AssetTypeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? LocationId { get; set; }
    public string? Status { get; set; }
    public string? Condition { get; set; }

    // Sorting
    public string SortBy { get; set; } = "name";
    public string SortDirection { get; set; } = "asc";

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}