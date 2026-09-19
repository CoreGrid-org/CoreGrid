using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.Maintenance.DTOs;
public class CompleteMaintenanceRequest
{
    [Required]
    [Range(0, 1_000_000_000_000)]
    public decimal? ActualCost { get; set; }

    [Required, MinLength(10), MaxLength(2000)]
    public string WorkPerformed { get; set; } = string.Empty;

    [Required]
    public DateOnly? CompletionDate { get; set; }

    [Required, MaxLength(20)]
    public string ResultingCondition { get; set; } = string.Empty;
    public string? OverspendJustification { get; set; }
}
