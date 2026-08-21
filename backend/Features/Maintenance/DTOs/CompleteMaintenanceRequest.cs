using System;

namespace CoreGrid.Api.Features.Maintenance.DTOs;
public class CompleteMaintenanceRequest
{
    public decimal ActualCost { get; set; }
    public string WorkPerformed { get; set; } = string.Empty;
    public DateOnly CompletionDate { get; set; }
    public string ResultingCondition { get; set; } = string.Empty;
    public string? OverspendJustification { get; set; }
}
