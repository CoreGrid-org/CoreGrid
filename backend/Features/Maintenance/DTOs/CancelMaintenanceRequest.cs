using System;
using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.Maintenance.DTOs;

public class CancelMaintenanceRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}
