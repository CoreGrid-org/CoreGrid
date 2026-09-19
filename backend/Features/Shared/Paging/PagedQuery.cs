namespace CoreGrid.Api.Features.Shared.Paging;

// Bound query parameters for every list endpoint (NFR-07, §5.4). Feature
// query-parameter records embed or mirror this shape; ToPagedResultAsync
// clamps it regardless of what the client sent.
public class PagedQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    // §10 export paths (CSV/PDF) need more rows per page than a UI list
    // ever would; ToPagedResultAsync accepts this as an explicit override
    // for exactly those callers rather than raising MaxPageSize globally.
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
