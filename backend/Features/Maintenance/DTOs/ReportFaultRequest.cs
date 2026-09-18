using System;

namespace CoreGrid.Api.Features.Maintenance.DTOs;

public class ReportFaultRequest
{
    public Guid AssetId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ObservedCondition { get; set; } = string.Empty; // NEW, GOOD, FAIR, POOR, UNSERVICEABLE

    // Whatever POST /api/maintenance/photos' UploadPhotoResponse.Url handed
    // back — an R2 object key, stored as MaintenanceRecord.PhotoObjectKey.
    public string? PhotoUrl { get; set; }
}
