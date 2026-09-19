using CoreGrid.Api.Features.Shared.Paging;

namespace CoreGrid.Api.Features.Assets.DTOs;

// Page/PageSize/SortBy/SortDirection/Search come from PagedQuery — this
// only adds the asset-specific filters (§5.3: GetAssetsAsync uses
// PagedQuery).
public class AssetQueryParameters : PagedQuery
{
    public Guid? CategoryId { get; set; }
    public Guid? AssetTypeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? LocationId { get; set; }
    public string? Status { get; set; }
    public string? Condition { get; set; }
}
