using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Maintenance.DTOs;

// FR-042: filter by status, priority, department, asset, assignee and date
// range, with sorting and pagination. DepartmentId filters via the owning
// Asset (MaintenanceRecord itself has no DepartmentId of its own — same
// join AssetService already does for its own DepartmentId filter). The
// date range applies to CreatedAt (when the record was requested/raised),
// matching the "Requested" column MaintenancePage.tsx already renders.
public class MaintenanceRecordFilter
{
    public Guid? AssetId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? AssigneeId { get; set; }
    public MaintenanceStatus? Status { get; set; }
    public MaintenanceType? Type { get; set; }
    public MaintenancePriority? Priority { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }

    // Sorting
    public string SortBy { get; set; } = "createdat";
    public string SortDirection { get; set; } = "desc";

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
