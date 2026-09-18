using System;
using System.ComponentModel.DataAnnotations;
using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Maintenance.DTOs;
public class CreateMaintenanceRequest
{
    [Required]
    public Guid? AssetId { get; set; }

    public string Description { get; set; } = string.Empty;
    public string ObservedCondition { get; set; } = string.Empty;

    // Whatever POST /api/maintenance/photos' UploadPhotoResponse.Url handed
    // back — an R2 object key, stored as MaintenanceRecord.PhotoObjectKey.
    public string? PhotoUrl { get; set; }

    [Required]
    public MaintenanceType? Type { get; set; }

    [Required]
    public MaintenancePriority? Priority { get; set; }

    public decimal? EstimatedCost { get; set; }
    public Guid? AssigneeId { get; set; }
}
