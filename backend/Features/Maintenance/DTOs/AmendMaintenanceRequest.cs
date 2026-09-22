using System.ComponentModel.DataAnnotations;
using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Maintenance.DTOs;

// Defines the request for updating a maintenance record.
public class AmendMaintenanceRequest
{
    [Required]
    public MaintenanceType? Type { get; set; }

    [Required]
    public MaintenancePriority? Priority { get; set; }

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
}
