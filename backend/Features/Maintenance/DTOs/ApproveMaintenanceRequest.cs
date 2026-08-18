using System;

namespace CoreGrid.Api.Features.Maintenance.DTOs;
public class ApproveMaintenanceRequest
{
    public Guid AssigneeId { get; set; }
    public decimal EstimatedCost { get; set; }
}
