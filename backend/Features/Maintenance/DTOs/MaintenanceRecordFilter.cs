using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared.Paging;

namespace CoreGrid.Api.Features.Maintenance.DTOs;

// Defines the filters for maintenance record queries.
public class MaintenanceRecordFilter : PagedQuery
{
    // Newest-first by default (unlike PagedQuery's own "asc" default) —
    // matches the "Requested" column's previous default ordering.
    public MaintenanceRecordFilter()
    {
        SortDirection = "desc";
    }

    public Guid? AssetId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? AssigneeId { get; set; }
    public MaintenanceStatus? Status { get; set; }
    public MaintenanceType? Type { get; set; }
    public MaintenancePriority? Priority { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
