using System;
using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.Maintenance.DTOs;
public class ApproveMaintenanceRequest
{
    [Required]
    public Guid? AssigneeId { get; set; }

    [Required]
    [Range(0, 1_000_000_000_000)]
    public decimal? EstimatedCost { get; set; }
}
