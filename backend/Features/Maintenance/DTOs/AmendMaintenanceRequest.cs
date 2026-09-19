using System.ComponentModel.DataAnnotations;
using CoreGrid.Api.Domain;

namespace CoreGrid.Api.Features.Maintenance.DTOs;

// SRS §9.3: "Amend classification, priority, description." Full-replace
// update, same convention as every other PUT in this codebase — all three
// fields required.
public class AmendMaintenanceRequest
{
    [Required]
    public MaintenanceType? Type { get; set; }

    [Required]
    public MaintenancePriority? Priority { get; set; }

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
}
