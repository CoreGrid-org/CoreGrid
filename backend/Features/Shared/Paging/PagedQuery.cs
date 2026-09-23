namespace CoreGrid.Api.Features.Shared.Paging;

// Defines common pagination and filtering parameters.
public class PagedQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

// Defines the maximum page size for export operations.
    public const int MaxExportPageSize = 500;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "asc";
    public string? Search { get; set; }

    public int ClampedPage => Page < 1 ? 1 : Page;

    public int ClampedPageSize(int maxPageSize = MaxPageSize) =>
        PageSize < 1 ? DefaultPageSize : Math.Min(PageSize, maxPageSize);

    public bool IsDescending => string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
}
